@echo off
rem For a clone of the repo: downloads what src\ ships but git doesn't hold (Node.js, Git Bash, llama.cpp) into
rem src\runtime\. Not the model: the app downloads that on its first run.
rem Run it again any time: it only downloads what's missing, and an interrupted download resumes.
rem Each component the same way: download -> unpack into <dest>.new -> rename to <dest>.
setlocal
cd /d "%~dp0.."
set "CURL=%SystemRoot%\System32\curl.exe"
set "TAR=%SystemRoot%\System32\tar.exe"
set "STAGING=build\downloads"

rem The proxy: curl reads HTTPS_PROXY, but not the Windows proxy setting (e.g. Clash), so copy it there.
set "KEY=HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings"
if defined HTTPS_PROXY goto proxied
reg query "%KEY%" /v ProxyEnable 2>nul | findstr /c:"0x1" >nul || goto proxied
for /f "tokens=3" %%p in ('reg query "%KEY%" /v ProxyServer 2^>nul ^| findstr ProxyServer') do set "SERVER=%%p"
if not defined SERVER goto proxied
rem Not per-protocol ones (http=...;https=...).
echo %SERVER%| findstr /c:"=" >nul && goto proxied
set "HTTPS_PROXY=http://%SERVER%"
echo (using the Windows proxy %SERVER%)
:proxied

rem The components: where each goes, whether to strip its zip's one top folder, its URL(s).
rem A .zip or .7z.exe is unpacked into the destination folder; anything else is saved as the destination file.
rem To upgrade one: change its URL, delete the old copy, run this again.
rem Node.js 24.21.0 LTS (the agent needs 22.18+ to run its .ts files as they are)          ~30 MB
call :get src\runtime\node 1 "https://nodejs.org/dist/v24.21.0/node-v24.21.0-win-x64.zip" || goto failed
rem Git for Windows 2.55.0.5, portable (Git Bash)                                          ~60 MB
call :get src\runtime\git 0 "https://github.com/git-for-windows/git/releases/download/v2.55.0.windows.5/PortableGit-2.55.0.5-64-bit.7z.exe" || goto failed
rem llama.cpp b10434, CUDA 12.4 build + its CUDA runtime (NVIDIA driver with CUDA 12.4+)   ~640 MB
call :get src\runtime\llama 0 "https://github.com/ggml-org/llama.cpp/releases/download/b10434/llama-b10434-bin-win-cuda-12.4-x64.zip" "https://github.com/ggml-org/llama.cpp/releases/download/b10434/cudart-llama-bin-win-cuda-12.4-x64.zip" || goto failed
rmdir "%STAGING%" 2>nul

echo.
echo Done. src\app.cmd starts 2B.
pause
exit /b 0

:failed
echo.
echo Failed; see the message above. Run this again to retry.
pause
exit /b 1

rem ---- call :get DEST STRIP "URL" ["URL"] ----
rem Downloads each URL, unpacks them all into DEST.new, renames that to DEST. Only a finished install is
rem DEST, so an existing one is skipped.
:get
if exist "%~1" exit /b 0
set "DEST=%~1"
set "STRIP=%~2"
echo.
echo == %DEST% ==
for %%u in (%3 %4) do call :download %%u || exit /b 1
if exist "%DEST%.new\" rmdir /s /q "%DEST%.new"
if exist "%DEST%.new" del "%DEST%.new"
for %%d in ("%DEST%") do if not exist "%%~dpd" mkdir "%%~dpd"
for %%u in (%3 %4) do call :unpack %%u || exit /b 1
move "%DEST%.new" "%DEST%" >nul || exit /b 1
for %%u in (%3 %4) do if exist "%STAGING%\%%~nxu" del "%STAGING%\%%~nxu"
echo Ready: %DEST%
exit /b 0

rem A finished download is build\downloads\<file>; an unfinished one is still <file>.part.
:download
set "FILE=%STAGING%\%~nx1"
if exist "%FILE%" exit /b 0
if not exist "%STAGING%" mkdir "%STAGING%"
echo Downloading %~nx1...
rem -C -: resume a partial download; a stalled connection is dropped and retried.
"%CURL%" -L --fail --retry 5 --retry-all-errors -C - --speed-limit 10000 --speed-time 60 --progress-bar -o "%FILE%.part" %1 && goto downloaded
echo Downloading %~nx1 failed; run this again to resume.
exit /b 1
:downloaded
move "%FILE%.part" "%FILE%" >nul
exit /b

:unpack
set "FILE=%STAGING%\%~nx1"
if /i "%FILE:~-4%"==".zip" goto unzip
if /i "%FILE:~-7%"==".7z.exe" goto sfx
move "%FILE%" "%DEST%.new" >nul
exit /b
:unzip
if not exist "%DEST%.new" mkdir "%DEST%.new"
"%TAR%" -xf "%FILE%" -C "%DEST%.new" --strip-components %STRIP%
exit /b
:sfx
rem A self-extracting 7-Zip archive.
"%FILE%" "-o%CD%\%DEST%.new" -y
exit /b
