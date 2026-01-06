using MyShopClient.Models;
using MyShopClient.Models.Kpi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShopClient.Services.Kpi
{
  public interface IKpiService
    {
        // Tiers
        Task<ApiResult<KpiTierPageResult>> GetKpiTiersAsync(int page = 1, int pageSize = 100);
        Task<ApiResult<KpiTierDto>> CreateKpiTierAsync(CreateKpiTierInput input);
        Task<ApiResult<bool>> UpdateKpiTierAsync(int kpiTierId, UpdateKpiTierInput input);
        Task<ApiResult<bool>> DeleteKpiTierAsync(int kpiTierId);

        // Targets
        Task<ApiResult<SaleKpiTargetPageResult>> GetSaleKpiTargetsAsync(int? saleId, int? year, int? month, int page = 1, int pageSize = 100);
Task<ApiResult<SaleKpiTargetDto>> SetMonthlyTargetAsync(SetMonthlyTargetInput input);

  // Dashboard
        Task<ApiResult<KpiDashboardDto>> GetKpiDashboardAsync(int saleId, int? year = null, int? month = null);

    // Commission
        Task<ApiResult<List<KpiCommissionDto>>> CalculateMonthlyKpiAsync(CalculateMonthlyKpiInput input);
     Task<ApiResult<KpiCommissionPageResult>> GetKpiCommissionsAsync(int? saleId, int? year, int? month, int page = 1, int pageSize = 100);
    }
}
