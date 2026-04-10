using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShotSkiMahiD.Models;
using ShotSkiMahiD.Services;

namespace ShotSkiMahiD.ViewModels
{
    public partial class LogViewerViewModel : ObservableObject
    {
        private readonly LogService _logService;

        [ObservableProperty]
        private ObservableCollection<LocalLogEntry> _logs = new();

        [ObservableProperty]
        private LocalLogEntry? _selectedLog;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _apiNameFilter = "全部";

        [ObservableProperty]
        private bool? _successFilter = null; // null=全部, true=成功, false=失败

        [ObservableProperty]
        private DateTime _filterStartDate = DateTime.Today.AddDays(-7);

        [ObservableProperty]
        private DateTime _filterEndDate = DateTime.Now;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private int _successCount;

        [ObservableProperty]
        private int _failCount;

        [ObservableProperty]
        private string _detailText = "请选择一条日志查看详细信息";

        public string[] ApiNameOptions { get; } = { "全部", "Start", "GetSfcKey", "AddSfcKey", "TestDataCollect2MainChild", "Complete" };

        public class FilterOption
        {
            public string DisplayText { get; set; } = "";
            public bool? Value { get; set; }
        }

        public FilterOption[] SuccessFilterOptions { get; } =
        {
            new FilterOption { DisplayText = "全部", Value = null },
            new FilterOption { DisplayText = "✓ 成功", Value = true },
            new FilterOption { DisplayText = "✗ 失败", Value = false }
        };

        [ObservableProperty]
        private FilterOption? _selectedSuccessFilter;

        partial void OnSelectedSuccessFilterChanged(FilterOption? value)
        {
            if (value != null)
                SuccessFilter = value.Value;
        }

        public LogViewerViewModel(LogService logService)
        {
            _logService = logService;
            SelectedSuccessFilter = SuccessFilterOptions[0];
            
            // 订阅新增日志事件
            logService.OnApiLogAdded += OnNewApiLog;

            // 加载已有日志
            LoadLogs();
        }

        partial void OnSelectedLogChanged(LocalLogEntry? value)
        {
            if (value != null)
                DetailText = value.ToDetailedString();
            else
                DetailText = "请选择一条日志查看详细信息";
        }

        [RelayCommand]
        private void Search()
        {
            ApplyFilter();
        }

        [RelayCommand]
        private void Refresh()
        {
            SearchKeyword = string.Empty;
            ApiNameFilter = "全部";
            SelectedSuccessFilter = SuccessFilterOptions[0];
            FilterStartDate = DateTime.Today.AddDays(-7);
            FilterEndDate = DateTime.Now;
            LoadLogs();
        }

        [RelayCommand]
        private void ExportSelected()
        {
            if (SelectedLog == null)
            {
                MessageBox.Show("请先选择要导出的日志条目", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"API_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    DefaultExt = ".txt",
                    Filter = "文本文件 (*.txt)|*.txt|所有文件|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dialog.FileName, SelectedLog.ToDetailedString());
                    MessageBox.Show($"日志已导出到: {dialog.FileName}", "导出成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnNewApiLog(object? sender, LocalLogEntry entry)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var apiName = ApiNameFilter == "全部" ? null : ApiNameFilter;
                var keyword = string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword.Trim();

                var matchesApiName = apiName == null || entry.ApiName.Contains(apiName, StringComparison.OrdinalIgnoreCase);
                var matchesKeyword = keyword == null ||
                    (entry.FullUrl.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                     entry.RequestBody.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                     entry.ResponseBody.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                     entry.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
                var matchesSuccess = SuccessFilter == null || entry.IsSuccess == SuccessFilter.Value;
                var matchesTime = entry.Timestamp >= FilterStartDate.Date && entry.Timestamp <= FilterEndDate.Date.AddDays(1).AddTicks(-1);

                if (matchesApiName && matchesKeyword && matchesSuccess && matchesTime)
                {
                    Logs.Insert(0, entry);
                    TotalCount++;
                    if (entry.IsSuccess)
                        SuccessCount++;
                    else
                        FailCount++;
                }
            });
        }

        private void LoadLogs()
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var apiName = ApiNameFilter == "全部" ? null : ApiNameFilter;
            var keyword = string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword.Trim();
            var startTime = FilterStartDate.Date;
            var endTime = FilterEndDate.Date.AddDays(1).AddTicks(-1);

            var filtered = _logService.FilterApiLogs(
                apiName: apiName,
                keyword: keyword,
                isSuccess: SuccessFilter,
                startTime: startTime,
                endTime: endTime);

            Logs.Clear();
            foreach (var log in filtered)
                Logs.Add(log);

            TotalCount = filtered.Count;
            SuccessCount = filtered.Count(l => l.IsSuccess);
            FailCount = filtered.Count(l => !l.IsSuccess);
        }
    }
}
