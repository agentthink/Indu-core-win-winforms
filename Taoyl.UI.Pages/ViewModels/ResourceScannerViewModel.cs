using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniExcelLibs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Storage;
using Taoyl.UI.Pages.Services;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// 文本资源扫描器 ViewModel
    ///
    /// 核心设计：
    /// 1. 扫描逻辑（Roslyn 提取 ToTranslate + 去重）全部内聚于 VM，后台 Task.Run 执行，UI 线程只做结果绑定
    /// 2. 状态反馈统一走 StatusMessage（View 订阅后气泡提示），命令不直接操作 UI
    /// 3. 导出 Excel / JSON 由 View 弹出文件对话框后传入路径，VM 只负责写文件
    /// </summary>
    public partial class ResourceScannerViewModel : ObservableObject
    {
        #region 依赖与状态

        private readonly TextResourceExtractor extractor = new TextResourceExtractor();
        private List<ResourceEntry> scannedEntries = new List<ResourceEntry>();

        #endregion

        #region 可观察属性

        [ObservableProperty]
        private bool isScanning;

        [ObservableProperty]
        private string statusMessage = "";

        [ObservableProperty]
        private DataTable resultTable;

        #endregion

        #region 扫描命令

        [RelayCommand]
        private async Task ScanAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                StatusMessage = "请先选择文件夹或.cs文件";
                return;
            }

            if (IsScanning) return;
            IsScanning = true;

            try
            {
                var files = CollectFiles(path);
                if (files.Count == 0)
                {
                    StatusMessage = "路径无效，未找到任何.cs文件";
                    return;
                }

                // 后台扫描（Roslyn 解析），UI 线程只做结果绑定
                var entries = await Task.Run(() => ScanCore(files));
                scannedEntries = entries;
                ResultTable = ResourceMapper.ToDataTable(entries);

                StatusMessage = $"扫描完成，共提取 {entries.Count} 条文本资源";
            }
            catch (Exception ex)
            {
                StatusMessage = $"扫描失败：{ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        private List<string> CollectFiles(string path)
        {
            var files = new List<string>();

            if (Directory.Exists(path))
            {
                // 递归获取所有 .cs 文件，排除 Program.cs
                files.AddRange(Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !Path.GetFileName(f).Equals("Program.cs", StringComparison.OrdinalIgnoreCase)));
            }
            else if (File.Exists(path) && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(path);
            }

            return files;
        }

        private List<ResourceEntry> ScanCore(List<string> files)
        {
            var entries = new List<ResourceEntry>();

            foreach (var file in files)
            {
                try
                {
                    var extracted = extractor.ExtractFromFile(file);
                    if (extracted != null)
                        entries.AddRange(extracted);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"扫描文件失败 {file}: {ex.Message}");
                }
            }

            // 去重：按 (命名空间, 类名, Key) 三元组。Key 格式 {Type}.{ControlName} 不含类名，
            // 仅按 Key 去重会误删跨类同语义键的条目（不同类的同名 key 只保留一条）
            return entries
                .GroupBy(e => new { e.Namespace, e.ClassName, e.Key })
                .Select(g => g.First())
                .ToList();
        }

        #endregion

        #region 导出命令

        [RelayCommand]
        private void ExportExcel(string filePath)
        {
            if (ResultTable == null || ResultTable.Rows.Count == 0)
            {
                StatusMessage = "没有可导出的数据";
                return;
            }

            try
            {
                MiniExcel.SaveAs(filePath, ResultTable, overwriteFile: true);
                StatusMessage = "导出成功";
            }
            catch (Exception ex)
            {
                StatusMessage = $"导出失败：{ex.Message}";
            }
        }

        [RelayCommand]
        private void ExportJson(string filePath)
        {
            if (scannedEntries.Count == 0)
            {
                StatusMessage = "没有可导出的数据";
                return;
            }

            try
            {
                var json = JsonConvert.SerializeObject(scannedEntries, Formatting.Indented);
                File.WriteAllText(filePath, json);
                StatusMessage = "导出成功";
            }
            catch (Exception ex)
            {
                StatusMessage = $"导出失败：{ex.Message}";
            }
        }

        /// <summary>
        /// 建议导出文件名（View 用于 SaveFileDialog 默认名）
        /// </summary>
        public string GetSuggestedFileName()
        {
            if (scannedEntries.Count == 0)
                return $"TextResource_{DateTime.Now:yyyyMMdd_HHmmss}";

            var first = scannedEntries.First();
            return $"{first.Namespace}_{first.ClassName}_{ResourceType.Text}";
        }

        #endregion
    }
}
