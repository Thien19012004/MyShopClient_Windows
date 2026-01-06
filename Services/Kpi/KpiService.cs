using MyShopClient.Infrastructure.GraphQL;
using MyShopClient.Models;
using MyShopClient.Models.Kpi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShopClient.Services.Kpi
{
    public class KpiService : IKpiService
    {
 private readonly IGraphQLClient _gql;

        public KpiService(IGraphQLClient gql)
        {
         _gql = gql;
      }

        private static ApiResult<T> Failure<T>(string message, int statusCode = 500)
        {
            return ApiResult<T>.CreateFailure(message, statusCode);
   }

    #region Tiers

        private class KpiTiersRoot { public ApiResult<KpiTierPageResult>? KpiTiers { get; set; } }
        private class CreateKpiTierRoot { public ApiResult<KpiTierDto>? CreateKpiTier { get; set; } }
        private class UpdateKpiTierRoot { public ApiResult<object>? UpdateKpiTier { get; set; } }
        private class DeleteKpiTierRoot { public ApiResult<object>? DeleteKpiTier { get; set; } }

        public async Task<ApiResult<KpiTierPageResult>> GetKpiTiersAsync(int page =1, int pageSize =100)
 {
 try
 {
 var variables = new
 {
 pagination = new { page, pageSize },
 filter = (object?)null
 };
 var data = await _gql.SendAsync<KpiTiersRoot>(KpiQueries.ListKpiTiersQuery, variables);
 return data?.KpiTiers ?? Failure<KpiTierPageResult>("No kpiTiers field");
 }
 catch (Exception ex)
 {
 return Failure<KpiTierPageResult>(ex.Message);
 }
 }

        public async Task<ApiResult<KpiTierDto>> CreateKpiTierAsync(CreateKpiTierInput input)
        {
     try
            {
         var variables = new { input };
           var data = await _gql.SendAsync<CreateKpiTierRoot>(KpiQueries.CreateKpiTierMutation, variables);
       return data?.CreateKpiTier ?? Failure<KpiTierDto>("No createKpiTier field");
            }
            catch (Exception ex)
    {
          return Failure<KpiTierDto>(ex.Message);
         }
    }

        public async Task<ApiResult<bool>> UpdateKpiTierAsync(int kpiTierId, UpdateKpiTierInput input)
        {
       try
   {
         var variables = new { id = kpiTierId, input };
   var data = await _gql.SendAsync<UpdateKpiTierRoot>(KpiQueries.UpdateKpiTierMutation, variables);
           var result = data?.UpdateKpiTier;
      if (result == null) return Failure<bool>("No updateKpiTier field");
              return new ApiResult<bool>
     {
 Success = result.Success,
     StatusCode = result.StatusCode,
  Message = result.Message,
             Data = result.Success
           };
         }
        catch (Exception ex)
            {
              return Failure<bool>(ex.Message);
   }
        }

      public async Task<ApiResult<bool>> DeleteKpiTierAsync(int kpiTierId)
        {
        try
 {
     var variables = new { id = kpiTierId };
       var data = await _gql.SendAsync<DeleteKpiTierRoot>(KpiQueries.DeleteKpiTierMutation, variables);
   var result = data?.DeleteKpiTier;
      if (result == null) return Failure<bool>("No deleteKpiTier field");
                return new ApiResult<bool>
     {
            Success = result.Success,
           StatusCode = result.StatusCode,
  Message = result.Message,
     Data = result.Success
       };
 }
     catch (Exception ex)
            {
       return Failure<bool>(ex.Message);
   }
        }

        #endregion

        #region Targets

        private class SaleKpiTargetsRoot { public ApiResult<SaleKpiTargetPageResult>? SaleKpiTargets { get; set; } }
   private class SetMonthlyTargetRoot { public ApiResult<SaleKpiTargetDto>? SetMonthlyTarget { get; set; } }

        public async Task<ApiResult<SaleKpiTargetPageResult>> GetSaleKpiTargetsAsync(int? saleId, int? year, int? month, int page =1, int pageSize =100)
        {
         try
            {
 var variables = new
 {
 pagination = new { page, pageSize },
 filter = new { saleId, year, month }
 };
 var data = await _gql.SendAsync<SaleKpiTargetsRoot>(KpiQueries.ListSaleKpiTargetsQuery, variables);
 return data?.SaleKpiTargets ?? Failure<SaleKpiTargetPageResult>("No saleKpiTargets field");
 }
 catch (Exception ex)
 {
 return Failure<SaleKpiTargetPageResult>(ex.Message);
 }
 }

        public async Task<ApiResult<SaleKpiTargetDto>> SetMonthlyTargetAsync(SetMonthlyTargetInput input)
        {
 try
         {
          var variables = new { input };
                var data = await _gql.SendAsync<SetMonthlyTargetRoot>(KpiQueries.SetMonthlyTargetMutation, variables);
       return data?.SetMonthlyTarget ?? Failure<SaleKpiTargetDto>("No setMonthlyTarget field");
    }
    catch (Exception ex)
            {
  return Failure<SaleKpiTargetDto>(ex.Message);
      }
        }

        #endregion

     #region Dashboard

        private class KpiDashboardRoot { public ApiResult<KpiDashboardDto>? KpiDashboard { get; set; } }

        public async Task<ApiResult<KpiDashboardDto>> GetKpiDashboardAsync(int saleId, int? year = null, int? month = null)
        {
    try
       {
    var variables = new { input = new { saleId, year, month } };
        var data = await _gql.SendAsync<KpiDashboardRoot>(KpiQueries.KpiDashboardQuery, variables);
           return data?.KpiDashboard ?? Failure<KpiDashboardDto>("No kpiDashboard field");
        }
            catch (Exception ex)
       {
            return Failure<KpiDashboardDto>(ex.Message);
            }
      }

      #endregion

  #region Commission

   private class CalculateKpiRoot { public ApiResult<List<KpiCommissionDto>>? CalculateMonthlyKpi { get; set; } }
        private class KpiCommissionsRoot { public ApiResult<KpiCommissionPageResult>? KpiCommissions { get; set; } }

        public async Task<ApiResult<List<KpiCommissionDto>>> CalculateMonthlyKpiAsync(CalculateMonthlyKpiInput input)
        {
       try
   {
   var variables = new { input };
                var data = await _gql.SendAsync<CalculateKpiRoot>(KpiQueries.CalculateMonthlyKpiMutation, variables);
        return data?.CalculateMonthlyKpi ?? Failure<List<KpiCommissionDto>>("No calculateMonthlyKpi field");
            }
            catch (Exception ex)
            {
return Failure<List<KpiCommissionDto>>(ex.Message);
            }
   }

    public async Task<ApiResult<KpiCommissionPageResult>> GetKpiCommissionsAsync(int? saleId, int? year, int? month, int page =1, int pageSize =100)
        {
         try
      {
           var variables = new { pagination = new { page, pageSize }, filter = new { saleId, year, month } };
  var data = await _gql.SendAsync<KpiCommissionsRoot>(KpiQueries.ListKpiCommissionsQuery, variables);
        return data?.KpiCommissions ?? Failure<KpiCommissionPageResult>("No kpiCommissions field");
     }
            catch (Exception ex)
          {
    return Failure<KpiCommissionPageResult>(ex.Message);
       }
        }

        #endregion
    }
}
