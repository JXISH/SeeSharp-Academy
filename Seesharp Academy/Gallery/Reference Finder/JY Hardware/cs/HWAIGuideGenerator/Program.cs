using System;
using Avalonia;

namespace HWAIGuideGenerator
{
    /// <summary>
    /// 程序入口点
    /// Program entry point
    /// </summary>
    internal sealed class Program
    {
        // 初始化代码，在Main之前运行，不要使用任何Avalonia,第三方API或任何SynchronizationContext依赖代码
        // Initialization code. Don't use any Avalonia, third-party APIs or any SynchronizationContext-reliant code before AppMain is called
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia配置，这里不要订阅任何事件或添加日志记录
        // Avalonia configuration, don't remove; also used by visual designer
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
