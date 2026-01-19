using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI;
using MyShopClient.Models;
using MyShopClient.Services.Category;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using System.Diagnostics;
using MyShopClient.Services.Report;

namespace MyShopClient.ViewModels
{
    public class GroupByOption
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class SalesViewModeOption
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents aggregated sales data for a category
    /// </summary>
    public class CategorySalesData
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ProductSalesPointDto> Points { get; set; } = new();
    }

    public partial class ReportViewModel : ObservableObject
    {
        private readonly IReportService _reportService;
        private readonly ICategoryService _categoryService;
        private CancellationTokenSource? _loadCts;

        public ObservableCollection<GroupByOption> GroupByOptions { get; } =
        new(new[]
          {
  new GroupByOption { Label = "Day", Value = "DAY" },
            new GroupByOption { Label = "Week", Value = "WEEK" },
         new GroupByOption { Label = "Month", Value = "MONTH" },
            new GroupByOption { Label = "Year", Value = "YEAR" }
            });

        public ObservableCollection<SalesViewModeOption> SalesViewModeOptions { get; } =
               new(new[]
        {
          new SalesViewModeOption { Label = "Products", Value = "PRODUCTS" },
           new SalesViewModeOption { Label = "Category", Value = "CATEGORY" }
           });

        [ObservableProperty]
        private GroupByOption? selectedGroupBy;

        private SalesViewModeOption? _selectedSalesViewMode;
        public SalesViewModeOption? SelectedSalesViewMode
        {
            get => _selectedSalesViewMode;
            set
            {
                if (SetProperty(ref _selectedSalesViewMode, value))
                {
                    OnPropertyChanged(nameof(SalesChartTitle));
                }
            }
        }

        [ObservableProperty]
        private string? fromDateText;

        [ObservableProperty]
        private string? toDateText;

        [ObservableProperty]
        private string? topText;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? errorMessage;

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        [ObservableProperty]
        private ISeries[] productSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private Axis[] productXAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] productYAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private ISeries[] revenueSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private Axis[] revenueXAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] revenueYAxes = Array.Empty<Axis>();

        public string SalesChartTitle => SelectedSalesViewMode?.Value == "CATEGORY"
            ? "Category sales (quantity)"
      : "Product sales (quantity)";

        private ISeries[] _revenuePieSeries = Array.Empty<ISeries>();
        public ISeries[] RevenuePieSeries
        {
            get => _revenuePieSeries;
            set
            {
                if (_revenuePieSeries != value)
                {
                    _revenuePieSeries = value;
                    OnPropertyChanged(nameof(RevenuePieSeries));
                }
            }
        }

        private bool _showRevenueAsPie;
        public bool ShowRevenueAsPie
        {
            get => _showRevenueAsPie;
            set
            {
                if (_showRevenueAsPie != value)
                {
                    _showRevenueAsPie = value;
                    OnPropertyChanged(nameof(ShowRevenueAsPie));
                }
            }
        }

        public ReportViewModel(IReportService reportService, ICategoryService categoryService)
        {
            _reportService = reportService;
            _categoryService = categoryService;

            SelectedGroupBy = GroupByOptions.FirstOrDefault(g => g.Value == "MONTH");
            SelectedSalesViewMode = SalesViewModeOptions.FirstOrDefault(m => m.Value == "PRODUCTS");

            var now = DateTime.Now;
            FromDateText = new DateTime(now.Year, 1, 1).ToString("yyyy-MM-dd");
            ToDateText = new DateTime(now.Year, 12, 31).ToString("yyyy-MM-dd");
            TopText = "5";

            ProductYAxes = new[] { new Axis { Name = "Quantity" } };
            RevenueYAxes = new[] { new Axis { Name = "Amount" } };
            ProductXAxes = new[] { new Axis { Labels = Array.Empty<string>() } };
            RevenueXAxes = new[] { new Axis { Labels = Array.Empty<string>() } };

            _ = LoadReportAsync();
        }

        private static DateTime? ParsePeriodToDateTime(string period)
        {
            if (string.IsNullOrWhiteSpace(period)) return null;
            if (DateTimeOffset.TryParse(period, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
            {
                return dto.DateTime;
            }
            if (DateTime.TryParseExact(period, new[] { "yyyy-MM-dd", "yyyy-MM", "yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt;
            }
            if (DateTime.TryParse(period, out dt)) return dt;
            return null;
        }

        private string FormatLabel(string period, string groupBy)
        {
            var dt = ParsePeriodToDateTime(period);
            if (dt == null) return period;
            return groupBy switch
            {
                "DAY" => dt.Value.ToString("yyyy-MM-dd"),
                "WEEK" => ISOWeek.GetYear(dt.Value) + "-W" + ISOWeek.GetWeekOfYear(dt.Value).ToString("D2"),
                "MONTH" => dt.Value.ToString("yyyy-MM"),
                "YEAR" => dt.Value.ToString("yyyy"),
                _ => dt.Value.ToString("yyyy-MM-dd")
            };
        }

        [RelayCommand]
        private Task ApplyFilterAsync() => LoadReportAsync();

        [RelayCommand]
        private void ToggleRevenuePie()
        {
            ShowRevenueAsPie = !ShowRevenueAsPie;
        }

        private async Task LoadReportAsync()
        {
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = new CancellationTokenSource();
            var cancellationToken = _loadCts.Token;

            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                // Check if Category mode is selected - show message and still load product data
                var isCategoryMode = SelectedSalesViewMode?.Value == "CATEGORY";

                var from = ParseDate(FromDateText);
                var to = ParseDate(ToDateText);

                if (from == null || to == null)
                {
                    ErrorMessage = "Please input valid date range (yyyy-MM-dd).";
                    return;
                }

                if (from > to)
                {
                    ErrorMessage = "From date must be before To date.";
                    return;
                }

                int? top = null;
                if (int.TryParse(TopText, out var topVal) && topVal > 0)
                    top = topVal;

                var opt = new ReportQueryOptions
                {
                    FromDate = from.Value,
                    ToDate = to.Value,
                    GroupBy = SelectedGroupBy?.Value ?? "MONTH",
                    Top = top,
                    CategoryId = null
                };

                // Load revenue data (always needed)
                var revenueTask = _reportService.GetRevenueProfitSeriesAsync(opt);

                if (isCategoryMode)
                {
                    // Load category sales data
                    var categorySalesData = await LoadCategorySalesAsync(opt, top, cancellationToken);
                    var revenueResult = await revenueTask;

                    cancellationToken.ThrowIfCancellationRequested();

                    if (!revenueResult.Success)
                    {
                        ErrorMessage = revenueResult.Message ?? "Cannot load revenue/profit report.";
                        return;
                    }

                    void UpdateCategoryCharts()
                    {
                        try
                        {
                            BuildCategoryChart(categorySalesData);
                            BuildRevenueChart(revenueResult.Data ?? new());
                        }
                        catch (Exception ex)
                        {
                            ErrorMessage = ex.Message;
                            OnPropertyChanged(nameof(HasError));
                        }
                    }

                    if (App.MainWindow?.DispatcherQueue != null)
                    {
                        App.MainWindow.DispatcherQueue.TryEnqueue(UpdateCategoryCharts);
                    }
                    else
                    {
                        try { UpdateCategoryCharts(); }
                        catch (Exception ex) { ErrorMessage = ex.Message; OnPropertyChanged(nameof(HasError)); }
                    }
                }
                else
                {
                    // Load product sales data
                    var productTask = _reportService.GetProductSalesSeriesAsync(opt);
                    await Task.WhenAll(productTask, revenueTask);

                    cancellationToken.ThrowIfCancellationRequested();

                    var productResult = productTask.Result;
                    var revenueResult = revenueTask.Result;

                    if (!productResult.Success)
                    {
                        ErrorMessage = productResult.Message ?? "Cannot load product sales report.";
                        return;
                    }

                    if (!revenueResult.Success)
                    {
                        ErrorMessage = revenueResult.Message ?? "Cannot load revenue/profit report.";
                        return;
                    }

                    void UpdateProductCharts()
                    {
                        try
                        {
                            BuildProductChart(productResult.Data ?? new());
                            BuildRevenueChart(revenueResult.Data ?? new());
                        }
                        catch (Exception ex)
                        {
                            ErrorMessage = ex.Message;
                            OnPropertyChanged(nameof(HasError));
                        }
                    }

                    if (App.MainWindow?.DispatcherQueue != null)
                    {
                        App.MainWindow.DispatcherQueue.TryEnqueue(UpdateProductCharts);
                    }
                    else
                    {
                        try { UpdateProductCharts(); }
                        catch (Exception ex) { ErrorMessage = ex.Message; OnPropertyChanged(nameof(HasError)); }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasError));
            }
        }

        /// <summary>
        /// Load sales data for each category by calling API with categoryId filter, then aggregate
        /// </summary>
        private async Task<List<CategorySalesData>> LoadCategorySalesAsync(ReportQueryOptions baseOpt, int? top, CancellationToken cancellationToken)
        {
            var result = new List<CategorySalesData>();

            // Step 1: Get all categories
            var categoriesResult = await _categoryService.GetCategoriesAsync(null, 1, 100);
            if (!categoriesResult.Success || categoriesResult.Data?.Items == null)
            {
                Debug.WriteLine("[ReportViewModel] Failed to load categories");
                return result;
            }

            var categories = categoriesResult.Data.Items;
            Debug.WriteLine($"[ReportViewModel] Loaded {categories.Count} categories");

            // Step 2: For each category, get product sales with categoryId filter
            var tasks = categories.Select(async category =>
                       {
                           var opt = new ReportQueryOptions
                           {
                               FromDate = baseOpt.FromDate,
                               ToDate = baseOpt.ToDate,
                               GroupBy = baseOpt.GroupBy,
                               Top = 1000, // Get all products in category (backend requires non-null value)
                               CategoryId = category.CategoryId
                           };

                           var salesResult = await _reportService.GetProductSalesSeriesAsync(opt);

                           if (!salesResult.Success || salesResult.Data == null || salesResult.Data.Count == 0)
                           {
                               return null;
                           }

                           // Aggregate all products' sales into category total
                           var allPeriods = salesResult.Data
     .SelectMany(p => p.Points)
             .Select(pt => pt.Period)
            .Distinct()
         .ToList();

                           var aggregatedPoints = allPeriods.Select(period =>
                 {
                    var totalValue = salesResult.Data
.SelectMany(p => p.Points)
     .Where(pt => pt.Period == period)
    .Sum(pt => pt.Value);

                    return new ProductSalesPointDto
                    {
                        Period = period,
                        Value = totalValue
                    };
                }).ToList();

                           // Only return if there's actual sales data
                           if (aggregatedPoints.All(p => p.Value == 0))
                           {
                               return null;
                           }

                           return new CategorySalesData
                           {
                               CategoryId = category.CategoryId,
                               Name = category.Name,
                               Points = aggregatedPoints
                           };
                       });

            var categoryResults = await Task.WhenAll(tasks);

            cancellationToken.ThrowIfCancellationRequested();

            // Filter out nulls and sort by total sales descending
            result = categoryResults
                .Where(c => c != null)
                          .Cast<CategorySalesData>()
                   .OrderByDescending(c => c.Points.Sum(p => p.Value))
            .ToList();

            // Apply top filter if specified
            if (top.HasValue && top.Value > 0)
            {
                result = result.Take(top.Value).ToList();
            }

            Debug.WriteLine($"[ReportViewModel] Loaded sales data for {result.Count} categories");
            return result;
        }

        private void BuildProductChart(List<ProductSalesSeriesDto> seriesData)
        {
            if (seriesData == null || seriesData.Count == 0)
            {
                ProductSeries = Array.Empty<ISeries>();
                ProductXAxes = new[] { new Axis { Labels = Array.Empty<string>() } };
                ProductYAxes = new[] { new Axis { Name = "Quantity" } };
                return;
            }

            var allPeriodsRaw = seriesData
                           .SelectMany(s => s.Points)
                           .Select(p => p.Period)
                   .Distinct()
                   .ToArray();

            var parsed = allPeriodsRaw
              .Select(p => new { Raw = p, Dt = ParsePeriodToDateTime(p) })
         .OrderBy(x => x.Dt ?? DateTime.MinValue)
                .ToArray();

            var allPeriods = parsed.Select(x => x.Raw).ToArray();
            var labels = parsed.Select(x => FormatLabel(x.Raw, SelectedGroupBy?.Value ?? "MONTH")).ToArray();

            var colors = new[]
               {
   SKColors.DeepSkyBlue,
   SKColors.Crimson,
           SKColors.MediumSeaGreen,
     SKColors.Orange,
    SKColors.Purple
     };

            var lineSeries = seriesData.Select((s, index) =>
              {
                  var values = allPeriods
             .Select(period => (double)(s.Points.FirstOrDefault(p => p.Period == period)?.Value ?? 0))
            .ToArray();

                  return new LineSeries<double>
                  {
                      Name = s.Name,
                      Values = values,
                      GeometrySize = 15,
                      GeometryStroke = new SolidColorPaint(colors[index % colors.Length]) { StrokeThickness = 2 },
                      GeometryFill = new SolidColorPaint(colors[index % colors.Length]),
                      Stroke = new SolidColorPaint(colors[index % colors.Length]) { StrokeThickness = 3 },
                      Fill = null,
                      LineSmoothness = 0,
                      DataLabelsSize = 12,
                      DataLabelsPaint = new SolidColorPaint(colors[index % colors.Length]),
                      DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
                  } as ISeries;
              }).ToArray();

            ProductSeries = lineSeries;
            ProductXAxes = new[]
     {
            new Axis
                {
    Labels = labels,
  LabelsRotation = 45,
   TextSize = 12
                }
       };
            ProductYAxes = new[]
                     {
              new Axis
  {
                  Name = "Quantity",
             TextSize = 12,
            MinLimit = 0
  }
            };
        }

        private void BuildCategoryChart(List<CategorySalesData> seriesData)
        {
            if (seriesData == null || seriesData.Count == 0)
            {
                ProductSeries = Array.Empty<ISeries>();
                ProductXAxes = new[] { new Axis { Labels = Array.Empty<string>() } };
                ProductYAxes = new[] { new Axis { Name = "Quantity" } };
                return;
            }

            var allPeriodsRaw = seriesData
              .SelectMany(s => s.Points)
              .Select(p => p.Period)
                     .Distinct()
                      .ToArray();

            var parsed = allPeriodsRaw
       .Select(p => new { Raw = p, Dt = ParsePeriodToDateTime(p) })
          .OrderBy(x => x.Dt ?? DateTime.MinValue)
            .ToArray();

            var allPeriods = parsed.Select(x => x.Raw).ToArray();
            var labels = parsed.Select(x => FormatLabel(x.Raw, SelectedGroupBy?.Value ?? "MONTH")).ToArray();

            var colors = new[]
       {
     SKColors.Coral,
        SKColors.DodgerBlue,
       SKColors.LimeGreen,
                SKColors.Gold,
              SKColors.MediumOrchid,
 SKColors.Tomato,
                SKColors.Teal,
SKColors.HotPink
            };

            var lineSeries = seriesData.Select((s, index) =>
   {
       var values = allPeriods
     .Select(period => (double)(s.Points.FirstOrDefault(p => p.Period == period)?.Value ?? 0))
     .ToArray();

       return new LineSeries<double>
       {
           Name = s.Name,
           Values = values,
           GeometrySize = 15,
           GeometryStroke = new SolidColorPaint(colors[index % colors.Length]) { StrokeThickness = 2 },
           GeometryFill = new SolidColorPaint(colors[index % colors.Length]),
           Stroke = new SolidColorPaint(colors[index % colors.Length]) { StrokeThickness = 3 },
           Fill = null,
           LineSmoothness = 0,
           DataLabelsSize = 12,
           DataLabelsPaint = new SolidColorPaint(colors[index % colors.Length]),
           DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
       } as ISeries;
   }).ToArray();

            ProductSeries = lineSeries;
            ProductXAxes = new[]
                      {
       new Axis
                {
    Labels = labels,
    LabelsRotation = 45,
         TextSize = 12
     }
       };
            ProductYAxes = new[]
         {
           new Axis
        {
      Name = "Quantity",
                 TextSize = 12,
         MinLimit = 0
 }
  };
        }

        private void BuildRevenueChart(List<RevenueProfitPointDto> points)
        {
            if (points == null || points.Count == 0)
            {
                RevenueSeries = Array.Empty<ISeries>();
                RevenueXAxes = new[] { new Axis { Labels = Array.Empty<string>() } };
                RevenueYAxes = new[] { new Axis { Name = "Amount" } };
                RevenuePieSeries = Array.Empty<ISeries>();
                return;
            }

            var parsed = points
             .Select(p => new { Raw = p.Period, Dt = ParsePeriodToDateTime(p.Period), Point = p })
            .OrderBy(x => x.Dt ?? DateTime.MinValue)
          .ToArray();

            var labels = parsed.Select(x => FormatLabel(x.Raw, SelectedGroupBy?.Value ?? "MONTH")).ToArray();
            var revenueValues = parsed.Select(x => (double)x.Point.Revenue).ToArray();
            var profitValues = parsed.Select(x => (double)x.Point.Profit).ToArray();

            RevenueSeries = new ISeries[]
             {
         new ColumnSeries<double>
 {
               Name = "Revenue",
           Values = revenueValues,
        Fill = new SolidColorPaint(SKColors.MediumSeaGreen)
       },
     new ColumnSeries<double>
          {
      Name = "Profit",
         Values = profitValues,
             Fill = new SolidColorPaint(SKColors.SteelBlue)
         }
                  };

            RevenueXAxes = new[]
 {
           new Axis
            {
                Labels = labels,
          LabelsRotation = 45
     }
   };
            RevenueYAxes = new[] { new Axis { Name = "Amount" } };

            if (revenueValues.Length > 1)
            {
                var pie = parsed.Select(x => (ISeries)new PieSeries<double>
                {
                    Name = FormatLabel(x.Raw, SelectedGroupBy?.Value ?? "MONTH"),
                    Values = new double[] { x.Point.Revenue },
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black)
                }).ToArray();

                RevenuePieSeries = pie;
            }
            else
            {
                RevenuePieSeries = Array.Empty<ISeries>();
            }
        }

        private static DateTime? ParseDate(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (DateTime.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt.Date;
            }
            return null;
        }
    }
}
