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
$manifestPath = "$archivePath.manifest.json"
$temporarySuffix = ".tmp-$PID-$([Guid]::NewGuid().ToString('N'))"
$temporaryArchivePath = Join-Path $outputRoot ".$archiveName$temporarySuffix.zip"
$temporaryChecksumPath = "$checksumPath$temporarySuffix"
$temporaryManifestPath = "$manifestPath$temporarySuffix"

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

function Get-GitOutput {
    param([Parameter(Mandatory)][string[]]$Arguments)

    $output = & git -C $projectRoot @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
    return $output
}

function Get-DirtyPaths {
    $statusLines = Get-GitOutput -Arguments @(
        "-c",
        "core.quotePath=false",
        "status",
        "--porcelain=v1",
        "--untracked-files=all"
    )
    $paths = foreach ($line in $statusLines) {
        if ($line.Length -lt 4) {
            continue
        }

        $path = $line.Substring(3)
        if ($path.Contains(" -> ")) {
            $path = $path.Split(@(" -> "), [StringSplitOptions]::None)[-1]
        }
        $path.Trim('"').Replace('\', '/')
    }
    return @($paths | Sort-Object -Unique)
}

try {
    $tarArguments = @("-a", "-c", "-f", $temporaryArchivePath, "-C", $projectParent)
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

    $archiveEntries = @(& tar -tf $temporaryArchivePath)
    if ($LASTEXITCODE -ne 0) {
        throw "tar -tf failed with exit code $LASTEXITCODE"
    }
    $entrySet = [Collections.Generic.HashSet[string]]::new(
        [StringComparer]::Ordinal
    )
    foreach ($entry in $archiveEntries) {
        [void]$entrySet.Add(
            $entry.Replace('\', '/').TrimStart([char[]]@('.', '/'))
        )
    }

    $dirtyPaths = Get-DirtyPaths
    $missingDirtyPaths = foreach ($path in $dirtyPaths) {
        $entryName = "$projectName/$path"
        if (-not $entrySet.Contains($entryName)) {
            $path
        }
    }
    if (@($missingDirtyPaths).Count -gt 0) {
        throw "Archive is missing dirty paths: $($missingDirtyPaths -join ', ')"
    }

    $branch = (Get-GitOutput -Arguments @("branch", "--show-current") | Select-Object -First 1)
    $baseCommit = (Get-GitOutput -Arguments @("rev-parse", "HEAD") | Select-Object -First 1)
    $manifest = [ordered]@{
        schemaVersion = 1
        project = $projectName
        branch = $branch
        baseCommit = $baseCommit
        createdAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
        dirtyPaths = $dirtyPaths
        excludedPaths = $excludedPaths
    }
    $manifest | ConvertTo-Json -Depth 4 |
        Set-Content -LiteralPath $temporaryManifestPath -Encoding utf8

    $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $temporaryArchivePath
    $checksumLine = "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), $archiveName
    Set-Content -LiteralPath $temporaryChecksumPath -Value $checksumLine -Encoding ascii

    foreach ($publishedPath in @($archivePath, $checksumPath, $manifestPath)) {
        if (Test-Path -LiteralPath $publishedPath) {
            Remove-Item -LiteralPath $publishedPath -Force
        }
    }
    Move-Item -LiteralPath $temporaryArchivePath -Destination $archivePath
    Move-Item -LiteralPath $temporaryChecksumPath -Destination $checksumPath
    Move-Item -LiteralPath $temporaryManifestPath -Destination $manifestPath

    $archive = Get-Item -LiteralPath $archivePath
    [PSCustomObject]@{
        Archive = $archive.FullName
        Bytes = $archive.Length
        SHA256 = $hash.Hash.ToLowerInvariant()
        ChecksumFile = $checksumPath
        ManifestFile = $manifestPath
        DirtyPathCount = $dirtyPaths.Count
    }
}
finally {
    foreach ($temporaryPath in @(
        $temporaryArchivePath,
        $temporaryChecksumPath,
        $temporaryManifestPath
    )) {
        if (Test-Path -LiteralPath $temporaryPath) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
    }
}
