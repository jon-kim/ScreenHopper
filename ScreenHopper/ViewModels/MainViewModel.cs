using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ScreenHopper.Commands;
using ScreenHopper.Models;
using ScreenHopper.Services;

namespace ScreenHopper.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ProcessDiscoveryService _processDiscoveryService;
    private readonly MonitorService _monitorService;
    private readonly PreferencesService _preferencesService;
    private readonly WindowMoveService _windowMoveService;

    private PreferencesStore _preferencesStore;
    private AppProcessItem? _selectedApplication;
    private MonitorInfo? _selectedMonitor;
    private ZoneSnap _selectedZone = ZoneSnap.FullCenter;

    public MainViewModel()
        : this(new ProcessDiscoveryService(), new MonitorService(), new PreferencesService(), new WindowMoveService())
    {
    }

    public MainViewModel(
        ProcessDiscoveryService processDiscoveryService,
        MonitorService monitorService,
        PreferencesService preferencesService,
        WindowMoveService windowMoveService)
    {
        _processDiscoveryService = processDiscoveryService;
        _monitorService = monitorService;
        _preferencesService = preferencesService;
        _windowMoveService = windowMoveService;

        _preferencesStore = _preferencesService.Load();

        Applications = new ObservableCollection<AppProcessItem>();
        Monitors = new ObservableCollection<MonitorInfo>();

        ZoneOptions =
        [
            ZoneSnap.LeftHalf,
            ZoneSnap.RightHalf,
            ZoneSnap.TopLeft,
            ZoneSnap.TopRight,
            ZoneSnap.BottomLeft,
            ZoneSnap.BottomRight,
            ZoneSnap.FullCenter
        ];

        RefreshAppsCommand = new RelayCommand(_ => RefreshApplications());
        RefreshMonitorsCommand = new RelayCommand(_ => RefreshMonitors());
        HideFromListCommand = new RelayCommand(ExecuteHideFromList, CanHideFromList);
        SelectZoneCommand = new RelayCommand(ExecuteSelectZone);
        MoveCommand = new RelayCommand(_ => MoveSelected(), _ => CanMoveSelected());

        RefreshMonitors();
        RefreshApplications();
    }

    public ObservableCollection<AppProcessItem> Applications { get; }

    public ObservableCollection<MonitorInfo> Monitors { get; }

    public IReadOnlyList<ZoneSnap> ZoneOptions { get; }

    public AppProcessItem? SelectedApplication
    {
        get => _selectedApplication;
        set
        {
            if (ReferenceEquals(_selectedApplication, value))
            {
                return;
            }

            if (_selectedApplication is not null)
            {
                _selectedApplication.Preferences.PropertyChanged -= SelectedPreferences_PropertyChanged;
            }

            _selectedApplication = value;

            if (_selectedApplication is not null)
            {
                if (!_preferencesStore.AppPreferences.TryGetValue(_selectedApplication.ProcessName, out var preferences))
                {
                    preferences = _selectedApplication.Preferences;
                    _preferencesStore.AppPreferences[_selectedApplication.ProcessName] = preferences;
                }

                _selectedApplication.Preferences = preferences;
                _selectedApplication.Preferences.PropertyChanged += SelectedPreferences_PropertyChanged;
                SetSelectedZoneFromPreference(_selectedApplication.Preferences.DefaultZone);
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(RequiresDoubleClick));
            OnPropertyChanged(nameof(OpacityLevel));
            OnPropertyChanged(nameof(AlwaysOnTop));
            RaiseCommandStates();
        }
    }

    public MonitorInfo? SelectedMonitor
    {
        get => _selectedMonitor;
        set
        {
            if (ReferenceEquals(_selectedMonitor, value))
            {
                return;
            }

            _selectedMonitor = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    public bool RequiresDoubleClick
    {
        get => SelectedApplication?.Preferences.RequiresDoubleClick ?? false;
        set
        {
            if (SelectedApplication is null || SelectedApplication.Preferences.RequiresDoubleClick == value)
            {
                return;
            }

            SelectedApplication.Preferences.RequiresDoubleClick = value;
            OnPropertyChanged();
        }
    }

    public int OpacityLevel
    {
        get => SelectedApplication?.Preferences.OpacityLevel ?? 100;
        set
        {
            if (SelectedApplication is null || SelectedApplication.Preferences.OpacityLevel == value)
            {
                return;
            }

            SelectedApplication.Preferences.OpacityLevel = value;
            OnPropertyChanged();
        }
    }

    public bool AlwaysOnTop
    {
        get => SelectedApplication?.Preferences.AlwaysOnTop ?? false;
        set
        {
            if (SelectedApplication is null || SelectedApplication.Preferences.AlwaysOnTop == value)
            {
                return;
            }

            SelectedApplication.Preferences.AlwaysOnTop = value;
            OnPropertyChanged();
        }
    }

    public ZoneSnap SelectedZone
    {
        get => _selectedZone;
        set
        {
            if (_selectedZone == value)
            {
                return;
            }

            _selectedZone = value;

            if (SelectedApplication is not null)
            {
                SelectedApplication.Preferences.DefaultZone = value;
            }

            OnPropertyChanged();
        }
    }

    public ICommand RefreshAppsCommand { get; }

    public ICommand RefreshMonitorsCommand { get; }

    public ICommand HideFromListCommand { get; }

    public ICommand SelectZoneCommand { get; }

    public ICommand MoveCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool TryMoveByArguments(string processName, int monitorIndex, ZoneSnap zone)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        RefreshMonitors();
        RefreshApplications();

        var matchedApp = Applications.FirstOrDefault(app =>
            app.ProcessName.Contains(processName, StringComparison.OrdinalIgnoreCase) ||
            app.DisplayText.Contains(processName, StringComparison.OrdinalIgnoreCase));

        if (matchedApp is null || monitorIndex < 1 || monitorIndex > Monitors.Count)
        {
            return false;
        }

        SelectedApplication = matchedApp;
        SelectedMonitor = Monitors[monitorIndex - 1];
        SelectedZone = zone;

        return MoveSelected();
    }

    private void RefreshApplications()
    {
        Applications.Clear();

        var blacklist = _preferencesStore.BlacklistedProcessSet;
        var apps = _processDiscoveryService.GetVisibleProcesses(blacklist, _preferencesStore.AppPreferences);

        foreach (var app in apps)
        {
            Applications.Add(app);
        }

        SelectedApplication = Applications.FirstOrDefault();
    }

    private void RefreshMonitors()
    {
        Monitors.Clear();

        foreach (var monitor in _monitorService.GetMonitors())
        {
            Monitors.Add(monitor);
        }

        SelectedMonitor = Monitors.FirstOrDefault(m => m.IsPrimary) ?? Monitors.FirstOrDefault();
    }

    private bool MoveSelected()
    {
        if (SelectedApplication is null || SelectedMonitor is null)
        {
            return false;
        }

        var moveRequest = new MoveRequest
        {
            WindowHandle = SelectedApplication.MainWindowHandle,
            DestinationMonitor = SelectedMonitor,
            Zone = SelectedApplication.Preferences.DefaultZone,
            RequiresDoubleClick = SelectedApplication.Preferences.RequiresDoubleClick,
            OpacityLevel = SelectedApplication.Preferences.OpacityLevel,
            AlwaysOnTop = SelectedApplication.Preferences.AlwaysOnTop
        };

        var moved = _windowMoveService.MoveWindow(moveRequest);
        SavePreferences();
        return moved;
    }

    private bool CanMoveSelected() => SelectedApplication is not null && SelectedMonitor is not null;

    private void SetSelectedZoneFromPreference(ZoneSnap zone)
    {
        _selectedZone = zone;
        OnPropertyChanged(nameof(SelectedZone));
    }

    private bool CanHideFromList(object? parameter) => parameter is AppProcessItem;

    private void ExecuteHideFromList(object? parameter)
    {
        if (parameter is not AppProcessItem item)
        {
            return;
        }

        var blacklist = _preferencesStore.BlacklistedProcessSet;
        blacklist.Add(item.ProcessName);

        _preferencesStore = new PreferencesStore
        {
            BlacklistedProcesses = blacklist.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList(),
            AppPreferences = _preferencesStore.AppPreferences
        };

        SavePreferences();
        RefreshApplications();
    }

    private void ExecuteSelectZone(object? parameter)
    {
        if (parameter is ZoneSnap zone)
        {
            SelectedZone = zone;
            SavePreferences();
        }
    }

    private void SavePreferences()
    {
        _preferencesService.Save(_preferencesStore);
    }

    private void SelectedPreferences_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(RequiresDoubleClick));
        OnPropertyChanged(nameof(OpacityLevel));
        OnPropertyChanged(nameof(AlwaysOnTop));

        if (e.PropertyName == nameof(AppPreferences.DefaultZone))
        {
            OnPropertyChanged(nameof(SelectedZone));
        }

        SavePreferences();
    }

    private void RaiseCommandStates()
    {
        if (MoveCommand is RelayCommand move)
        {
            move.RaiseCanExecuteChanged();
        }

        if (HideFromListCommand is RelayCommand hide)
        {
            hide.RaiseCanExecuteChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
