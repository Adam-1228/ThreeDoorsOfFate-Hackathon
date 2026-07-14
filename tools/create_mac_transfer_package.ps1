[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [string]$VersionTag = (Get-Date -Format "yyyyMMdd")
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectParent = Split-Path -Parent $projectRoot
$projectName = Split-Path -Leaf $projectRoot

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectParent "Builds\Transfer"
}

$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

$archiveName = "ThreeDoorsOfFate-mac-ios-source-$VersionTag.zip"
$archivePath = Join-Path $outputRoot $archiveName
$checksumPath = "$archivePath.sha256"

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

$excludedPaths = @(
    ".git",
    ".vs",
    "Library",
    "Temp",
    "Obj",
    "obj",
    "Build",
    "Builds",
    "Logs",
    "UserSettings",
    "Backups",
    "Previews",
    "tools/previews",
    "tools/installers",
    "tools/archived_assets",
    "tools/__pycache__"
)

$tarArguments = @("-a", "-c", "-f", $archivePath, "-C", $projectParent)
foreach ($path in $excludedPaths) {
    $tarArguments += "--exclude=$projectName/$path"
}
$tarArguments += "--exclude=$projectName/*.csproj"
$tarArguments += "--exclude=$projectName/*.sln"
$tarArguments += "--exclude=$projectName/*.user"
$tarArguments += $projectName

& tar @tarArguments
if ($LASTEXITCODE -ne 0) {
    throw "tar failed with exit code $LASTEXITCODE"
}

$hash = Get-FileHash -Algorithm SHA256 -LiteralPath $archivePath
$checksumLine = "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), $archiveName
Set-Content -LiteralPath $checksumPath -Value $checksumLine -Encoding ascii

$archive = Get-Item -LiteralPath $archivePath
[PSCustomObject]@{
    Archive = $archive.FullName
    Bytes = $archive.Length
    SHA256 = $hash.Hash.ToLowerInvariant()
    ChecksumFile = $checksumPath
}
