# CodeLinks - Visual Studio 擴充功能

## 功能概覽

CodeLinks 是一個 Visual Studio 2022 擴充功能，提供便利的程式碼導航功能：

- 📍 在註解中寫 `// tag:#MySpot` 以宣告「定位點」
- 🔗 在其他地方寫 `// goto:#MySpot` 生成可點擊的藍色箭頭條
- 🖱️ Click / Ctrl+Click 即可跳至對應定位點
- 🔍 支援跨檔案 / 跨專案搜尋
- ⚡ 即時更新，無外部相依

## 安裝需求

- Visual Studio 2022 (17.0 或更高版本)
- .NET Framework 4.7.2 或更高版本

## 建置與安裝

### 從原始碼建置

1. **複製儲存庫**
   ```bash
   git clone https://github.com/Po-Yu-Chang/VSCodeLinks.git
   cd VSCodeLinks/CodeLinks
   ```

2. **在 Visual Studio 中開啟**
   - 開啟 `CodeLinks.csproj`
   - 確保已安裝 "Visual Studio extension development" 工作負載

3. **建置專案**
   - 按 F6 或選擇 Build → Build Solution
   - 建置完成後會在 `bin\Debug\` 資料夾中產生 `CodeLinks.vsix`

4. **安裝擴充功能**
   - 關閉所有 Visual Studio 實例
   - 雙擊 `CodeLinks.vsix` 檔案
   - 按照安裝精靈的指示完成安裝
   - 重新啟動 Visual Studio

## 使用方法

### 基本語法

```csharp
// tag:#方法名稱
public void MyMethod()
{
    // 你的程式碼
}

public void AnotherMethod()
{
    // goto:#方法名稱  ← 這裡會出現藍色箭頭，點擊可跳轉
    MyMethod();
}
```

### 範例

查看 `Demo.cs` 檔案中的完整範例：

```csharp
// tag:#MainMethod
public static void Main(string[] args)
{
    Console.WriteLine("Hello World!");
    
    // goto:#HelperMethod  ← 點擊藍色箭頭跳轉
    HelperMethod();
}

// tag:#HelperMethod
private static void HelperMethod()
{
    Console.WriteLine("Helper method called!");
    
    // goto:#MainMethod  ← 可以跳回主方法
}
```

### 功能特色

- **跨檔案支援**: 可以在不同的 .cs 檔案之間跳轉
- **即時更新**: 程式碼變更時會自動重新整理標記
- **視覺化提示**: goto 標記會在右側邊緣顯示藍色箭頭
- **工具提示**: 滑鼠懸停時顯示目標標記資訊

## 開發資訊

### 專案結構

```
CodeLinks/
├── Classification/           # 分類定義和格式
│   └── CodeLinkClassificationDefinition.cs
├── Tagging/                 # 標記識別和處理
│   ├── CodeLinkTag.cs
│   └── CodeLinkTagger.cs
├── Navigation/              # 導航和 UI
│   ├── CodeLinkMargin.cs
│   └── CodeLinkJumpService.cs
├── Properties/
│   └── AssemblyInfo.cs
├── CodeLinks.csproj
├── source.extension.vsixmanifest
└── Demo.cs                  # 功能展示範例
```

### 技術架構

- **標記器 (Tagger)**: 使用正規表達式識別 `// tag:#` 和 `// goto:#` 模式
- **分類器 (Classifier)**: 定義標記的視覺樣式
- **邊緣 (Margin)**: 在編輯器右側顯示可點擊的藍色箭頭
- **跳轉服務**: 使用 DTE API 搜尋目標標記並執行跳轉

### 支援的語言

目前支援：
- C# (.cs 檔案)

未來可能支援：
- Visual Basic (.vb 檔案)
- F# (.fs 檔案)
- 其他程式語言

## 故障排除

### 常見問題

1. **標記沒有顯示**
   - 確保使用正確的語法：`// tag:#標記名稱` 和 `// goto:#標記名稱`
   - 標記名稱必須以字母開頭，只能包含字母、數字和底線

2. **跳轉功能無法運作**
   - 確保目標標記存在於專案中的任何 .cs 檔案
   - 檢查標記名稱是否完全一致（區分大小寫）

3. **藍色箭頭沒有出現**
   - 確保正在編輯 C# 檔案
   - 嘗試重新載入檔案或重新啟動 Visual Studio

### 除錯模式

如果需要除錯擴充功能：

1. 在 Visual Studio 中開啟 CodeLinks 專案
2. 按 F5 啟動實驗性實例
3. 在實驗性實例中測試功能

## 貢獻

歡迎貢獻此專案！請：

1. Fork 此儲存庫
2. 建立功能分支
3. 提交您的變更
4. 建立 Pull Request

## 授權

此專案採用 MIT 授權 - 詳見 [LICENSE](LICENSE) 檔案

## 作者

- **Your Name** - 初始開發

## 版本歷史

- **v1.0.0** (2025-07-19) - 初始版本
  - 基本的 tag/goto 功能
  - 跨檔案跳轉支援
  - 藍色箭頭視覺提示

## 參考資料

- [Visual Studio Extensibility Documentation](https://learn.microsoft.com/visualstudio/extensibility)
- [VSSDK Extensibility Samples](https://github.com/Microsoft/VSSDK-Extensibility-Samples)
- [Roslyn Syntax API](https://learn.microsoft.com/dotnet/csharp/roslyn-sdk)
