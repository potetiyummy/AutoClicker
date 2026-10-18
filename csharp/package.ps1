param(
    [string]$Version = "1.14"
)

# 配布用のインストーラーを作る（32 ビットと 64 ビット）
# 出力先：リポジトリ直下の dist フォルダ
#   AutoClicker-v<Version>_installer_64bit.exe
#   AutoClicker-v<Version>_installer_32bit.exe
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
    $payload = Join-Path $dist "payload-$arch"
    New-Item -ItemType Directory -Force $payload | Out-Null

    # 2. 起動用の exe（単体）と、本体の dll を、インストーラーに入れる材料にする
    dotnet publish (Join-Path $csharp "Launcher\AutoClicker.Launcher.csproj") -c Release -r "win-$arch" -o $payload | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "起動用 exe（$arch）のビルドに失敗しました" }
    Copy-Item $dll $payload -Force

    # 3. インストーラー（材料を埋め込んで、1 つの exe にする）
    $installOut = Join-Path $dist "installer-$arch"
    dotnet publish (Join-Path $csharp "Installer\AutoClicker.Installer.csproj") -c Release -r "win-$arch" -p:PayloadDir=$payload -o $installOut | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "インストーラー（$arch）のビルドに失敗しました" }

    $name = "AutoClicker-v" + $Version + "_installer_" + $bits + "bit.exe"
    Copy-Item (Join-Path $installOut "AutoClicker-Installer.exe") (Join-Path $dist $name) -Force
    Write-Output ("作成: " + (Join-Path $dist $name))
}
