using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI;
using MyShopClient.Models;
using SkiaSharp;
using System;
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
        public string Label { get; set; } = string.Empty; // hiển thị
        public string Value { get; set; } = string.Empty; // DAY/WEEK/MONTH/YEAR
    }

    public partial class ReportViewModel : ObservableObject
    {
    private readonly IReportService _reportService;
        private CancellationTokenSource? _loadCts;

        public ObservableCollection<GroupByOption> GroupByOptions { get; } =
         new(new[]
            {
   new GroupByOption { Label = "Day", Value = "DAY" },
                new GroupByOption { Label = "Week", Value = "WEEK" },
              new GroupByOption { Label = "Month", Value = "MONTH" },
        new GroupByOption { Label = "Year", Value = "YEAR" }
            });

        [ObservableProperty] private GroupByOption? selectedGroupBy;
        [ObservableProperty] private string? fromDateText;
     [ObservableProperty] private string? toDateText;
[ObservableProperty] private string? topText;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

// LiveCharts binding
     [ObservableProperty] private ISeries[] productSeries = Array.Empty<ISeries>();
        [ObservableProperty] private Axis[] productXAxes = Array.Empty<Axis>();
        [ObservableProperty] private Axis[] productYAxes = Array.Empty<Axis>();

 [ObservableProperty] private ISeries[] revenueSeries = Array.Empty<ISeries>();
        [ObservableProperty] private Axis[] revenueXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] revenueYAxes = Array.Empty<Axis>();

        // Pie series for revenue distribution option (explicit properties to avoid source-gen issues)
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

        public ReportViewModel(IReportService reportService)
     {
            _reportService = reportService;

            SelectedGroupBy = GroupByOptions.FirstOrDefault(g => g.Value == "MONTH");

  // default: năm hiện tại
         var now = DateTime.Now;
            FromDateText = new DateTime(now.Year,1,1).ToString("yyyy-MM-dd");
   ToDateText = new DateTime(now.Year,12,31).ToString("yyyy-MM-dd");
    TopText = "5";

            // Initialize default Y axes so chart control won't throw when no data yet
ProductYAxes = new[] { new Axis { Name = "Quantity" } };
            RevenueYAxes = new[] { new Axis { Name = "Amount" } };
            // Initialize default X axes as well to ensure both axes arrays contain at least one element
  ProductXAxes = new[] { new Axis { Labels = Array.Empty<string>() } };
            RevenueXAxes = new[] { new Axis { Labels = Array.Empty<string>() } };

      _ = LoadReportAsync();
        }

        private static DateTime? ParsePeriodToDateTime(string period)
        {
         if (string.IsNullOrWhiteSpace(period)) return null;
        // Try ISO/UTC first
            if (DateTimeOffset.TryParse(period, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
     {
     return dto.DateTime;
   }
     // Try plain date
   if (DateTime.TryParseExact(period, new[] { "yyyy-MM-dd", "yyyy-MM", "yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
     return dt;
            }
   // fallback
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
     "WEEK" =>
 // show week as yyyy-Www where ww is ISO week number
                ISOWeek.GetYear(dt.Value) + "-W" + ISOWeek.GetWeekOfYear(dt.Value).ToString("D2"),
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
            // Cancel previous request if any
   _loadCts?.Cancel();
        _loadCts?.Dispose();
            _loadCts = new CancellationTokenSource();
  var cancellationToken = _loadCts.Token;

            if (IsBusy) return;
            IsBusy = true;
    ErrorMessage = string.Empty;

          try
   {
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
          if (int.TryParse(TopText, out var topVal) && topVal >0)
      top = topVal;

     var opt = new ReportQueryOptions
         {
        FromDate = from.Value,
         ToDate = to.Value,
GroupBy = SelectedGroupBy?.Value ?? "MONTH",
        Top = top,
       CategoryId = null
  };

    // Pass cancellationToken to service calls (requires service update)
 var productTask = _reportService.GetProductSalesSeriesAsync(opt);
           var revenueTask = _reportService.GetRevenueProfitSeriesAsync(opt);

      await Task.WhenAll(productTask, revenueTask);

      // Check if cancelled
          cancellationToken.ThrowIfCancellationRequested();

       var productResult = productTask.Result;
           var revenueResult = revenueTask.Result;

   Debug.WriteLine($"[ReportViewModel] productResult.Success={productResult?.Success} revenueResult.Success={revenueResult?.Success}");
             Debug.WriteLine($"[ReportViewModel] product count={(productResult?.Data?.Count ??0)} revenue points count={(revenueResult?.Data?.Count ??0)}");

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

     void UpdateChartsAndErrors()
      {
            Debug.WriteLine("[ReportViewModel] UpdateChartsAndErrors running on UI thread: " + (App.MainWindow?.DispatcherQueue != null));
         try
        {
     BuildProductChart(productResult.Data ?? new());
            BuildRevenueChart(revenueResult.Data ?? new());
    }
          catch (Exception ex)
          {
     Debug.WriteLine("[ReportViewModel] Exception in UpdateChartsAndErrors: " + ex);
           ErrorMessage = ex.Message;
    OnPropertyChanged(nameof(HasError));
        }
    }

    // Try to marshal to UI thread using MainWindow.DispatcherQueue if available
           if (App.MainWindow?.DispatcherQueue != null)
       {
       bool enqueued = App.MainWindow.DispatcherQueue.TryEnqueue(UpdateChartsAndErrors);
                 Debug.WriteLine($"[ReportViewModel] DispatcherQueue.TryEnqueue returned {enqueued}");
        }
   else
     {
        // Fallback: run on current thread
    try
 {
                UpdateChartsAndErrors();
          }
       catch (Exception ex)
               {
            Debug.WriteLine("[ReportViewModel] Exception in UpdateChartsAndErrors: " + ex);
  ErrorMessage = ex.Message;
            OnPropertyChanged(nameof(HasError));
    }
   }
            }
 catch (OperationCanceledException)
      {
      Debug.WriteLine("[ReportViewModel] LoadReportAsync cancelled");
            // Ignore cancellation
            }
  catch (Exception ex)
     {
        ErrorMessage = ex.Message;
        Debug.WriteLine("[ReportViewModel] Exception: " + ex);
   }
            finally
   {
      IsBusy = false;
         OnPropertyChanged(nameof(HasError));
 }
        }

private void BuildProductChart(System.Collections.Generic.List<ProductSalesSeriesDto> seriesData)
        {
      Debug.WriteLine($"[ReportViewModel] BuildProductChart called with seriesData.Count={seriesData?.Count}");

 if (seriesData == null || seriesData.Count ==0)
         {
       ProductSeries = Array.Empty<ISeries>();
                ProductXAxes = new[] { new Axis { Labels = Array.Empty<string>() } };
                ProductYAxes = new[] { new Axis { Name = "Quantity" } };
Debug.WriteLine("[ReportViewModel] No product series, cleared chart.");
         return;
            }

  // Collect all periods and parse to DateTime order
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

    // Define distinct colors for each product
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
       .Select(period => (double)(s.Points.FirstOrDefault(p => p.Period == period)?.Value ??0))
        .ToArray();

     Debug.WriteLine($"[ReportViewModel] Series '{s.Name}' values: [{string.Join(',', values.Take(5))}]");

         return new LineSeries<double>
      {
                  Name = s.Name,
       Values = values,
     GeometrySize = 15,   // Larger markers for visibility
GeometryStroke = new SolidColorPaint(colors[index % colors.Length]) { StrokeThickness = 2 },
    GeometryFill = new SolidColorPaint(colors[index % colors.Length]),
          Stroke = new SolidColorPaint(colors[index % colors.Length]) { StrokeThickness = 3 },
             Fill = null,
 LineSmoothness = 0,  // Straight lines (set to 0.5-1 for smooth curves if desired)
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
 MinLimit = 0  // Start Y axis from 0
            }
            };

    Debug.WriteLine($"[ReportViewModel] ProductSeries set with {ProductSeries.Length} series");
        }

      private void BuildRevenueChart(System.Collections.Generic.List<RevenueProfitPointDto> points)
        {
            Debug.WriteLine($"[ReportViewModel] BuildRevenueChart called with points.Count={points?.Count}");

     if (points == null || points.Count ==0)
            {
                RevenueSeries = Array.Empty<ISeries>();
                RevenueXAxes = new[] { new Axis { Labels = Array.Empty<string>() } };
                RevenueYAxes = new[] { new Axis { Name = "Amount" } };
                RevenuePieSeries = Array.Empty<ISeries>();
                Debug.WriteLine("[ReportViewModel] No revenue points, cleared chart.");
                return;
}

            // parse and order by date
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
    LabelsRotation =45
           }
     };
            RevenueYAxes = new[] { new Axis { Name = "Amount" } };

     // Build pie: revenue distribution across labels (only if multiple slices)
       if (revenueValues.Length >1)
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
            if (DateTime.TryParseExact(
        text.Trim(),
          "yyyy-MM-dd",
   CultureInfo.InvariantCulture,
     DateTimeStyles.None,
         out var dt))
  {
       return dt.Date;
     }
          return null;
        }
    }
}
