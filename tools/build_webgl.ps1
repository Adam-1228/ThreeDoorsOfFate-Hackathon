param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe',
    [string]$ProjectRoot = (Join-Path $PSScriptRoot '..')
)

$projectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$buildRoot = (Resolve-Path (Join-Path $projectRoot '..')).Path
$indexPath = Join-Path $buildRoot 'Builds\WebGL\index.html'
$logPath = Join-Path $buildRoot 'Builds\Logs\webgl-build.log'

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    Write-Error "Unity executable not found: $UnityPath"
    exit 1
}

New-Item -ItemType Directory -Force (Split-Path $logPath -Parent) | Out-Null

$unityArguments = @(
    '-batchmode'
    '-nographics'
    '-quit'
    '-projectPath'
    "`"$projectRoot`""
    '-executeMethod'
    'ThreeDoorsOfFate.Editor.PlayableGameBuilder.BuildWebGLPlayable'
    '-logFile'
    "`"$logPath`""
)
$unityProcess = Start-Process `
    -FilePath $UnityPath `
    -ArgumentList $unityArguments `
    -Wait `
    -PassThru
$unityExitCode = $unityProcess.ExitCode

if ($unityExitCode -ne 0) {
    exit $unityExitCode
}

if (-not (Test-Path -LiteralPath $indexPath -PathType Leaf)) {
    Write-Error "WebGL build did not produce index.html: $indexPath"
    exit 1
}

Write-Output "WebGL build succeeded: $indexPath"
exit 0
