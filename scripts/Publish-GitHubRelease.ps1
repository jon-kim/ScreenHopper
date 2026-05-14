param(
	[string]$ProjectPath = ".\ScreenHopper\ScreenHopper.csproj",
	[string]$Configuration = "Release",
	[string]$Runtime = "win-x64",
	[string]$Tag,
	[string]$ReleaseTitle,
	[string]$NotesFile,
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

function New-ReleaseTag {
	$now = Get-Date
	return "v1.$($now.ToString('yyyy')).$('{0:D3}' -f $now.DayOfYear).$($now.ToString('HHmmssfff'))"
}

Assert-CommandExists -Name "git"
Assert-CommandExists -Name "dotnet"
Assert-CommandExists -Name "gh"

if ([string]::IsNullOrWhiteSpace($Tag)) {
	$Tag = New-ReleaseTag
}

if ([string]::IsNullOrWhiteSpace($ReleaseTitle)) {
	$ReleaseTitle = "ScreenHopper $Tag"
}

$repoRoot = Resolve-Path "."
$resolvedProjectPath = Resolve-Path $ProjectPath
$projectDirectory = Split-Path -Parent $resolvedProjectPath

$artifactRoot = Join-Path $repoRoot "artifacts"
$publishDir = Join-Path $artifactRoot "publish\$Tag\$Runtime"
$zipPath = Join-Path $artifactRoot "ScreenHopper-$Tag-$Runtime.zip"

if (-not $SkipTagCreation) {
	$existingTag = (& git tag --list $Tag)
	if ([string]::IsNullOrWhiteSpace($existingTag)) {
		Write-Host "Creating tag $Tag"
		& git tag -a $Tag -m "ScreenHopper $Tag"
	}
	else {
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

	Write-Host "Publishing self-contained single-file build"
	& dotnet publish $resolvedProjectPath -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -p:PublishDir="$publishDir\"

	if (Test-Path $zipPath) {
		Remove-Item $zipPath -Force
	}

	Write-Host "Creating archive $zipPath"
	Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force
}

if (-not (Test-Path $zipPath)) {
	throw "Release artifact not found: $zipPath"
}

$releaseArgs = @("release", "create", $Tag, $zipPath, "--title", $ReleaseTitle)

if ($Draft) {
	$releaseArgs += "--draft"
}

if ($Prerelease) {
	$releaseArgs += "--prerelease"
}

if (-not [string]::IsNullOrWhiteSpace($NotesFile)) {
	$resolvedNotes = Resolve-Path $NotesFile
	$releaseArgs += @("--notes-file", $resolvedNotes)
}
else {
	$releaseArgs += "--generate-notes"
}

Write-Host "Creating GitHub release $Tag"
& gh @releaseArgs

Write-Host "Release complete."
Write-Host "Tag: $Tag"
Write-Host "Artifact: $zipPath"
