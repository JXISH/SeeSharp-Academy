using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HWAIGuideGenerator.Models;

namespace HWAIGuideGenerator.Services
{
    /// <summary>
    /// 文件搜索服务
    /// Service for searching driver and example files in the file system
    /// </summary>
    public class FileSearchService
    {
        /// <summary>
        /// 获取驱动根目录
        /// Gets the driver root directory based on the operating system
        /// </summary>
        public string GetDriverRootDirectory(OperatingSystemType osType)
        {
            return osType == OperatingSystemType.Windows
                ? @"c:\SeeSharp\JYTEK\Hardware"
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SeeSharp", "JYTEK", "Hardware");
        }

        /// <summary>
        /// 获取用户范例根目录
        /// Gets the user example root directory
        /// </summary>
        public string GetUserExampleRootDirectory(OperatingSystemType osType)
        {
            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return osType == OperatingSystemType.Windows
                ? Path.Combine(userHome, "SeeSharp", "JYTEK")
                : Path.Combine(userHome, "SeeSharp", "JYTEK");
        }

        /// <summary>
        /// 获取系统范例根目录
        /// Gets the system example root directory
        /// </summary>
        public string GetSystemExampleRootDirectory(OperatingSystemType osType)
        {
            return osType == OperatingSystemType.Windows
                ? @"c:\SeeSharp\JYTEK"
                : "/home/SeeSharp/JYTEK";
        }

        /// <summary>
        /// 搜索驱动目录
        /// Searches for driver directory containing the compact driver name
        /// </summary>
        /// <param name="driverRoot">驱动根目录</param>
        /// <param name="compactDriverName">紧凑驱动名</param>
        /// <returns>找到的驱动目录列表(按深度排序)</returns>
        public List<string> SearchDriverDirectories(string driverRoot, string compactDriverName)
        {
            var results = new List<(string Path, int Depth)>();

            if (!Directory.Exists(driverRoot))
            {
                return new List<string>();
            }

            try
            {
                // 获取所有子目录
                var allDirs = Directory.GetDirectories(driverRoot, "*", SearchOption.AllDirectories);
                
                foreach (var dir in allDirs)
                {
                    string dirName = Path.GetFileName(dir);
                    // 去除空格和连字符后比较
                    string compactDirName = dirName.Replace(" ", "").Replace("-", "");
                    
                    if (compactDirName.Contains(compactDriverName, StringComparison.OrdinalIgnoreCase))
                    {
                        // 计算相对深度
                        int depth = dir.Substring(driverRoot.Length).Count(c => c == Path.DirectorySeparatorChar || c == '/');
                        results.Add((dir, depth));
                    }
                }
            }
            catch (Exception)
            {
                // 忽略访问错误
            }

            // 按深度排序，返回最浅的目录
            return results.OrderBy(r => r.Depth).Select(r => r.Path).ToList();
        }

        /// <summary>
        /// 搜索驱动DLL文件
        /// Searches for driver DLL files containing the model number in the Bin subdirectory
        /// </summary>
        /// <param name="driverDirectory">驱动目录</param>
        /// <param name="modelNumber">型号数值</param>
        /// <returns>找到的DLL文件列表</returns>
        public List<string> SearchDriverDllFiles(string driverDirectory, string modelNumber)
        {
            var binDir = Path.Combine(driverDirectory, "Bin");
            
            if (!Directory.Exists(binDir))
            {
                // 尝试其他常见路径
                binDir = Path.Combine(driverDirectory, "bin");
                if (!Directory.Exists(binDir))
                {
                    // 直接在驱动目录搜索
                    binDir = driverDirectory;
                }
            }

            try
            {
                return Directory.GetFiles(binDir, "*.dll")
                    .Where(f => Path.GetFileName(f).Contains(modelNumber))
                    .Select(f => Path.GetFileName(f))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 搜索驱动XML文件
        /// Searches for XML files matching the DLL names
        /// </summary>
        /// <param name="driverDirectory">驱动目录</param>
        /// <param name="dllFileNames">DLL文件名列表</param>
        /// <returns>找到的XML文件列表</returns>
        public List<string> SearchDriverXmlFiles(string driverDirectory, List<string> dllFileNames)
        {
            var binDir = Path.Combine(driverDirectory, "Bin");
            
            if (!Directory.Exists(binDir))
            {
                binDir = Path.Combine(driverDirectory, "bin");
                if (!Directory.Exists(binDir))
                {
                    binDir = driverDirectory;
                }
            }

            var xmlFiles = new List<string>();
            
            foreach (var dllName in dllFileNames)
            {
                string xmlName = Path.ChangeExtension(dllName, ".xml");
                string xmlPath = Path.Combine(binDir, xmlName);
                
                if (File.Exists(xmlPath))
                {
                    xmlFiles.Add(xmlName);
                }
            }

            return xmlFiles;
        }

        /// <summary>
        /// 搜索范例目录 - 按确切目录名搜索
        /// Searches for example directory in user and system directories by exact name
        /// </summary>
        /// <param name="osType">操作系统类型</param>
        /// <param name="exampleDirectoryName">范例目录名</param>
        /// <returns>找到的范例目录路径</returns>
        public string? SearchExampleDirectory(OperatingSystemType osType, string exampleDirectoryName)
        {
            // 首先搜索用户目录
            string userRoot = GetUserExampleRootDirectory(osType);
            var userResult = SearchDirectoryByName(userRoot, exampleDirectoryName);
            if (userResult != null)
            {
                return userResult;
            }

            // 然后搜索系统目录
            string systemRoot = GetSystemExampleRootDirectory(osType);
            return SearchDirectoryByName(systemRoot, exampleDirectoryName);
        }

        /// <summary>
        /// 搜索范例目录 - 按型号数值搜索
        /// Searches for example directory containing model number and "Example" in name
        /// </summary>
        /// <param name="osType">操作系统类型</param>
        /// <param name="modelNumber">型号数值(4-5位数字)</param>
        /// <returns>找到的范例目录路径列表</returns>
        public List<string> SearchExampleDirectoriesByModelNumber(OperatingSystemType osType, string modelNumber)
        {
            var results = new List<string>();

            // 搜索用户目录
            string userRoot = GetUserExampleRootDirectory(osType);
            results.AddRange(SearchDirectoriesContainingModelAndExample(userRoot, modelNumber));
            if (results.Count == 0)
            {
                // 搜索系统目录
                string systemRoot = GetSystemExampleRootDirectory(osType);
                results.AddRange(SearchDirectoriesContainingModelAndExample(systemRoot, modelNumber));
            }
            // 去重并按深度排序(浅的优先)
            return results
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(d => d.Count(c => c == Path.DirectorySeparatorChar || c == '/'))
                .ToList();
        }

        /// <summary>
        /// 搜索包含型号数值和"Example"的目录
        /// </summary>
        private List<string> SearchDirectoriesContainingModelAndExample(string rootPath, string modelNumber)
        {
            var results = new List<string>();

            if (!Directory.Exists(rootPath))
            {
                return results;
            }

            try
            {
                // 获取所有子目录
                var allDirs = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories);
                
                foreach (var dir in allDirs)
                {
                    string dirName = Path.GetFileName(dir);
                    // 目录名必须同时包含型号数值和"Example"
                    if (dirName.Contains(modelNumber, StringComparison.OrdinalIgnoreCase) &&
                        dirName.Contains("Example", StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(dir);
                    }
                }
            }
            catch (Exception)
            {
                // 忽略访问错误
            }

            return results;
        }

        /// <summary>
        /// 按名称搜索目录
        /// </summary>
        private string? SearchDirectoryByName(string rootPath, string directoryName)
        {
            if (!Directory.Exists(rootPath))
            {
                return null;
            }

            try
            {
                // 直接子目录匹配
                var directMatch = Path.Combine(rootPath, directoryName);
                if (Directory.Exists(directMatch))
                {
                    return directMatch;
                }

                // 搜索子目录
                var allDirs = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories);
                return allDirs.FirstOrDefault(d => 
                    Path.GetFileName(d).Equals(directoryName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取范例目录下的两级子目录树
        /// Builds a tree of subdirectories up to 2 levels deep
        /// </summary>
        /// <param name="exampleDirectory">范例目录</param>
        /// <returns>范例树节点列表</returns>
        public List<ExampleTreeNode> GetExampleTree(string exampleDirectory)
        {
            var rootNodes = new List<ExampleTreeNode>();

            if (!Directory.Exists(exampleDirectory))
            {
                return rootNodes;
            }

            try
            {
                // 第一级子目录
                var level1Dirs = Directory.GetDirectories(exampleDirectory);
                
                foreach (var level1Dir in level1Dirs)
                {
                    var level1Node = new ExampleTreeNode
                    {
                        Name = Path.GetFileName(level1Dir),
                        FullPath = level1Dir,
                        IsExpanded = true
                    };

                    // 第二级子目录
                    try
                    {
                        var level2Dirs = Directory.GetDirectories(level1Dir);
                        foreach (var level2Dir in level2Dirs)
                        {
                            level1Node.Children.Add(new ExampleTreeNode
                            {
                                Name = Path.GetFileName(level2Dir),
                                FullPath = level2Dir,
                                IsExpanded = false
                            });
                        }
                    }
                    catch (Exception)
                    {
                        // 忽略访问错误
                    }

                    rootNodes.Add(level1Node);
                }
            }
            catch (Exception)
            {
                // 忽略访问错误
            }

            return rootNodes;
        }

        /// <summary>
        /// 获取当前操作系统类型
        /// Gets the current operating system type
        /// </summary>
        public OperatingSystemType GetCurrentOperatingSystem()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? OperatingSystemType.Windows
                : OperatingSystemType.Linux;
        }

        /// <summary>
        /// 查找目录下的解决方案文件
        /// Finds solution files (.sln) in the directory
        /// </summary>
        public List<string> FindSolutionFiles(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return new List<string>();
            }

            try
            {
                return Directory.GetFiles(directory, "*.sln", SearchOption.AllDirectories)
                    .Select(f => Path.GetFileName(f))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 查找目录下的项目文件
        /// Finds project files (.csproj) in the directory
        /// </summary>
        public List<string> FindProjectFiles(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return new List<string>();
            }

            try
            {
                return Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories)
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }
    }
}
