namespace MyShopClient.Infrastructure.GraphQL
{
    public static class KpiQueries
    {
        
        public const string ListKpiTiersQuery = @"
query($pagination: PaginationInput, $filter: KpiTierFilterInput) {
    kpiTiers(pagination: $pagination, filter: $filter) {
    statusCode
        success
        message
        data {
            page
  pageSize
  totalItems
      totalPages
        items {
     kpiTierId
   name
    minRevenue
 bonusPercent
          description
          displayOrder
  }
   }
    }
}";

      
        public const string CreateKpiTierMutation = @"
mutation($input: CreateKpiTierInput!) {
    createKpiTier(input: $input) {
        statusCode
success
      message
        data {
            kpiTierId
            name
  minRevenue
        bonusPercent
 description
       displayOrder
        }
    }
}";

  
   public const string UpdateKpiTierMutation = @"
mutation($id: Int!, $input: UpdateKpiTierInput!) {
    updateKpiTier(kpiTierId: $id, input: $input) {
        statusCode
        success
     message
    }
}";

   
        public const string DeleteKpiTierMutation = @"
mutation($id: Int!) {
    deleteKpiTier(kpiTierId: $id) {
        statusCode
success
        message
    }
}";

      
        public const string SetMonthlyTargetMutation = @"
mutation($input: SetMonthlyTargetInput!) {
    setMonthlyTarget(input: $input) {
    statusCode
        success
        message
  data {
       saleKpiTargetId
            saleId
          saleName
   year
    month
  targetRevenue
            actualRevenue
       progress
            kpiTierId
            kpiTierName
     bonusAmount
            calculatedAt
            createdAt
        }
    }
}";

 
        public const string ListSaleKpiTargetsQuery = @"
query($pagination: PaginationInput, $filter: SaleKpiTargetFilterInput) {
    saleKpiTargets(pagination: $pagination, filter: $filter) {
    statusCode
 success
   message
    data {
  page
   pageSize
      totalItems
            totalPages
     items {
            saleKpiTargetId
      saleId
   saleName
    year
           month
     targetRevenue
         actualRevenue
      progress
         kpiTierName
   bonusAmount
    calculatedAt
      createdAt
         }
        }
    }
}";

     
        public const string KpiDashboardQuery = @"
query($input: KpiDashboardInput!) {
  kpiDashboard(input: $input) {
        statusCode
        success
    message
 data {
      saleId
  saleName
            currentYear
   currentMonth
        targetRevenue
            actualRevenue
            progress
    remainingRevenue
       estimatedBaseCommission
      estimatedBonusCommission
     estimatedTotalCommission
            currentKpiTierId
currentKpiTierName
 totalOrdersThisMonth
            totalOrdersPaid
         availableTiers {
                kpiTierId
    name
      minRevenue
    bonusPercent
             displayOrder
            }
        }
    }
}";

      
        public const string CalculateMonthlyKpiMutation = @"
mutation($input: CalculateMonthlyKpiInput!) {
    calculateMonthlyKpi(input: $input) {
   statusCode
   success
        message
        data {
kpiCommissionId
saleId
    saleName
      year
            month
  baseCommission
     bonusCommission
       totalCommission
     kpiTierId
            kpiTierName
            totalRevenue
    totalOrders
 calculatedAt
   }
    }
}";


      public const string ListKpiCommissionsQuery = @"
query($pagination: PaginationInput, $filter: KpiCommissionFilterInput) {
 kpiCommissions(pagination: $pagination, filter: $filter) {
 statusCode
 success
 message
 data {
 page
 pageSize
 totalItems
 totalPages
 items {
 kpiCommissionId
 saleId
 saleName
 year
 month
 baseCommission
 bonusCommission
 totalCommission
 kpiTierName
 totalRevenue
 totalOrders
 calculatedAt
 }
 }
 }
}";
    }
}
