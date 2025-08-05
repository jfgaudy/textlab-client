# Script de compilation de la version optimisée

Write-Host "Building optimized TextLab Client..." -ForegroundColor Green

# Arrêter les processus TextLab en cours
Write-Host "Stopping running TextLab processes..." -ForegroundColor Yellow
Get-Process -Name "TextLabClient" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# Nettoyer et compiler
Write-Host "Cleaning and building..." -ForegroundColor Yellow
dotnet clean
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful! Starting optimized application..." -ForegroundColor Green
    Start-Process ".\bin\Debug\net8.0-windows\TextLabClient.exe"
} else {
    Write-Host "Build failed!" -ForegroundColor Red
} 