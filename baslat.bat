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
rem Yerel dil modeli bellegi doldurdugunda veritabani sorgulari bellek bekleyip yavaslamasin diye
rem LocalDB'nin elinde en az 512 MB tutmasi saglanir (sqlcmd yoksa atlanir)
where sqlcmd >nul 2>&1 && sqlcmd -S "(localdb)\MSSQLLocalDB" -E -b -Q "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'min server memory (MB)', 512; RECONFIGURE;" >nul 2>&1

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
rem Mevcut sanal ortamda yeni paket eksikse (ornegin Gemini'den yerel modele gecis) yeniden kur
ai-service\.venv\Scripts\python.exe -c "import langchain_ollama" >nul 2>&1 || ai-service\.venv\Scripts\python.exe -m pip install -q --disable-pip-version-check -r ai-service\requirements.txt

rem Yerel dil modeli (Ollama). API anahtari gerekmez; belgeler bilgisayardan cikmaz.
set "AI_MODE=rules"
set "OLLAMA="
where ollama >nul 2>&1 && set "OLLAMA=ollama"
if not defined OLLAMA if exist "%LOCALAPPDATA%\Programs\Ollama\ollama.exe" set "OLLAMA=%LOCALAPPDATA%\Programs\Ollama\ollama.exe"
if not defined OLLAMA (
  echo [UYARI] Ollama bulunamadi: https://ollama.com/download
  echo         YZ servisi dil modeli olmadan, yalnizca kural moduyla calisacak.
  goto ai_start
)
curl -s -m 3 http://127.0.0.1:11434/api/version >nul 2>&1 || (start "Ollama" /min "%OLLAMA%" serve & timeout /t 6 /nobreak >nul)
rem Model adlari ai-service\.env icinde degistirildiyse onlari elle indirin (ollama pull ...)
"%OLLAMA%" list | findstr /c:"gemma3:4b" >nul || (echo Dil modeli indiriliyor, ilk seferde ~3.4 GB... & "%OLLAMA%" pull gemma3:4b)
"%OLLAMA%" list | findstr /c:"bge-m3" >nul || (echo Embedding modeli indiriliyor, ilk seferde ~1.2 GB... & "%OLLAMA%" pull bge-m3)
set "AI_MODE=hybrid"
:ai_start
echo YZ servisi baslatiliyor ^(%AI_MODE%^) -^> http://localhost:8000
start "PCI DSS - YZ Servisi" cmd /k "cd /d "%~dp0ai-service" && set ANALYSIS_MODE=%AI_MODE%&& .venv\Scripts\python.exe -m uvicorn app:app --port 8000"
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
