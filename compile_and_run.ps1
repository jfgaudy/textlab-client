Write-Host "🔨 COMPILATION ET LANCEMENT DE TEXTLAB..." -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

# Nettoyer
Write-Host "🧹 Nettoyage..." -ForegroundColor Yellow
dotnet clean

# Compiler
Write-Host "📦 Compilation..." -ForegroundColor Yellow
dotnet build

# Vérifier et lancer
if (Test-Path ".\bin\Debug\net8.0-windows\TextLabClient.exe") {
    Write-Host "✅ COMPILATION RÉUSSIE !" -ForegroundColor Green
    Write-Host "🚀 LANCEMENT DE L'APPLICATION..." -ForegroundColor Cyan
    
    Start-Process ".\bin\Debug\net8.0-windows\TextLabClient.exe"
    
    Write-Host ""
    Write-Host "🎉 APPLICATION LANCÉE AVEC INTERFACE ANGLAISE !" -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Green
} else {
    Write-Host "❌ ERREUR : Application non trouvée après compilation !" -ForegroundColor Red
} 