@echo off
echo ======================================
echo Git 重置到初始狀態
echo ======================================

cd /d "%~dp0"

echo 1. 顯示目前 Git 歷史
git log --oneline -5

echo.
echo 2. 重置到第一個提交（保留需求文件）
git reset --hard 88c87b1

echo.
echo 3. 清理所有未追蹤檔案
git clean -fd

echo.
echo 4. 刪除建置輸出資料夾
if exist "CodeLinks\bin" rmdir /s /q "CodeLinks\bin"
if exist "CodeLinks\obj" rmdir /s /q "CodeLinks\obj"
if exist ".vs" rmdir /s /q ".vs"

echo.
echo 5. 檢查 Git 狀態
git status

echo.
echo 6. 顯示目前檔案
dir

echo.
echo ======================================
echo Git 重置完成！
echo 現在請閱讀 CORRECT_IMPLEMENTATION_GUIDE.md
echo 按照指南重新建立正確的 Visual Studio 擴充套件
echo ======================================
pause
