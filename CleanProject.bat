@echo off
echo =====================================
echo 清理 CodeLinks 專案目錄
echo =====================================

cd /d "%~dp0"

echo 正在清理專案目錄...

REM 刪除 CodeLinks 中的舊檔案和資料夾
echo 1. 刪除建置輸出
if exist "CodeLinks\bin" rmdir /s /q "CodeLinks\bin"
if exist "CodeLinks\obj" rmdir /s /q "CodeLinks\obj"

echo 2. 刪除舊的 Navigation 資料夾
if exist "CodeLinks\Navigation" rmdir /s /q "CodeLinks\Navigation"

echo 3. 刪除舊的 Tagging 資料夾  
if exist "CodeLinks\Tagging" rmdir /s /q "CodeLinks\Tagging"

echo 4. 刪除多餘的套件載入器
if exist "CodeLinks\ForcePackageLoader.cs" del "CodeLinks\ForcePackageLoader.cs"

echo 5. 刪除多餘的命令檔案
if exist "CodeLinks\Commands\JumpToTag.vsct" del "CodeLinks\Commands\JumpToTag.vsct"
if exist "CodeLinks\Commands\JumpToTagCommand.cs" del "CodeLinks\Commands\JumpToTagCommand.cs"
if exist "CodeLinks\Commands\SimpleJumpCommand.cs" del "CodeLinks\Commands\SimpleJumpCommand.cs"
if exist "CodeLinks\Commands\SimpleRightClickJump.cs" del "CodeLinks\Commands\SimpleRightClickJump.cs"
if exist "CodeLinks\Commands\SuperSimpleJumpCommand.cs" del "CodeLinks\Commands\SuperSimpleJumpCommand.cs"
if exist "CodeLinks\Commands\TestCommand.cs" del "CodeLinks\Commands\TestCommand.cs"

echo 6. 刪除 .vs 資料夾
if exist ".vs" rmdir /s /q ".vs"

echo 7. 刪除 .claude 資料夾
if exist ".claude" rmdir /s /q ".claude"

echo.
echo =====================================
echo 清理完成！
echo.
echo 保留的重要檔案：
echo ✓ CodeLinks\CodeLinks.csproj
echo ✓ CodeLinks\source.extension.vsixmanifest  
echo ✓ CodeLinks\CodeLinksPackage.cs
echo ✓ CodeLinks\CodeLinksPackage.vsct
echo ✓ CodeLinks\Tagger\CodeLinkTagger.cs
echo ✓ CodeLinks\Classifier\CodeLinkClassifier.cs
echo ✓ CodeLinks\Commands\JumpCommand.cs
echo ✓ CodeLinks\Properties\AssemblyInfo.cs
echo ✓ CodeLinks\Resources\Icon.png
echo ✓ TestSolution\ (測試專案)
echo ✓ Build.bat (建構腳本)
echo ✓ README.md (說明文件)
echo ✓ Requirement.md (需求文件)
echo ✓ .git\ (Git 版本控制)
echo.
echo 現在專案目錄已經整理乾淨！
echo 可以執行 Build.bat 來建構專案。
echo =====================================

pause
