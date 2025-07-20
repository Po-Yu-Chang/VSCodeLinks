# CodeLinks - Visual Studio 擴充功能

## 功能概覽

CodeLinks 是一個輕量級的 Visual Studio 2022 擴充功能，提供便利的程式碼導航功能：

- 📍 在註解中寫 `// tag:#MySpot` 以宣告「定位點」（藍色標記）
- 🔗 在其他地方寫 `// goto:#MySpot` 生成綠色標記
- 🖱️ **雙擊** `goto` 標記即可跳至對應定位點
- ⚡ 即時更新，無外部相依
- 🎯 純 MEF 架構，穩定可靠

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
   - 按 F5 或選擇 Debug → Start Debugging
   - 這會啟動實驗性 Visual Studio 實例來測試擴充功能
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
    // goto:#方法名稱  ← 雙擊這行可跳轉到上面的 tag
    MyMethod();
}
```

### 範例

查看 `TestFile.cs` 檔案中的完整範例：

```csharp
// tag:#MainMethod
public static void Main(string[] args)
{
    Console.WriteLine("Hello World!");
    // goto:#Helper  ← 雙擊跳轉
}

// tag:#Helper
private static void CallHelper()
{
    Console.WriteLine("Helper method called!");
    // goto:#MainMethod  ← 可以跳回主方法
}
```

### 操作方式

1. **建立標籤**: 使用 `// tag:#標籤名稱` 建立藍色標記
2. **跳轉**: 使用 `// goto:#標籤名稱` 建立綠色標記
3. **導航**: **雙擊** 包含 `goto` 的行即可跳轉

### 功能特色

- **同檔案跳轉**: 在同一個檔案內快速跳轉
- **跨檔案跳轉**: 自動搜尋專案中的其他檔案
- **智慧搜尋**: 自動偵測專案根目錄（.csproj, .sln, .git）
- **即時更新**: 程式碼變更時會自動重新整理標記
- **視覺化提示**: 
  - `tag` 標記顯示為藍色
  - `goto` 標記顯示為綠色
- **簡單穩定**: 純 MEF 架構，無複雜依賴

## 專案結構

```
CodeLinks/
├── Properties/
│   └── AssemblyInfo.cs         # 組件資訊
├── CodeLinks.csproj            # 專案檔
├── source.extension.vsixmanifest  # VSIX 資訊清單
├── UltraSimpleExtension.cs     # 核心實作
└── TestFile.cs                 # 功能測試範例
```

## 技術架構

- **標記器 (Tagger)**: 使用正規表達式識別 `// tag:#` 和 `// goto:#` 模式
- **滑鼠處理器**: 處理雙擊事件觸發跳轉
- **純 MEF 架構**: 使用 Managed Extensibility Framework
- **ITextMarkerTag**: 提供語法高亮功能

### 支援的語言

目前支援所有文字檔案類型：
- C# (.cs 檔案)
- JavaScript (.js 檔案)  
- TypeScript (.ts 檔案)
- 純文字 (.txt 檔案)
- XML (.xml 檔案)
- HTML (.html 檔案)
- 以及其他文字檔案

## 故障排除

### 常見問題

1. **標記沒有顯示**
   - 確保使用正確的語法：`// tag:#標記名稱` 和 `// goto:#標記名稱`
   - 標記名稱必須以字母開頭，只能包含字母、數字和底線

2. **跳轉功能無法運作**
   - 確保目標標記存在於當前檔案或專案中的其他檔案
   - 檢查標記名稱是否完全一致（不區分大小寫）
   - 嘗試雙擊整個 `goto` 行，而不只是部分文字
   - 確保專案結構正確（有 .csproj, .sln 或 .git 檔案）

3. **標記顏色沒有出現**
   - 重新載入檔案或重新啟動 Visual Studio
   - 確保擴充功能已正確安裝並啟用

### 除錯模式

如果需要除錯擴充功能：

1. 在 Visual Studio 中開啟 CodeLinks 專案
2. 按 F5 啟動實驗性實例
3. 在實驗性實例中開啟 `TestFile.cs` 測試功能
4. 查看 Output 視窗的 Debug 訊息

## 版本歷史

- **v1.1.0** (2025-07-20) - 跨檔案跳轉版本
  - 基本的 tag/goto 功能
  - 雙擊跳轉支援
  - 藍色/綠色語法高亮
  - 跨檔案導航功能
  - 智慧專案偵測
  - 純 MEF 架構，穩定可靠

## 授權

此專案採用 MIT 授權 - 詳見 [LICENSE](LICENSE) 檔案

## 參考資料

- [Visual Studio Extensibility Documentation](https://learn.microsoft.com/visualstudio/extensibility)
- [VSSDK Extensibility Samples](https://github.com/Microsoft/VSSDK-Extensibility-Samples)
- [MEF (Managed Extensibility Framework)](https://learn.microsoft.com/dotnet/framework/mef/)