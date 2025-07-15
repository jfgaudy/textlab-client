Write-Host "Lancement de l'application TextLab..." -ForegroundColor Green

if (Test-Path ".\bin\Release\net8.0-windows\TextLabClient.exe") {
    Write-Host "Application trouvee" -ForegroundColor Green
    Start-Process ".\bin\Release\net8.0-windows\TextLabClient.exe"
    Write-Host "Application lancee - Suivez les instructions :" -ForegroundColor Yellow
    Write-Host "1. Cliquez sur 'Tester la connexion'" -ForegroundColor White
    Write-Host "2. Selectionnez 'PAC_Repo' dans la liste" -ForegroundColor White
    Write-Host "3. Double-cliquez sur un document pour les details" -ForegroundColor White
} else {
    Write-Host "Application non trouvee - Recompilation..." -ForegroundColor Red
    dotnet build -c Release
    if (Test-Path ".\bin\Release\net8.0-windows\TextLabClient.exe") {
        Start-Process ".\bin\Release\net8.0-windows\TextLabClient.exe"
        Write-Host "Application compilee et lancee" -ForegroundColor Green
    }
} 