param(
	[string]$ProjectPath = ".\ScreenHopper\ScreenHopper.csproj",
	[string]$Configuration = "Release",
	[string]$Runtime = "win-x64",
	[string]$Tag,
	[string]$ReleaseTitle,
	[string]$NotesFile,
	[string]$GitHubRepo = "https://github.com/jon-kim/ScreenHopper",
	[string]$GitHubToken = $env:GITHUB_TOKEN,
	[switch]$Draft,
	[switch]$Prerelease,
	[switch]$SkipTagCreation,
	[switch]$SkipPublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-CommandExists {
	param([string]$Name)

	if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
		throw "Required command '$Name' was not found. Install it and retry."
	}
}

function New-AssemblyVersion {
	$now = Get-Date
	$year = $now.ToString('yy')
	$dayOfYear = '{0:D3}' -f $now.DayOfYear
	$minutesSinceMidnight = ($now.Hour * 60) + $now.Minute
	return "0.$year.$dayOfYear.$minutesSinceMidnight"
}

Assert-CommandExists -Name "git"
Assert-CommandExists -Name "dotnet"
Assert-CommandExists -Name "gh"
Assert-CommandExists -Name "vpk"

if ([string]::IsNullOrWhiteSpace($Tag)) {
	$assemblyVersion = New-AssemblyVersion
	$Tag = "v$assemblyVersion"
} else {
	$assemblyVersion = $Tag -replace '^v', ''
}

# Velopack requires a 3-part SemVer package version.
# Convert assembly version 0.yy.dayofyear.minutes to 0.yydayofyear.minutes.
$versionParts = $assemblyVersion.Split('.')
if ($versionParts.Length -ne 4) {
	throw "Assembly version '$assemblyVersion' is invalid. Expected format: 0.yy.dayofyear.current_time_in_minutes"
}
$semverMinor = "$($versionParts[1])$($versionParts[2])"
$semver = "$($versionParts[0]).$semverMinor.$($versionParts[3])"

if ([string]::IsNullOrWhiteSpace($ReleaseTitle)) {
	$ReleaseTitle = "ScreenHopper $Tag"
}

$repoRoot = Resolve-Path "."
$resolvedProjectPath = Resolve-Path $ProjectPath

$artifactRoot    = Join-Path $repoRoot "artifacts"
$publishDir      = Join-Path $artifactRoot "publish\$Tag\$Runtime"
$velopackOutDir  = Join-Path $artifactRoot "velopack\$Tag"

if (-not $SkipTagCreation) {
	$existingTag = (& git tag --list $Tag)
	if ([string]::IsNullOrWhiteSpace($existingTag)) {
		Write-Host "Creating tag $Tag"
		& git tag -a $Tag -m "ScreenHopper $Tag"
	} else {
		Write-Host "Tag $Tag already exists locally."
	}

	Write-Host "Pushing tag $Tag"
	& git push origin $Tag
}

if (-not $SkipPublish) {
	if (Test-Path $publishDir) {
		Remove-Item $publishDir -Recurse -Force
	}
	New-Item -Path $publishDir -ItemType Directory -Force | Out-Null

	# Publish without single-file for Velopack (needs loose files)
	Write-Host "Publishing build for Velopack packaging (version $assemblyVersion)"
	& dotnet publish $resolvedProjectPath -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=false -p:PublishDir="$publishDir\" -p:UseTimestampVersion=false -p:Version=$assemblyVersion -p:InformationalVersion=$assemblyVersion -p:AssemblyVersion=$assemblyVersion -p:FileVersion=$assemblyVersion

	if (Test-Path $velopackOutDir) {
		Remove-Item $velopackOutDir -Recurse -Force
	}
	New-Item -Path $velopackOutDir -ItemType Directory -Force | Out-Null

	Write-Host "Packaging with Velopack (version $semver)"
	& vpk pack `
		--packId "ScreenHopper" `
		--packVersion $semver `
		--packDir $publishDir `
		--mainExe "ScreenHopper.exe" `
		--outputDir $velopackOutDir
}

if (-not (Test-Path $velopackOutDir)) {
	throw "Velopack output not found: $velopackOutDir"
}

# Resolve token: explicit param > env var > gh CLI credential store
if ([string]::IsNullOrWhiteSpace($GitHubToken)) {
	Write-Host "GITHUB_TOKEN not set, attempting to resolve token via 'gh auth token'..."
	$GitHubToken = (& gh auth token 2>$null) | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($GitHubToken)) {
	throw "No GitHub token found. Set the GITHUB_TOKEN environment variable, pass -GitHubToken, or run 'gh auth login'."
}

# Let vpk create the release and upload assets in one step
Write-Host "Creating GitHub release $Tag and uploading Velopack assets"
$vpkArgs = @(
	"upload", "github",
	"--repoUrl",    $GitHubRepo,
	"--outputDir",  $velopackOutDir,
	"--tag",        $Tag,
	"--token",      $GitHubToken,
	"--releaseName", $ReleaseTitle
)

if ($Prerelease) { $vpkArgs += "--pre-release" }
if (-not $Draft) { $vpkArgs += "--publish" }

& vpk @vpkArgs

# Patch the release: notes and optional flags.
# gh release edit does not support --generate-notes, so we call the API to
# generate notes and then pass them as a plain string.
Write-Host "Patching release notes"
$ghEditArgs = @("release", "edit", $Tag)

if ($Draft)      { $ghEditArgs += "--draft" }
if ($Prerelease) { $ghEditArgs += "--prerelease" }
if (-not $Draft) { $ghEditArgs += "--latest" }

if (-not [string]::IsNullOrWhiteSpace($NotesFile)) {
	$ghEditArgs += @("--notes-file", (Resolve-Path $NotesFile))
} else {
	# Ask the GitHub API to generate release notes for this tag, then apply them
	$repoSlug = ($GitHubRepo -replace 'https://github.com/', '')
	$apiBody  = "{`"tag_name`":`"$Tag`"}"
	$generated = ($apiBody | & gh api "repos/$repoSlug/releases/generate-notes" --method POST --input - 2>$null) |
				 ConvertFrom-Json -ErrorAction SilentlyContinue

	if ($generated -and $generated.body) {
		$ghEditArgs += @("--notes", $generated.body)
	}
}

& gh @ghEditArgs

Write-Host "Release complete."
Write-Host "Tag:     $Tag"
Write-Host "Version: $semver"
Write-Host "Output:  $velopackOutDir"
