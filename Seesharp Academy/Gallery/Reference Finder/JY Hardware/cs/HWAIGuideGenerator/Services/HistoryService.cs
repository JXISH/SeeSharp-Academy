using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using HWAIGuideGenerator.Models;

namespace HWAIGuideGenerator.Services
{
    /// <summary>
    /// 历史记录服务
    /// Service for managing input/output history with persistence
    /// </summary>
    public class HistoryService
    {
        private const string HistoryFileName = "AIGuideGenerator_Hardware_History.json";
        private const int MaxHistoryItems = 20;

        private readonly string _historyFilePath;
        private HistoryData _historyData;

        /// <summary>
        /// 当前历史数据
        /// </summary>
        public HistoryData CurrentHistory => _historyData;

        public HistoryService()
        {
            _historyFilePath = GetHistoryFilePath();
            _historyData = LoadHistory();
        }

        /// <summary>
        /// 获取历史文件路径
        /// Gets the history file path based on the operating system
        /// </summary>
        private string GetHistoryFilePath()
        {
            string baseDir;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: C:\Users\用户名\AppData\Roaming\SeeSharp\JYTEK\
                baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SeeSharp", "JYTEK");
            }
            else
            {
                // Linux: /home/用户名/.config/SeeSharp/JYTEK
                baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".config", "SeeSharp", "JYTEK");
            }

            // 确保目录存在
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            return Path.Combine(baseDir, HistoryFileName);
        }

        /// <summary>
        /// 加载历史记录
        /// Loads history data from the JSON file
        /// </summary>
        private HistoryData LoadHistory()
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    string json = File.ReadAllText(_historyFilePath);
                    return JsonSerializer.Deserialize<HistoryData>(json) ?? new HistoryData();
                }
            }
            catch (Exception)
            {
                // 如果加载失败，返回空历史记录
            }

            return new HistoryData();
        }

        /// <summary>
        /// 保存历史记录
        /// Saves history data to the JSON file
        /// </summary>
        public void SaveHistory()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string json = JsonSerializer.Serialize(_historyData, options);
                File.WriteAllText(_historyFilePath, json);
            }
            catch (Exception)
            {
                // 保存失败时忽略错误
            }
        }

        /// <summary>
        /// 添加JYPEDIA文件路径到历史记录
        /// </summary>
        public void AddJypediaFilePath(string path)
        {
            AddToHistory(_historyData.JypediaFilePaths, path);
            SaveHistory();
        }

        /// <summary>
        /// 添加硬件型号到历史记录
        /// </summary>
        public void AddHardwareModel(string model)
        {
            AddToHistory(_historyData.HardwareModels, model);
            SaveHistory();
        }

        /// <summary>
        /// 添加驱动目录到历史记录
        /// </summary>
        public void AddDriverDirectory(string directory)
        {
            AddToHistory(_historyData.DriverDirectories, directory);
            SaveHistory();
        }

        /// <summary>
        /// 添加输出记录到历史记录
        /// </summary>
        public void AddOutputRecord(HistoryOutputRecord record)
        {
            // 添加到列表开头
            _historyData.OutputRecords.Insert(0, record);

            // 限制数量
            if (_historyData.OutputRecords.Count > MaxHistoryItems)
            {
                _historyData.OutputRecords = _historyData.OutputRecords.Take(MaxHistoryItems).ToList();
            }

            SaveHistory();
        }

        /// <summary>
        /// 获取JYPEDIA文件路径历史
        /// </summary>
        public IReadOnlyList<string> GetJypediaFilePathHistory()
        {
            return _historyData.JypediaFilePaths.AsReadOnly();
        }

        /// <summary>
        /// 获取硬件型号历史
        /// </summary>
        public IReadOnlyList<string> GetHardwareModelHistory()
        {
            return _historyData.HardwareModels.AsReadOnly();
        }

        /// <summary>
        /// 获取驱动目录历史
        /// </summary>
        public IReadOnlyList<string> GetDriverDirectoryHistory()
        {
            return _historyData.DriverDirectories.AsReadOnly();
        }

        /// <summary>
        /// 获取输出记录历史
        /// </summary>
        public IReadOnlyList<HistoryOutputRecord> GetOutputRecordHistory()
        {
            return _historyData.OutputRecords.AsReadOnly();
        }

        /// <summary>
        /// 添加项到历史列表(去重并限制数量)
        /// </summary>
        private void AddToHistory(List<string> list, string item)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                return;
            }

            // 移除已存在的相同项
            list.RemoveAll(x => x.Equals(item, StringComparison.OrdinalIgnoreCase));

            // 添加到列表开头
            list.Insert(0, item);

            // 限制数量
            if (list.Count > MaxHistoryItems)
            {
                list.RemoveRange(MaxHistoryItems, list.Count - MaxHistoryItems);
            }
        }

        /// <summary>
        /// 清除所有历史记录
        /// </summary>
        public void ClearAllHistory()
        {
            _historyData = new HistoryData();
            SaveHistory();
        }
    }
}
