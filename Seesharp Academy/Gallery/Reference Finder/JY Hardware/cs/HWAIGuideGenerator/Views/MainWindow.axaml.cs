using Avalonia.Controls;
using HWAIGuideGenerator.ViewModels;

namespace HWAIGuideGenerator.Views
{
    /// <summary>
    /// 主窗口代码后置
    /// Main window code-behind
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 窗口加载后设置StorageProvider
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SetStorageProvider(StorageProvider);
            }
        }
    }
}
