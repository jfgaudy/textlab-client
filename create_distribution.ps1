# Script de création d'une distribution TextLab Client pour Vitor
# Usage: .\create_distribution.ps1

Write-Host "Creating TextLab Client Distribution Package..." -ForegroundColor Green

# Nettoyer et compiler en Release
Write-Host "`nBuilding Release version..." -ForegroundColor Yellow
dotnet clean -c Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# Créer le dossier de distribution
$distFolder = "TextLabClient_Distribution"
if (Test-Path $distFolder) {
    Remove-Item $distFolder -Recurse -Force
}
New-Item -ItemType Directory -Path $distFolder

# Copier l'exécutable
Write-Host "`nCreating distribution folder..." -ForegroundColor Yellow
Copy-Item "bin\Release\net8.0-windows\win-x64\publish\TextLabClient.exe" "$distFolder\"

# Créer un README pour Vitor
$readmeContent = @"
# TextLab Client - Distribution Package

## Installation Instructions

### System Requirements
- Windows 10/11 (64-bit)
- No additional software required (self-contained)

### Installation
1. Download the distribution folder
2. Extract to your desired location
3. Run TextLabClient.exe

## Features
- Modern Windows interface in English
- Multi-repository management
- Document visualization and editing
- Git synchronization
- API connection management

## Configuration
1. Launch the application
2. Click "Connect" to authenticate
3. Configure your API URL (default: http://localhost:8000)
4. Add your repositories through "Repository Management"

## Support
For technical support, contact the development team.

---
**Version:** 2.0  
**Build Date:** $(Get-Date -Format 'yyyy-MM-dd HH:mm')  
**Target:** Windows x64 (Self-contained)
"@

Set-Content -Path "$distFolder\README.md" -Value $readmeContent -Encoding UTF8

# Créer un script de lancement optionnel
$launchScript = @"
@echo off
title TextLab Client
echo Starting TextLab Client...
start TextLabClient.exe
"@

Set-Content -Path "$distFolder\Launch_TextLabClient.bat" -Value $launchScript -Encoding ASCII

# Afficher les informations de la distribution
Write-Host "`nDistribution created successfully!" -ForegroundColor Green
Write-Host "`nPackage Information:" -ForegroundColor Cyan

$exeFile = Get-ChildItem "$distFolder\TextLabClient.exe"
$sizeMB = [math]::Round($exeFile.Length/1MB, 2)

Write-Host "   Folder: $distFolder" -ForegroundColor White
Write-Host "   Main executable: TextLabClient.exe ($sizeMB MB)" -ForegroundColor White
Write-Host "   Documentation: README.md" -ForegroundColor White
Write-Host "   Launcher: Launch_TextLabClient.bat" -ForegroundColor White

Write-Host "`nNext Steps:" -ForegroundColor Cyan
Write-Host "   1. Compress the '$distFolder' folder into a ZIP file" -ForegroundColor White
Write-Host "   2. Send the ZIP file to Vitor" -ForegroundColor White
Write-Host "   3. Vitor can extract and run TextLabClient.exe directly" -ForegroundColor White

# Créer automatiquement le ZIP
Write-Host "`nCreating ZIP archive..." -ForegroundColor Yellow
$zipPath = "TextLabClient_Distribution.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path $distFolder -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "`nZIP archive created: $zipPath" -ForegroundColor Green
$zipFile = Get-ChildItem $zipPath
$zipSizeMB = [math]::Round($zipFile.Length/1MB, 2)
Write-Host "   Archive size: $zipSizeMB MB" -ForegroundColor White

Write-Host "`nReady to send to Vitor!" -ForegroundColor Green 