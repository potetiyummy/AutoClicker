param(
    [string]$Version = "1.15.3"
)

# 配布用の zip を作る（32 ビットと 64 ビット）
# 中身：autoclicker.exe と AutoClicker.dll（同じフォルダに置く）
# 出力先：リポジトリ直下の dist フォルダ
#   AutoClicker-v<Version>_64bit.zip
#   AutoClicker-v<Version>_32bit.zip
$ErrorActionPreference = "Stop"

$csharp = $PSScriptRoot
$dist = Join-Path (Split-Path $csharp -Parent) "dist"
New-Item -ItemType Directory -Force $dist | Out-Null

# 1. 本体の dll（32 ビットと 64 ビットで共通）
dotnet build (Join-Path $csharp "AutoClicker.csproj") -c Release | Out-Null
if ($LASTEXITCODE -ne 0) { throw "本体のビルドに失敗しました" }
$dll = Join-Path $csharp "bin\Release\net8.0-windows\AutoClicker.dll"

foreach ($arch in @("x64", "x86")) {
    $bits = if ($arch -eq "x64") { "64" } else { "32" }
    $folder = Join-Path $dist ("AutoClicker-v" + $Version + "_" + $bits + "bit")
    Remove-Item -Recurse -Force $folder -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force $folder | Out-Null

    # 2. 起動用の exe（単体）と、本体の dll を同じフォルダに置く
    dotnet publish (Join-Path $csharp "Launcher\AutoClicker.Launcher.csproj") -c Release -r "win-$arch" -o $folder | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "起動用 exe（$arch）のビルドに失敗しました" }
    Copy-Item $dll $folder -Force

    # 3. zip にする
    $zip = Join-Path $dist ("AutoClicker-v" + $Version + "_" + $bits + "bit.zip")
    Remove-Item $zip -ErrorAction SilentlyContinue
    Compress-Archive -Path (Join-Path $folder "*") -DestinationPath $zip
    Write-Output ("作成: " + $zip)
}
