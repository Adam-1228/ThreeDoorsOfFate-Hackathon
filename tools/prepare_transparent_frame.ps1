param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing
$resolvedInput = [System.IO.Path]::GetFullPath($InputPath)
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
$source = [System.Drawing.Bitmap]::new($resolvedInput)
$working = [System.Drawing.Bitmap]::new(
    $source.Width,
    $source.Height,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
)
$graphics = [System.Drawing.Graphics]::FromImage($working)
$graphics.Clear([System.Drawing.Color]::Transparent)
$graphics.DrawImageUnscaled($source, 0, 0)
$graphics.Dispose()
$source.Dispose()

$fullRect = [System.Drawing.Rectangle]::new(0, 0, $working.Width, $working.Height)
$data = $working.LockBits(
    $fullRect,
    [System.Drawing.Imaging.ImageLockMode]::ReadWrite,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
)
$pixels = [byte[]]::new([Math]::Abs($data.Stride) * $data.Height)
[System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $pixels, 0, $pixels.Length)

$minX = $working.Width
$minY = $working.Height
$maxX = -1
$maxY = -1
for ($y = 0; $y -lt $working.Height; $y += 1) {
    $row = $y * $data.Stride
    for ($x = 0; $x -lt $working.Width; $x += 1) {
        $index = $row + $x * 4
        $blue = [int]$pixels[$index]
        $green = [int]$pixels[$index + 1]
        $red = [int]$pixels[$index + 2]
        $maximum = [Math]::Max($red, [Math]::Max($green, $blue))
        $minimum = [Math]::Min($red, [Math]::Min($green, $blue))
        $average = [int](($red + $green + $blue) / 3)
        $isBackground = $average -ge 220 -and ($maximum - $minimum) -le 22
        $alpha = if ($isBackground) { 0 } else { 255 }
        $pixels[$index + 3] = [byte]$alpha

        if ($alpha -gt 0) {
            $minX = [Math]::Min($minX, $x)
            $minY = [Math]::Min($minY, $y)
            $maxX = [Math]::Max($maxX, $x)
            $maxY = [Math]::Max($maxY, $y)
        }
    }
}

[System.Runtime.InteropServices.Marshal]::Copy($pixels, 0, $data.Scan0, $pixels.Length)
$working.UnlockBits($data)
if ($maxX -lt $minX -or $maxY -lt $minY) {
    $working.Dispose()
    throw 'No visible frame pixels were found.'
}

$padding = 8
$cropLeft = [Math]::Max(0, $minX - $padding)
$cropTop = [Math]::Max(0, $minY - $padding)
$cropRight = [Math]::Min($working.Width - 1, $maxX + $padding)
$cropBottom = [Math]::Min($working.Height - 1, $maxY + $padding)
$cropRect = [System.Drawing.Rectangle]::FromLTRB(
    $cropLeft,
    $cropTop,
    $cropRight + 1,
    $cropBottom + 1
)

$outputDirectory = [System.IO.Path]::GetDirectoryName($resolvedOutput)
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$cropped = $working.Clone($cropRect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$cropped.Save($resolvedOutput, [System.Drawing.Imaging.ImageFormat]::Png)
$cropped.Dispose()
$working.Dispose()
Write-Output "Prepared transparent frame: $resolvedOutput"
