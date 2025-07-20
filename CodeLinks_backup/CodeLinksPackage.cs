using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using CodeLinks.Commands;
using Task = System.Threading.Tasks.Task;

namespace CodeLinks
{
    /// <summary>
    /// CodeLinks VSIX 套件 - 提供 tag/goto 程式碼導航功能
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(CodeLinksPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class CodeLinksPackage : AsyncPackage
    {
        public const string PackageGuidString = "12345678-1234-5678-9abc-123456789012";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            
            System.Diagnostics.Debug.WriteLine("CodeLinks: Package initializing...");
            
            // 初始化跳轉命令
            await JumpCommand.InitializeAsync(this);
            
            System.Diagnostics.Debug.WriteLine("CodeLinks: Package initialized successfully");
        }
    }
}