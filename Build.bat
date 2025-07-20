@echo off
echo =====================================
echo 建構 CodeLinks Visual Studio 擴充套件
echo =====================================

cd /d "%~dp0"

echo 1. 檢查專案檔案
if not exist "CodeLinks\CodeLinks.csproj" (
    echo 錯誤: 找不到 CodeLinks.csproj 檔案
    pause
    exit /b 1
)

echo 2. 清理舊建置輸出
if exist "CodeLinks\bin" rmdir /s /q "CodeLinks\bin"
if exist "CodeLinks\obj" rmdir /s /q "CodeLinks\obj"

echo 3. 建構專案
MSBuild CodeLinks\CodeLinks.csproj /p:Configuration=Debug /p:Platform="Any CPU" /t:Build

if %ERRORLEVEL% EQU 0 (
    echo.
    echo =====================================
    echo 建構成功！
    echo VSIX 檔案位置: CodeLinks\bin\Debug\CodeLinks.vsix
    echo =====================================
    echo.
    echo 現在您可以：
    echo 1. 雙擊 VSIX 檔案安裝擴充套件
    echo 2. 或按 F5 在 Visual Studio 中偵錯
    echo.
) else (
    echo.
    echo =====================================
    echo 建構失敗！請檢查錯誤訊息
    echo =====================================
)

pause
