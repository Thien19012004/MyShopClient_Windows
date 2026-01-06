using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models.Kpi;
using MyShopClient.Services.Auth;
using MyShopClient.Services.Kpi;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels.Kpi
{
    public class SaleOption
    {
        public int SaleId { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        public override string ToString() => DisplayName;
    }

    public class KpiViewModel : ObservableObject
    {
        private readonly IKpiService _kpiService;
        private readonly IAuthService _authService;

        public KpiViewModel(IKpiService kpiService, IAuthService authService)
        {
            _kpiService = kpiService;
            _authService = authService;

            var now = DateTime.Now;
            _selectedYear = now.Year;
            _selectedMonth = now.Month;

            // Initialize years (5 years back, current, 1 year forward)
            for (int y = now.Year - 5; y <= now.Year + 1; y++)
                Years.Add(y);

            // Months 1-12
            for (int m = 1; m <= 12; m++)
                Months.Add(m);

            UpdateUserInfo();

            LoadDashboardCommand = new AsyncRelayCommand(LoadDashboardAsync);
            LoadCommissionHistoryCommand = new AsyncRelayCommand(LoadCommissionHistoryAsync);
            LoadTiersCommand = new AsyncRelayCommand(LoadTiersAsync);
            CreateTierCommand = new AsyncRelayCommand(CreateTierAsync);
            UpdateTierCommand = new AsyncRelayCommand(UpdateTierAsync);
            LoadTargetsCommand = new AsyncRelayCommand(LoadTargetsAsync);
            SetTargetCommand = new AsyncRelayCommand(SetTargetAsync);
            CalculateKpiCommand = new AsyncRelayCommand(CalculateKpiAsync);
            InitializeCommand = new AsyncRelayCommand(InitializeAsync);
            LoadSalesCommand = new AsyncRelayCommand(LoadSalesAsync);
        }

        private void UpdateUserInfo()
        {
            var user = _authService.CurrentUser;
            if (user != null)
            {
                CurrentUserId = user.UserId;
                CurrentUserName = user.FullName ?? user.Username;
                IsAdmin = user.Roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
   r.Equals("Moderator", StringComparison.OrdinalIgnoreCase));
                IsSale = user.Roles.Any(r => r.Equals("Sale", StringComparison.OrdinalIgnoreCase));
            }
        }

        #region Commands

        public IAsyncRelayCommand LoadDashboardCommand { get; }
        public IAsyncRelayCommand LoadCommissionHistoryCommand { get; }
        public IAsyncRelayCommand LoadTiersCommand { get; }
        public IAsyncRelayCommand CreateTierCommand { get; }
        public IAsyncRelayCommand UpdateTierCommand { get; }
        public IAsyncRelayCommand LoadTargetsCommand { get; }
        public IAsyncRelayCommand SetTargetCommand { get; }
        public IAsyncRelayCommand CalculateKpiCommand { get; }
        public IAsyncRelayCommand InitializeCommand { get; }
        public IAsyncRelayCommand LoadSalesCommand { get; }

        #endregion

        #region Properties

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private int _currentUserId;
        public int CurrentUserId
        {
            get => _currentUserId;
            set => SetProperty(ref _currentUserId, value);
        }

        private string _currentUserName = string.Empty;
        public string CurrentUserName
        {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        private bool _isAdmin;
        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                if (SetProperty(ref _isAdmin, value))
                {
                    OnPropertyChanged(nameof(IsSaleOnly));
                }
            }
        }

        private bool _isSale;
        public bool IsSale
        {
            get => _isSale;
            set
            {
                if (SetProperty(ref _isSale, value))
                {
                    OnPropertyChanged(nameof(IsSaleOnly));
                }
            }
        }

        public bool IsSaleOnly => IsSale && !IsAdmin;

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (SetProperty(ref _selectedYear, value))
                    _ = LoadDashboardAsync();
            }
        }

        private int _selectedMonth;
        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (SetProperty(ref _selectedMonth, value))
                    _ = LoadDashboardAsync();
            }
        }

        public ObservableCollection<int> Years { get; } = new();
        public ObservableCollection<int> Months { get; } = new();

        // Sales list for Admin to select
        public ObservableCollection<SaleOption> Sales { get; } = new();

        private SaleOption? _selectedSale;
        public SaleOption? SelectedSale
        {
            get => _selectedSale;
            set
            {
                if (SetProperty(ref _selectedSale, value))
                    _ = LoadDashboardAsync();
            }
        }

        // Dashboard data
        private KpiDashboardDto? _dashboardData;
        public KpiDashboardDto? DashboardData
        {
            get => _dashboardData;
            set => SetProperty(ref _dashboardData, value);
        }

        private decimal _targetRevenue;
        public decimal TargetRevenue
        {
            get => _targetRevenue;
            set => SetProperty(ref _targetRevenue, value);
        }

        private decimal _actualRevenue;
        public decimal ActualRevenue
        {
            get => _actualRevenue;
            set => SetProperty(ref _actualRevenue, value);
        }

        private decimal _progressPercent;
        public decimal ProgressPercent
        {
            get => _progressPercent;
            set => SetProperty(ref _progressPercent, value);
        }

        private decimal _remainingRevenue;
        public decimal RemainingRevenue
        {
            get => _remainingRevenue;
            set => SetProperty(ref _remainingRevenue, value);
        }

        private decimal _estimatedBaseCommission;
        public decimal EstimatedBaseCommission
        {
            get => _estimatedBaseCommission;
            set => SetProperty(ref _estimatedBaseCommission, value);
        }

        private decimal _estimatedBonusCommission;
        public decimal EstimatedBonusCommission
        {
            get => _estimatedBonusCommission;
            set => SetProperty(ref _estimatedBonusCommission, value);
        }

        private decimal _estimatedTotalCommission;
        public decimal EstimatedTotalCommission
        {
            get => _estimatedTotalCommission;
            set => SetProperty(ref _estimatedTotalCommission, value);
        }

        private string? _currentTierName;
        public string? CurrentTierName
        {
            get => _currentTierName;
            set => SetProperty(ref _currentTierName, value);
        }

        private int _totalOrdersThisMonth;
        public int TotalOrdersThisMonth
        {
            get => _totalOrdersThisMonth;
            set => SetProperty(ref _totalOrdersThisMonth, value);
        }

        private int _totalOrdersPaid;
        public int TotalOrdersPaid
        {
            get => _totalOrdersPaid;
            set => SetProperty(ref _totalOrdersPaid, value);
        }

        // Commission history
        public ObservableCollection<KpiCommissionDto> CommissionHistory { get; } = new();

        // Tiers (Admin tab)
        public ObservableCollection<KpiTierDto> Tiers { get; } = new();

        // Targets (Admin tab)
        public ObservableCollection<SaleKpiTargetDto> Targets { get; } = new();

        // For admin: selected sale for viewing dashboard (legacy - keep for compatibility)
        private int? _selectedSaleIdForDashboard;
        public int? SelectedSaleIdForDashboard
        {
            get => _selectedSaleIdForDashboard;
            set => SetProperty(ref _selectedSaleIdForDashboard, value);
        }

        // New tier input
        private string _newTierName = string.Empty;
        public string NewTierName
        {
            get => _newTierName;
            set => SetProperty(ref _newTierName, value);
        }

        private double _newTierMinRevenue;
        public double NewTierMinRevenue
        {
            get => _newTierMinRevenue;
            set => SetProperty(ref _newTierMinRevenue, value);
        }

        private double _newTierBonusPercent;
        public double NewTierBonusPercent
        {
            get => _newTierBonusPercent;
            set => SetProperty(ref _newTierBonusPercent, value);
        }

        private string _newTierDescription = string.Empty;
        public string NewTierDescription
        {
            get => _newTierDescription;
            set => SetProperty(ref _newTierDescription, value);
        }

        private double _newTierDisplayOrder;
        public double NewTierDisplayOrder
        {
            get => _newTierDisplayOrder;
            set => SetProperty(ref _newTierDisplayOrder, value);
        }

        // Edit tier
        private KpiTierDto? _selectedTier;
        public KpiTierDto? SelectedTier
        {
            get => _selectedTier;
            set => SetProperty(ref _selectedTier, value);
        }

        // Set target input
        private double _targetSaleId;
        public double TargetSaleId
        {
            get => _targetSaleId;
            set => SetProperty(ref _targetSaleId, value);
        }

        private double _targetRevenueInput;
        public double TargetRevenueInput
        {
            get => _targetRevenueInput;
            set => SetProperty(ref _targetRevenueInput, value);
        }

        #endregion

        #region Load Sales (for Admin ComboBox)

        public async Task LoadSalesAsync()
        {
            if (!IsAdmin) return;

            try
            {
                // Load sales from KPI targets or commissions
                var result = await _kpiService.GetKpiCommissionsAsync(null, null, null, 1, 100);
                if (result.Success && result.Data != null)
                {
                    Sales.Clear();

                    // Add current user first
                    Sales.Add(new SaleOption
                    {
                        SaleId = CurrentUserId,
                        DisplayName = $"{CurrentUserName} (Me)"
                    });

                    // Get unique sales from commissions
                    var uniqueSales = result.Data.Items
              .Where(c => c.SaleId != CurrentUserId)
                  .GroupBy(c => c.SaleId)
                  .Select(g => g.First())
                   .ToList();

                    foreach (var sale in uniqueSales)
                    {
                        Sales.Add(new SaleOption
                        {
                            SaleId = sale.SaleId,
                            DisplayName = sale.SaleName
                        });
                    }

                    // Also try to get from targets
                    var targetsResult = await _kpiService.GetSaleKpiTargetsAsync(null, null, null, 1, 100);
                    if (targetsResult.Success && targetsResult.Data != null)
                    {
                        var existingIds = Sales.Select(s => s.SaleId).ToHashSet();
                        var additionalSales = targetsResult.Data.Items
                         .Where(t => !existingIds.Contains(t.SaleId))
                        .GroupBy(t => t.SaleId)
                         .Select(g => g.First())
                                         .ToList();

                        foreach (var sale in additionalSales)
                        {
                            Sales.Add(new SaleOption
                            {
                                SaleId = sale.SaleId,
                                DisplayName = sale.SaleName
                            });
                        }
                    }

                    // Select current user by default
                    SelectedSale = Sales.FirstOrDefault(s => s.SaleId == CurrentUserId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KPI] Error loading sales: {ex.Message}");
            }
        }

        #endregion

        #region Dashboard

        public async Task LoadDashboardAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                // Use selected sale if admin, otherwise current user
                int saleId = (IsAdmin && SelectedSale != null)
               ? SelectedSale.SaleId
                   : CurrentUserId;

                var result = await _kpiService.GetKpiDashboardAsync(saleId, SelectedYear, SelectedMonth);
                if (result.Success && result.Data != null)
                {
                    DashboardData = result.Data;
                    TargetRevenue = result.Data.TargetRevenue;
                    ActualRevenue = result.Data.ActualRevenue;
                    ProgressPercent = result.Data.Progress;
                    RemainingRevenue = result.Data.RemainingRevenue;
                    EstimatedBaseCommission = result.Data.EstimatedBaseCommission;
                    EstimatedBonusCommission = result.Data.EstimatedBonusCommission;
                    EstimatedTotalCommission = result.Data.EstimatedTotalCommission;
                    CurrentTierName = result.Data.CurrentKpiTierName ?? "No tier achieved";
                    TotalOrdersThisMonth = result.Data.TotalOrdersThisMonth;
                    TotalOrdersPaid = result.Data.TotalOrdersPaid;
                }
                else
                {
                    ErrorMessage = result.Message ?? "Cannot load KPI data.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadCommissionHistoryAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                int? saleId = IsAdmin ? null : CurrentUserId;
                var result = await _kpiService.GetKpiCommissionsAsync(saleId, SelectedYear, null, 1, 50);
                if (result.Success && result.Data != null)
                {
                    CommissionHistory.Clear();
                    foreach (var item in result.Data.Items)
                    {
                        CommissionHistory.Add(item);
                    }
                }
                else
                {
                    ErrorMessage = result.Message ?? "Failed to load commission history.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Tiers (Admin)

        public async Task LoadTiersAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var result = await _kpiService.GetKpiTiersAsync(1, 100);
                if (result.Success && result.Data != null)
                {
                    Tiers.Clear();
                    foreach (var tier in result.Data.Items.OrderBy(t => t.DisplayOrder))
                    {
                        Tiers.Add(tier);
                    }
                }
                else
                {
                    ErrorMessage = result.Message ?? "Cannot load tier list.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CreateTierAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTierName)) return;
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var input = new CreateKpiTierInput
                {
                    Name = NewTierName,
                    MinRevenue = (decimal)NewTierMinRevenue,
                    BonusPercent = (decimal)NewTierBonusPercent,
                    Description = NewTierDescription,
                    DisplayOrder = (int)NewTierDisplayOrder
                };

                var result = await _kpiService.CreateKpiTierAsync(input);
                if (result.Success)
                {
                    NewTierName = string.Empty;
                    NewTierMinRevenue = 0;
                    NewTierBonusPercent = 0;
                    NewTierDescription = string.Empty;
                    NewTierDisplayOrder = 0;
                    await LoadTiersAsync();
                }
                else
                {
                    ErrorMessage = result.Message ?? "Cannot create tier.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task UpdateTierAsync()
        {
            if (SelectedTier == null) return;
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var input = new UpdateKpiTierInput
                {
                    Name = SelectedTier.Name,
                    MinRevenue = SelectedTier.MinRevenue,
                    BonusPercent = SelectedTier.BonusPercent,
                    Description = SelectedTier.Description,
                    DisplayOrder = SelectedTier.DisplayOrder
                };

                var result = await _kpiService.UpdateKpiTierAsync(SelectedTier.KpiTierId, input);
                if (result.Success)
                {
                    await LoadTiersAsync();
                }
                else
                {
                    ErrorMessage = result.Message ?? "Cannot update tier.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteTierAsync(KpiTierDto tier)
        {
            if (tier == null) return;
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var result = await _kpiService.DeleteKpiTierAsync(tier.KpiTierId);
                if (result.Success)
                {
                    await LoadTiersAsync();
                }
                else
                {
                    ErrorMessage = result.Message ?? "Cannot delete tier.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Targets (Admin)

        public async Task LoadTargetsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var result = await _kpiService.GetSaleKpiTargetsAsync(null, SelectedYear, SelectedMonth, 1, 100);
                if (result.Success && result.Data != null)
                {
                    Targets.Clear();
                    foreach (var target in result.Data.Items)
                    {
                        Targets.Add(target);
                    }
                }
                else
                {
                    ErrorMessage = result.Message ?? "Cannot load targets.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task SetTargetAsync()
        {
            if (TargetSaleId <= 0 || TargetRevenueInput <= 0) return;
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var input = new SetMonthlyTargetInput
                {
                    SaleId = (int)TargetSaleId,
                    Year = SelectedYear,
                    Month = SelectedMonth,
                    TargetRevenue = (decimal)TargetRevenueInput
                };

                var result = await _kpiService.SetMonthlyTargetAsync(input);
                if (result.Success)
                {
                    TargetSaleId = 0;
                    TargetRevenueInput = 0;
                    await LoadTargetsAsync();
                }
                else
                {
                    ErrorMessage = result.Message ?? "Cannot set target.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Calculate (Admin)

        public async Task CalculateKpiAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var input = new CalculateMonthlyKpiInput
                {
                    Year = SelectedYear,
                    Month = SelectedMonth,
                    SaleId = null // Calculate for all sales
                };

                var result = await _kpiService.CalculateMonthlyKpiAsync(input);
                if (result.Success)
                {
                    // Reload commission history to see results
                    await LoadCommissionHistoryAsync();
                }
                else
                {
                    ErrorMessage = result.Message ?? "Cannot calculate KPI.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Initialize

        public async Task InitializeAsync()
        {
            UpdateUserInfo();

            if (IsAdmin)
            {
                await LoadSalesAsync();
                await LoadTiersAsync();
            }

            await LoadDashboardAsync();
        }

        #endregion
    }
}
