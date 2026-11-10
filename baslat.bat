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

rem ---------- YZ servisi (Python) ----------
if exist "ai-service\.venv\Scripts\python.exe" goto ai_env
set "PY="
py -3.13 --version >nul 2>&1 && set "PY=py -3.13" && goto ai_venv
py -3.12 --version >nul 2>&1 && set "PY=py -3.12" && goto ai_venv
py -3.11 --version >nul 2>&1 && set "PY=py -3.11" && goto ai_venv
where python >nul 2>&1 && set "PY=python" && goto ai_venv
echo [UYARI] Python bulunamadi ^(3.11-3.13 onerilir: https://www.python.org^). YZ analizi calismayacak.
goto ai_skip
:ai_venv
echo YZ servisi paketleri kuruluyor ^(%PY%^), ilk seferde birkac dakika surebilir...
%PY% -m venv ai-service\.venv
ai-service\.venv\Scripts\python.exe -m pip install -q --disable-pip-version-check -r ai-service\requirements.txt
:ai_env
if exist "ai-service\.env" goto ai_start
copy ai-service\.env.example ai-service\.env >nul
echo [UYARI] ai-service\.env olusturuldu ama GOOGLE_API_KEY bos. Anahtari yazip baslat.bat'i yeniden calistirin.
echo         Anahtar: https://aistudio.google.com/apikey  -- YZ analizi bu calistirmada atlaniyor.
goto ai_skip
:ai_start
findstr /r /c:"^GOOGLE_API_KEY=..*" ai-service\.env >nul || (echo [UYARI] ai-service\.env icinde GOOGLE_API_KEY bos. YZ analizi atlaniyor. & goto ai_skip)
echo YZ servisi baslatiliyor -^> http://localhost:8000
start "PCI DSS - YZ Servisi" cmd /k "cd /d "%~dp0ai-service" && .venv\Scripts\python.exe -m uvicorn app:app --port 8000"
:ai_skip

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
