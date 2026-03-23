using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HWAIGuideGenerator.Models;
using HWAIGuideGenerator.Services;

namespace HWAIGuideGenerator.ViewModels
{
    /// <summary>
    /// 主窗口ViewModel
    /// Main window view model containing all business logic
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        // Services
        private readonly ExcelService _excelService;
        private readonly FileSearchService _fileSearchService;
        private readonly HistoryService _historyService;
        private readonly PromptGeneratorService _promptGeneratorService;

        // 存储Avalonia的StorageProvider用于文件对话框
        private IStorageProvider? _storageProvider;

        // 当前搜索结果
        private SearchResult? _currentSearchResult;

        #region 可绑定属性

        /// <summary>
        /// JYPEDIA文件路径
        /// </summary>
        [ObservableProperty]
        private string _jypediaFilePath = string.Empty;

        /// <summary>
        /// JYPEDIA文件路径历史记录
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _jypediaFilePathHistory = new();

        /// <summary>
        /// 选中的操作系统
        /// </summary>
        [ObservableProperty]
        private OperatingSystemType _selectedOperatingSystem;

        /// <summary>
        /// 操作系统选项
        /// </summary>
        public List<OperatingSystemType> OperatingSystemOptions { get; } = new()
        {
            OperatingSystemType.Windows,
            OperatingSystemType.Linux
        };

        /// <summary>
        /// 硬件型号
        /// </summary>
        [ObservableProperty]
        private string _hardwareModel = string.Empty;

        /// <summary>
        /// 硬件型号历史记录
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _hardwareModelHistory = new();

        /// <summary>
        /// 是否复制驱动
        /// </summary>
        [ObservableProperty]
        private bool _copyDriver;

        /// <summary>
        /// 目标驱动目录
        /// </summary>
        [ObservableProperty]
        private string _targetDriverDirectory = string.Empty;

        /// <summary>
        /// 驱动目录历史记录
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _driverDirectoryHistory = new();

        /// <summary>
        /// 范例树
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ExampleTreeNodeViewModel> _exampleTree = new();

        /// <summary>
        /// 输出文本(搜索结果和生成的训导词)
        /// </summary>
        [ObservableProperty]
        private string _outputText = string.Empty;

        /// <summary>
        /// 状态消息
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = "就绪";

        /// <summary>
        /// 是否正在处理
        /// </summary>
        [ObservableProperty]
        private bool _isProcessing;

        /// <summary>
        /// 是否已完成搜索
        /// </summary>
        [ObservableProperty]
        private bool _hasSearchResult;

        /// <summary>
        /// 是否有范例树数据
        /// </summary>
        [ObservableProperty]
        private bool _hasExampleTree;

        /// <summary>
        /// 范例状态消息
        /// </summary>
        [ObservableProperty]
        private string _exampleStatusMessage = "请先执行查找操作";

        /// <summary>
        /// 输出历史记录
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<HistoryOutputRecord> _outputHistory = new();

        /// <summary>
        /// 选中的历史输出记录
        /// </summary>
        [ObservableProperty]
        private HistoryOutputRecord? _selectedOutputHistory;

        #endregion

        public MainWindowViewModel()
        {
            _excelService = new ExcelService();
            _fileSearchService = new FileSearchService();
            _historyService = new HistoryService();
            _promptGeneratorService = new PromptGeneratorService();

            // 初始化当前操作系统
            SelectedOperatingSystem = _fileSearchService.GetCurrentOperatingSystem();

            // 加载历史记录
            LoadHistoryData();
        }

        /// <summary>
        /// 设置StorageProvider用于文件对话框
        /// </summary>
        public void SetStorageProvider(IStorageProvider storageProvider)
        {
            _storageProvider = storageProvider;
        }

        /// <summary>
        /// 加载历史数据
        /// </summary>
        private void LoadHistoryData()
        {
            JypediaFilePathHistory = new ObservableCollection<string>(_historyService.GetJypediaFilePathHistory());
            HardwareModelHistory = new ObservableCollection<string>(_historyService.GetHardwareModelHistory());
            DriverDirectoryHistory = new ObservableCollection<string>(_historyService.GetDriverDirectoryHistory());
            OutputHistory = new ObservableCollection<HistoryOutputRecord>(_historyService.GetOutputRecordHistory());
        }

        /// <summary>
        /// 当选中历史输出记录变化时
        /// </summary>
        partial void OnSelectedOutputHistoryChanged(HistoryOutputRecord? value)
        {
            if (value != null)
            {
                OutputText = value.GeneratedPrompt;
            }
        }

        #region 命令

        /// <summary>
        /// 浏览JYPEDIA文件
        /// </summary>
        [RelayCommand]
        private async Task BrowseJypediaFileAsync()
        {
            if (_storageProvider == null) return;

            var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择JYPEDIA Excel文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Excel文件") { Patterns = new[] { "*.xlsx", "*.xls" } },
                    new FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count > 0)
            {
                JypediaFilePath = files[0].Path.LocalPath;
            }
        }

        /// <summary>
        /// 浏览驱动目标目录
        /// </summary>
        [RelayCommand]
        private async Task BrowseDriverDirectoryAsync()
        {
            if (_storageProvider == null) return;

            var folders = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择驱动目标目录",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                TargetDriverDirectory = folders[0].Path.LocalPath;
            }
        }

        /// <summary>
        /// 查找按钮命令
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(JypediaFilePath))
            {
                StatusMessage = "请选择JYPEDIA文件";
                return;
            }

            if (string.IsNullOrWhiteSpace(HardwareModel))
            {
                StatusMessage = "请输入硬件型号";
                return;
            }

            IsProcessing = true;
            HasSearchResult = false;
            HasExampleTree = false;
            ExampleStatusMessage = "正在搜索...";
            StatusMessage = "正在查找...";
            ExampleTree.Clear();
            OutputText = string.Empty;

            try
            {
                await Task.Run(() => PerformSearch());
            }
            catch (Exception ex)
            {
                StatusMessage = $"查找出错: {ex.Message}";
                OutputText = $"错误: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 执行搜索
        /// </summary>
        private void PerformSearch()
        {
            // 提取型号数值(4-5位连续数字)
            string modelNumber = ExtractModelNumber(HardwareModel);
            if (string.IsNullOrEmpty(modelNumber))
            {
                StatusMessage = "无法从硬件型号中提取数字";
                return;
            }

            // 读取JYPEDIA文件
            var jypediaRows = _excelService.ReadJypediaFile(JypediaFilePath);

            // 查找驱动
            var driverResult = SearchDriver(jypediaRows, modelNumber);
            if (driverResult == null)
            {
                StatusMessage = "未找到匹配的驱动";
                return;
            }

            // 查找范例
            var exampleResult = SearchExample(jypediaRows, modelNumber);

            // 创建搜索结果
            _currentSearchResult = new SearchResult
            {
                HardwareModel = HardwareModel,
                ModelNumber = modelNumber,
                OperatingSystem = SelectedOperatingSystem,
                Driver = driverResult,
                Example = exampleResult ?? new ExampleSearchResult()
            };

            // 保存历史记录
            _historyService.AddJypediaFilePath(JypediaFilePath);
            _historyService.AddHardwareModel(HardwareModel);

            // 更新UI(需要在UI线程执行)
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // 更新历史记录显示
                if (!JypediaFilePathHistory.Contains(JypediaFilePath))
                {
                    JypediaFilePathHistory.Insert(0, JypediaFilePath);
                }
                if (!HardwareModelHistory.Contains(HardwareModel))
                {
                    HardwareModelHistory.Insert(0, HardwareModel);
                }

                // 生成并显示搜索结果摘要
                OutputText = _promptGeneratorService.GenerateSearchResultSummary(_currentSearchResult);

                // 构建范例树
                if (exampleResult != null && exampleResult.IsValid)
                {
                    var treeNodes = _fileSearchService.GetExampleTree(exampleResult.ExampleDirectory);
                    foreach (var node in treeNodes)
                    {
                        ExampleTree.Add(new ExampleTreeNodeViewModel(node));
                    }
                    
                    if (ExampleTree.Count > 0)
                    {
                        HasExampleTree = true;
                        ExampleStatusMessage = string.Empty;
                    }
                    else
                    {
                        HasExampleTree = false;
                        ExampleStatusMessage = $"范例目录下没有子目录\n{exampleResult.ExampleDirectory}";
                    }
                }
                else
                {
                    HasExampleTree = false;
                    ExampleStatusMessage = "未在JYPEDIA中找到匹配的C#范例\n请检查硬件型号是否正确";
                }

                HasSearchResult = true;
                StatusMessage = "查找完成";
            });
        }

        /// <summary>
        /// 搜索驱动
        /// </summary>
        private DriverSearchResult? SearchDriver(List<JypediaRow> rows, string modelNumber)
        {
            // 筛选驱动行
            var driverRows = _excelService.FilterDriverRows(rows, modelNumber, SelectedOperatingSystem);
            
            if (driverRows.Count == 0)
            {
                return null;
            }

            // 使用第一个匹配的驱动(如有多个可以后续弹窗选择)
            var driverRow = driverRows.First();
            var (driverName, compactDriverName) = _excelService.ExtractDriverName(driverRow.FileName);

            // 搜索驱动目录
            string driverRoot = _fileSearchService.GetDriverRootDirectory(SelectedOperatingSystem);
            var driverDirs = _fileSearchService.SearchDriverDirectories(driverRoot, compactDriverName);

            if (driverDirs.Count == 0)
            {
                return null;
            }

            string driverDir = driverDirs.First();

            // 搜索DLL文件
            var dllFiles = _fileSearchService.SearchDriverDllFiles(driverDir, modelNumber);
            if (dllFiles.Count == 0)
            {
                return null;
            }

            // 搜索XML文件
            var xmlFiles = _fileSearchService.SearchDriverXmlFiles(driverDir, dllFiles);

            return new DriverSearchResult
            {
                DriverDirectory = driverDir,
                DriverName = driverName,
                CompactDriverName = compactDriverName,
                DllFiles = dllFiles,
                XmlFiles = xmlFiles
            };
        }

        /// <summary>
        /// 搜索范例 - 直接搜索包含型号数值和"Example"的目录
        /// Searches for example directories containing model number and "Example"
        /// </summary>
        private ExampleSearchResult? SearchExample(List<JypediaRow> rows, string modelNumber)
        {
            // 直接搜索包含型号数值和"Example"的目录
            var exampleDirs = _fileSearchService.SearchExampleDirectoriesByModelNumber(SelectedOperatingSystem, modelNumber);

            if (exampleDirs.Count == 0)
            {
                return null;
            }

            // 使用第一个找到的目录(深度最浅的)
            string exampleDir = exampleDirs.First();

            return new ExampleSearchResult
            {
                ExampleDirectory = exampleDir,
                DirectoryName = Path.GetFileName(exampleDir)
            };
        }

        /// <summary>
        /// 生成按钮命令
        /// </summary>
        [RelayCommand]
        private void Generate()
        {
            if (_currentSearchResult == null)
            {
                StatusMessage = "请先执行查找";
                return;
            }

            if (CopyDriver && string.IsNullOrWhiteSpace(TargetDriverDirectory))
            {
                StatusMessage = "请选择驱动目标目录";
                return;
            }

            try
            {
                // 获取选中的范例
                var selectedExamples = GetSelectedExamples();

                // 生成AI训导词
                string prompt = _promptGeneratorService.GeneratePrompt(
                    _currentSearchResult,
                    selectedExamples,
                    CopyDriver,
                    TargetDriverDirectory);

                OutputText = prompt;

                // 保存到历史记录
                var record = _promptGeneratorService.CreateOutputRecord(_currentSearchResult, prompt, selectedExamples);
                _historyService.AddOutputRecord(record);
                OutputHistory.Insert(0, record);

                // 保存驱动目录历史
                if (CopyDriver && !string.IsNullOrEmpty(TargetDriverDirectory))
                {
                    _historyService.AddDriverDirectory(TargetDriverDirectory);
                    if (!DriverDirectoryHistory.Contains(TargetDriverDirectory))
                    {
                        DriverDirectoryHistory.Insert(0, TargetDriverDirectory);
                    }
                }

                StatusMessage = "训导词生成完成";
            }
            catch (Exception ex)
            {
                StatusMessage = $"生成出错: {ex.Message}";
            }
        }

        /// <summary>
        /// 复制到剪贴板命令
        /// </summary>
        [RelayCommand]
        private async Task CopyToClipboardAsync()
        {
            if (string.IsNullOrWhiteSpace(OutputText))
            {
                StatusMessage = "没有可复制的内容";
                return;
            }

            try
            {
                await TextCopy.ClipboardService.SetTextAsync(OutputText);
                StatusMessage = "已复制到剪贴板";
            }
            catch (Exception ex)
            {
                StatusMessage = $"复制失败: {ex.Message}";
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 提取型号数值(4-5位连续数字)
        /// </summary>
        private string ExtractModelNumber(string hardwareModel)
        {
            // 使用正则表达式匹配4-5位连续数字
            var match = Regex.Match(hardwareModel, @"\d{4,5}");
            return match.Success ? match.Value : string.Empty;
        }

        /// <summary>
        /// 获取选中的范例节点
        /// </summary>
        private List<ExampleTreeNode> GetSelectedExamples()
        {
            var selected = new List<ExampleTreeNode>();
            
            foreach (var node in ExampleTree)
            {
                CollectSelectedNodes(node, selected);
            }

            return selected;
        }

        /// <summary>
        /// 递归收集选中的节点
        /// </summary>
        private void CollectSelectedNodes(ExampleTreeNodeViewModel node, List<ExampleTreeNode> selected)
        {
            if (node.IsSelected)
            {
                selected.Add(node.ToModel());
            }

            foreach (var child in node.Children)
            {
                CollectSelectedNodes(child, selected);
            }
        }

        #endregion
    }

    /// <summary>
    /// 范例树节点ViewModel
    /// ViewModel wrapper for ExampleTreeNode with observable properties
    /// </summary>
    public partial class ExampleTreeNodeViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _fullPath = string.Empty;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isExpanded = true;

        [ObservableProperty]
        private ObservableCollection<ExampleTreeNodeViewModel> _children = new();

        public ExampleTreeNodeViewModel()
        {
        }

        public ExampleTreeNodeViewModel(ExampleTreeNode model)
        {
            Name = model.Name;
            FullPath = model.FullPath;
            IsSelected = model.IsSelected;
            IsExpanded = model.IsExpanded;

            foreach (var child in model.Children)
            {
                Children.Add(new ExampleTreeNodeViewModel(child));
            }
        }

        /// <summary>
        /// 转换为Model
        /// </summary>
        public ExampleTreeNode ToModel()
        {
            return new ExampleTreeNode
            {
                Name = Name,
                FullPath = FullPath,
                IsSelected = IsSelected,
                IsExpanded = IsExpanded
            };
        }
    }
}
