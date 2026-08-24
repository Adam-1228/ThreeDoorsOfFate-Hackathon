[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$outputRoot = Join-Path $projectRoot 'Assets\Audio\SFX'
$ffmpegCandidates = @(
    (Get-Command ffmpeg -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1),
    'C:\ffmpeg\bin\ffmpeg.exe'
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

if ($ffmpegCandidates.Count -eq 0) {
    throw 'FFmpeg was not found in PATH or C:\ffmpeg\bin\ffmpeg.exe.'
}

$ffmpeg = $ffmpegCandidates[0]
$culture = [System.Globalization.CultureInfo]::InvariantCulture

$sounds = @(
    @{ Path = 'UI\ui_accept_01.wav'; Duration = 0.22; Expr = '0.46*sin(2*PI*(980*t+850*t*t))*exp(-24*t)+0.24*sin(2*PI*1840*t)*exp(-34*t)+0.16*sin(2*PI*3130*t)*exp(-42*t)' },
    @{ Path = 'UI\ui_accept_02.wav'; Duration = 0.23; Expr = '0.44*sin(2*PI*(1060*t+760*t*t))*exp(-23*t)+0.23*sin(2*PI*1970*t)*exp(-33*t)+0.15*sin(2*PI*3370*t)*exp(-41*t)' },
    @{ Path = 'UI\ui_accept_03.wav'; Duration = 0.21; Expr = '0.45*sin(2*PI*(920*t+940*t*t))*exp(-25*t)+0.22*sin(2*PI*1710*t)*exp(-36*t)+0.17*sin(2*PI*2890*t)*exp(-44*t)' },
    @{ Path = 'UI\ui_back.wav'; Duration = 0.28; Expr = '0.42*sin(2*PI*(760*t-430*t*t))*exp(-18*t)+0.22*sin(2*PI*1240*t)*exp(-28*t)+0.13*sin(2*PI*2410*t)*exp(-35*t)' },
    @{ Path = 'UI\ui_denied.wav'; Duration = 0.38; Expr = '0.34*sin(2*PI*178*t)*exp(-8*t)+0.31*sin(2*PI*193*t)*exp(-8.5*t)+0.18*sin(2*PI*911*t)*exp(-24*t)' },
    @{ Path = 'UI\panel_open.wav'; Duration = 0.42; Expr = '0.34*sin(2*PI*(330*t+880*t*t))*exp(-7.5*t)+0.20*sin(2*PI*(740*t+420*t*t))*exp(-10*t)+0.12*sin(2*PI*2660*t)*exp(-28*t)' },
    @{ Path = 'UI\panel_close.wav'; Duration = 0.38; Expr = '0.35*sin(2*PI*(780*t-570*t*t))*exp(-8*t)+0.20*sin(2*PI*(1280*t-530*t*t))*exp(-11*t)+0.12*sin(2*PI*2360*t)*exp(-29*t)' },

    @{ Path = 'Cards\card_draw_01.wav'; Duration = 0.34; Expr = '0.22*(sin(2*PI*1733*t)*sin(2*PI*2389*t)+sin(2*PI*3119*t)*sin(2*PI*967*t))*exp(-8*t)+0.18*sin(2*PI*(420*t+360*t*t))*exp(-12*t)' },
    @{ Path = 'Cards\card_draw_02.wav'; Duration = 0.36; Expr = '0.21*(sin(2*PI*1811*t)*sin(2*PI*2477*t)+sin(2*PI*3299*t)*sin(2*PI*1031*t))*exp(-7.5*t)+0.18*sin(2*PI*(390*t+410*t*t))*exp(-11*t)' },
    @{ Path = 'Cards\card_play_01.wav'; Duration = 0.43; Expr = '0.24*(sin(2*PI*1499*t)*sin(2*PI*2341*t)+sin(2*PI*3181*t)*sin(2*PI*887*t))*exp(-9*t)+0.30*sin(2*PI*(210*t-45*t*t))*exp(-10*t)+0.13*sin(2*PI*1230*t)*exp(-24*t)' },
    @{ Path = 'Cards\card_play_02.wav'; Duration = 0.45; Expr = '0.23*(sin(2*PI*1579*t)*sin(2*PI*2297*t)+sin(2*PI*3067*t)*sin(2*PI*953*t))*exp(-8.5*t)+0.29*sin(2*PI*(225*t-52*t*t))*exp(-9.5*t)+0.13*sin(2*PI*1310*t)*exp(-23*t)' },
    @{ Path = 'Cards\card_discard.wav'; Duration = 0.48; Expr = '0.24*(sin(2*PI*1699*t)*sin(2*PI*2591*t)+sin(2*PI*3463*t)*sin(2*PI*1103*t))*exp(-6.5*t)+0.23*sin(2*PI*(510*t-380*t*t))*exp(-8*t)' },

    @{ Path = 'World\run_start.wav'; Duration = 1.35; Expr = '0.26*sin(2*PI*(72*t+54*t*t))*exp(-1.8*t)+0.22*sin(2*PI*(144*t+108*t*t))*exp(-2.1*t)+0.17*sin(2*PI*(288*t+180*t*t))*exp(-2.7*t)+0.10*sin(2*PI*1840*t)*exp(-10*t)' },
    @{ Path = 'World\door_open.wav'; Duration = 1.20; Expr = '0.30*sin(2*PI*(63*t+22*t*t))*exp(-1.6*t)+0.22*sin(2*PI*421*t)*exp(-3.8*t)+0.18*sin(2*PI*677*t)*exp(-4.5*t)+0.12*sin(2*PI*1931*t)*exp(-10*t)' },
    @{ Path = 'World\turn_commit.wav'; Duration = 0.62; Expr = '0.38*sin(2*PI*(112*t-38*t*t))*exp(-6*t)+0.23*sin(2*PI*347*t)*exp(-9*t)+0.16*sin(2*PI*1279*t)*exp(-19*t)' },
    @{ Path = 'World\dice_roll.wav'; Duration = 0.92; Expr = '0.23*(sin(2*PI*1187*t)*sin(2*PI*1879*t)+sin(2*PI*2777*t)*sin(2*PI*719*t))*(0.55+0.45*sin(2*PI*17*t))*exp(-2.7*t)+0.20*sin(2*PI*176*t)*exp(-4*t)' },
    @{ Path = 'World\player_hit.wav'; Duration = 0.72; Expr = '0.42*sin(2*PI*(79*t-18*t*t))*exp(-5.2*t)+0.25*sin(2*PI*233*t)*exp(-8*t)+0.18*sin(2*PI*731*t)*exp(-13*t)+0.14*sin(2*PI*1597*t)*exp(-19*t)' },
    @{ Path = 'World\heal.wav'; Duration = 1.05; Expr = '0.24*sin(2*PI*(326*t+155*t*t))*exp(-2.6*t)+0.20*sin(2*PI*(489*t+210*t*t))*exp(-3*t)+0.16*sin(2*PI*(652*t+275*t*t))*exp(-3.5*t)' },
    @{ Path = 'World\combat_start.wav'; Duration = 1.18; Expr = '0.34*sin(2*PI*(61*t+11*t*t))*exp(-2.2*t)+0.25*sin(2*PI*183*t)*exp(-3.1*t)+0.19*sin(2*PI*397*t)*exp(-4.2*t)+0.12*sin(2*PI*1421*t)*exp(-9*t)' },
    @{ Path = 'World\enemy_defeat.wav'; Duration = 1.32; Expr = '0.34*sin(2*PI*(146*t-46*t*t))*exp(-2.4*t)+0.25*sin(2*PI*(292*t-92*t*t))*exp(-2.8*t)+0.16*sin(2*PI*857*t)*exp(-6*t)+0.12*sin(2*PI*2143*t)*exp(-10*t)' },
    @{ Path = 'World\treasure_open.wav'; Duration = 1.30; Expr = '0.22*sin(2*PI*(262*t+180*t*t))*exp(-2.4*t)+0.20*sin(2*PI*(524*t+250*t*t))*exp(-2.8*t)+0.17*sin(2*PI*1048*t)*exp(-3.4*t)+0.13*sin(2*PI*2096*t)*exp(-4.2*t)' },
    @{ Path = 'World\event_choice.wav'; Duration = 0.74; Expr = '0.30*sin(2*PI*(247*t+95*t*t))*exp(-4.1*t)+0.23*sin(2*PI*494*t)*exp(-5.2*t)+0.15*sin(2*PI*1223*t)*exp(-8.5*t)' },
    @{ Path = 'World\rest.wav'; Duration = 1.45; Expr = '0.25*sin(2*PI*(196*t+26*t*t))*exp(-1.8*t)+0.21*sin(2*PI*(294*t+34*t*t))*exp(-2.1*t)+0.17*sin(2*PI*(392*t+41*t*t))*exp(-2.5*t)' },
    @{ Path = 'World\curse_accept.wav'; Duration = 1.12; Expr = '0.29*sin(2*PI*(92*t-18*t*t))*exp(-2.8*t)+0.25*sin(2*PI*137*t)*exp(-3.2*t)+0.19*sin(2*PI*271*t)*exp(-4.1*t)+0.13*sin(2*PI*997*t)*exp(-8*t)' },
    @{ Path = 'World\defeat.wav'; Duration = 2.25; Expr = '0.30*sin(2*PI*(118*t-19*t*t))*exp(-1.2*t)+0.24*sin(2*PI*(177*t-28*t*t))*exp(-1.35*t)+0.19*sin(2*PI*(236*t-37*t*t))*exp(-1.55*t)' },
    @{ Path = 'World\victory.wav'; Duration = 2.20; Expr = '0.24*sin(2*PI*(220*t+55*t*t))*exp(-1.1*t)+0.22*sin(2*PI*(277*t+69*t*t))*exp(-1.2*t)+0.20*sin(2*PI*(330*t+82*t*t))*exp(-1.3*t)+0.11*sin(2*PI*1760*t)*exp(-4*t)' },
    @{ Path = 'World\ending.wav'; Duration = 2.85; Expr = '0.23*sin(2*PI*(147*t+24*t*t))*exp(-0.85*t)+0.21*sin(2*PI*(220*t+36*t*t))*exp(-0.95*t)+0.18*sin(2*PI*(294*t+48*t*t))*exp(-1.05*t)+0.12*sin(2*PI*(588*t+65*t*t))*exp(-1.25*t)' },

    @{ Path = 'Rewards\reward_reveal.wav'; Duration = 1.08; Expr = '0.24*sin(2*PI*(392*t+260*t*t))*exp(-2.7*t)+0.20*sin(2*PI*(588*t+310*t*t))*exp(-3.1*t)+0.16*sin(2*PI*1568*t)*exp(-5.5*t)' },
    @{ Path = 'Rewards\reward_claim.wav'; Duration = 0.78; Expr = '0.28*sin(2*PI*(523*t+180*t*t))*exp(-4*t)+0.22*sin(2*PI*784*t)*exp(-5.2*t)+0.16*sin(2*PI*1568*t)*exp(-7.5*t)' },
    @{ Path = 'Rewards\gold_gain.wav'; Duration = 0.68; Expr = '0.25*sin(2*PI*1327*t)*exp(-5*t)+0.22*sin(2*PI*1979*t)*exp(-6.3*t)+0.18*sin(2*PI*2654*t)*exp(-7.8*t)+0.12*sin(2*PI*3981*t)*exp(-11*t)' },
    @{ Path = 'Rewards\purchase.wav'; Duration = 0.82; Expr = '0.27*sin(2*PI*987*t)*exp(-4.6*t)+0.23*sin(2*PI*1480*t)*exp(-5.5*t)+0.18*sin(2*PI*1974*t)*exp(-6.7*t)+0.15*sin(2*PI*2961*t)*exp(-9*t)' },
    @{ Path = 'Rewards\upgrade.wav'; Duration = 1.15; Expr = '0.23*sin(2*PI*(330*t+185*t*t))*exp(-2.5*t)+0.21*sin(2*PI*(495*t+225*t*t))*exp(-2.9*t)+0.18*sin(2*PI*(660*t+280*t*t))*exp(-3.3*t)' },
    @{ Path = 'Rewards\item_equip.wav'; Duration = 0.88; Expr = '0.31*sin(2*PI*(164*t-22*t*t))*exp(-4.5*t)+0.24*sin(2*PI*493*t)*exp(-5.5*t)+0.18*sin(2*PI*985*t)*exp(-7.2*t)+0.12*sin(2*PI*2471*t)*exp(-11*t)' },
    @{ Path = 'Rewards\save_success.wav'; Duration = 0.72; Expr = '0.27*sin(2*PI*(440*t+180*t*t))*exp(-4.2*t)+0.22*sin(2*PI*660*t)*exp(-5.5*t)+0.16*sin(2*PI*1320*t)*exp(-8*t)' },
    @{ Path = 'Rewards\save_failure.wav'; Duration = 0.62; Expr = '0.31*sin(2*PI*164*t)*exp(-5*t)+0.27*sin(2*PI*181*t)*exp(-5.4*t)+0.15*sin(2*PI*737*t)*exp(-11*t)' },
    @{ Path = 'Rewards\load_success.wav'; Duration = 0.84; Expr = '0.25*sin(2*PI*(370*t+150*t*t))*exp(-3.7*t)+0.21*sin(2*PI*(555*t+190*t*t))*exp(-4.3*t)+0.16*sin(2*PI*1110*t)*exp(-7.5*t)' },
    @{ Path = 'Rewards\load_failure.wav'; Duration = 0.66; Expr = '0.30*sin(2*PI*151*t)*exp(-4.8*t)+0.27*sin(2*PI*169*t)*exp(-5.1*t)+0.16*sin(2*PI*677*t)*exp(-10*t)' }
)

foreach ($sound in $sounds) {
    $destination = Join-Path $outputRoot $sound.Path
    $directory = Split-Path -Parent $destination
    New-Item -ItemType Directory -Path $directory -Force | Out-Null

    $duration = [double]$sound.Duration
    $fadeOut = [Math]::Min(0.12, $duration * 0.25)
    $fadeStart = $duration - $fadeOut
    $durationText = $duration.ToString('0.000', $culture)
    $fadeOutText = $fadeOut.ToString('0.000', $culture)
    $fadeStartText = $fadeStart.ToString('0.000', $culture)
    $source = 'aevalsrc={0}:s=48000:d={1}' -f $sound.Expr, $durationText
    $filter = 'highpass=f=35,lowpass=f=15000,afade=t=in:st=0:d=0.004,afade=t=out:st={0}:d={1},volume=1.3,alimiter=limit=0.82:attack=1:release=20:level=false' -f $fadeStartText, $fadeOutText

    & $ffmpeg -hide_banner -loglevel error -y -f lavfi -i $source -af $filter -ac 1 -ar 48000 -c:a pcm_s16le $destination
    if ($LASTEXITCODE -ne 0) {
        throw "FFmpeg failed while generating $($sound.Path)."
    }
}

Write-Host ("Generated {0} original game SFX files in {1}" -f $sounds.Count, $outputRoot)
