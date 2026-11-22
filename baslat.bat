@echo off
chcp 65001 >nul
title PCI DSS Platform
cd /d "%~dp0"

echo === PCI DSS On Degerlendirme Platformu ===
echo.

where dotnet >nul 2>&1 || (echo [HATA] .NET 9 SDK bulunamadi: https://dotnet.microsoft.com/download/dotnet/9.0 & pause & exit /b 1)
where npm >nul 2>&1 || (echo [HATA] Node.js bulunamadi: https://nodejs.org ^(LTS surumu^) & pause & exit /b 1)
where sqllocaldb >nul 2>&1 || (echo [HATA] SQL Server LocalDB bulunamadi. README'deki "Kurulum" bolumune bakin. & pause & exit /b 1)

rem LocalDB ornegi yoksa olusturur, kapaliysa baslatir
sqllocaldb info MSSQLLocalDB >nul 2>&1 || sqllocaldb create MSSQLLocalDB >nul
sqllocaldb start MSSQLLocalDB >nul
if not exist "frontend\node_modules" (
  echo Frontend paketleri kuruluyor, ilk seferde birkac dakika surebilir...
  pushd frontend & call npm install & popd
)

echo Backend baslatiliyor    -^> http://localhost:5000
start "PCI DSS - Backend" cmd /k "cd /d "%~dp0backend\PciDss.API" && dotnet run"
echo Frontend baslatiliyor   -^> http://localhost:5173
start "PCI DSS - Frontend" cmd /k "cd /d "%~dp0frontend" && npm run dev"

rem Backend ilk calistirmada derlenir ve veritabanini olusturur; biraz bekle
timeout /t 20 /nobreak >nul
start http://localhost:5173

echo.
echo Tarayici acildi. Kapatmak icin acilan pencereleri kapatin.
echo Sayfa hata verirse backend penceresinde "Now listening" yazisini bekleyip sayfayi yenileyin.
pause
