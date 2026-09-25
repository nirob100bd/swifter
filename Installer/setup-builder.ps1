param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release",
    [string]$OutputDir = "publish"
)

$ErrorActionPreference = "Stop"
$RootDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$PublishDir = Join-Path $RootDir $OutputDir
$InstallerDir = Join-Path $RootDir "Installer"
$OutputExe = Join-Path $PublishDir "Swifter_Setup_v$Version.exe"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Swifter Setup Builder v$Version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/5] Publishing application..." -ForegroundColor Yellow
dotnet publish "$RootDir\Swifter.csproj" `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o "$PublishDir\app"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed!"
    exit 1
}

Write-Host "[2/5] Preparing installer assets..." -ForegroundColor Yellow
$AssetsDir = Join-Path $PublishDir "app\Assets"
if (-not (Test-Path $AssetsDir)) {
    New-Item -ItemType Directory -Path $AssetsDir -Force | Out-Null
}

$LogoSrc = Join-Path $RootDir "Assets\logo.png"
$IcoSrc = Join-Path $RootDir "Assets\app.ico"
if (Test-Path $LogoSrc) { Copy-Item $LogoSrc "$AssetsDir\logo.png" -Force }
if (Test-Path $IcoSrc) { Copy-Item $IcoSrc "$AssetsDir\app.ico" -Force }

Write-Host "[3/5] Creating Inno Setup script..." -ForegroundColor Yellow
$InnoScript = @"
[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName=Swifter Browser
AppVersion=$Version
AppPublisher=nirob100bd
AppPublisherURL=https://github.com/nirob100bd/swifter
DefaultDirName={autopf}\Swifter
DefaultGroupName=Swifter Browser
AllowNoIcons=yes
OutputDir=$PublishDir
OutputBaseFilename=Swifter_Setup_v$Version
SetupIconFile=$RootDir\Assets\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
UninstallDisplayName=Swifter Browser
VersionInfoVersion=$Version.0
VersionInfoCompany=nirob100bd
VersionInfoDescription=Swifter Browser Installer
VersionInfoProductName=Swifter Browser

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"
Name: "startmenu"; Description: "Create Start Menu shortcuts"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "$PublishDir\app\Swifter.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "$PublishDir\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Swifter Browser"; Filename: "{app}\Swifter.exe"
Name: "{group}\Uninstall Swifter"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Swifter Browser"; Filename: "{app}\Swifter.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Swifter.exe"; Description: "Launch Swifter Browser"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\Swifter"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"
Root: HKCU; Subkey: "Software\Swifter"; ValueType: string; ValueName: "Version"; ValueData: "$Version"
"@

$InnoScriptPath = Join-Path $InstallerDir "SwifterSetup.iss"
Set-Content -Path $InnoScriptPath -Value $InnoScript -Encoding UTF8

Write-Host "[4/5] Building installer..." -ForegroundColor Yellow
$InnoCompiler = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $InnoCompiler) {
    Write-Host "[4/5] Inno Setup not found, creating portable ZIP instead..." -ForegroundColor Yellow
    $ZipPath = Join-Path $PublishDir "Swifter_v$Version_Portable.zip"
    Compress-Archive -Path "$PublishDir\app\*" -DestinationPath $ZipPath -Force
    Write-Host "Created portable package: $ZipPath" -ForegroundColor Green
    $OutputExe = $ZipPath
} else {
    & $InnoCompiler $InnoScriptPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Installer build failed!"
        exit 1
    }
}

Write-Host "[5/5] Verifying output..." -ForegroundColor Yellow
if (Test-Path $OutputExe) {
    $Size = (Get-Item $OutputExe).Length / 1MB
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Output: $OutputExe" -ForegroundColor White
    Write-Host "  Size:   $([math]::Round($Size, 2)) MB" -ForegroundColor White
    Write-Host "========================================" -ForegroundColor Green
} else {
    Write-Error "Output file not found: $OutputExe"
    exit 1
}