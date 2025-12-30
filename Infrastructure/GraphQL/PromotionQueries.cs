namespace MyShopClient.Infrastructure.GraphQL
{
 public static class PromotionQueries
 {
 public const string GetPromotionsQuery = @"
query($pagination: PaginationInput, $filter: PromotionFilterInput) {
 promotions(pagination: $pagination, filter: $filter) {
 statusCode
 success
 message
 data {
 page
 pageSize
 totalItems
 totalPages
 items {
 promotionId
 name
 discountPercent
 startDate
 endDate
 scope
 productCount
 categoryCount
 productIds
 categoryIds
 }
 }
 }
}";

 public const string GetPromotionByIdQuery = @"
query($id: Int!) {
 promotionById(promotionId: $id) {
 statusCode
 success
 message
 data {
 promotionId
 name
 discountPercent
 startDate
 endDate
 scope
 productIds
 categoryIds
 }
 }
}";

 public const string CreatePromotionMutation = @"
mutation($input: CreatePromotionInput!) {
 createPromotion(input: $input) {
 statusCode
 success
 message
 data {
 promotionId
 name
 discountPercent
 startDate
 endDate
 scope
 productIds
 categoryIds
 }
 }
}";

 public const string UpdatePromotionMutation = @"
mutation($id: Int!, $input: UpdatePromotionInput!) {
 updatePromotion(promotionId: $id, input: $input) {
 statusCode
 success
 message
 data {
 promotionId
 name
 discountPercent
 startDate
 endDate
 scope
 productIds
 categoryIds
 }
 }
}";

 public const string DeletePromotionMutation = @"
mutation($id: Int!) {
 deletePromotion(promotionId: $id) {
 statusCode
 success
 message
 }
}";
 }
}
