namespace MyShopClient.Infrastructure.GraphQL
{
 public static class ProductsQueries
 {
 public const string DeleteProductMutation = @"
mutation DeleteProduct($productId: Int!) {
 deleteProduct(productId: $productId) {
 statusCode
 success
 message
 }
}";

 public const string GetProductByIdQuery = @"
query GetProductById($productId: Int!) {
 productById(productId: $productId) {
 statusCode
 success
 message
 data {
 productId
 sku
 name
 importPrice
 salePrice
 stockQuantity
 description
 categoryId
 categoryName
 imagePaths
 }
 }
}";

 public const string CreateProductMutation = @"
mutation CreateProduct($input: CreateProductInput!) {
 createProduct(input: $input) {
 statusCode
 success
 message
 data {
 productId
 sku
 name
 salePrice
 stockQuantity
 }
 }
}";

 public const string UpdateProductMutation = @"
mutation UpdateProduct($productId: Int!, $input: UpdateProductInput!) {
 updateProduct(productId: $productId, input: $input) {
 statusCode
 success
 message
 data {
 productId
 sku
 name
 salePrice
 stockQuantity
 }
 }
}";

 // Template for GetProducts query. Use string.Format with values:
 //0: page,1: pageSize,2: searchLiteral,3: categoryLiteral,4: minPriceLiteral,5: maxPriceLiteral,6: sortFieldLiteral,7: ascLiteral
 public const string GetProductsTemplate = @"
query GetProducts {{
 products(
 pagination: {{ page: {0}, pageSize: {1} }}
 filter: {{ search: {2}, categoryId: {3}, minPrice: {4}, maxPrice: {5} }}
 sort: {{ field: {6}, asc: {7} }}
 ) {{
 statusCode
 success
 message
 data {{
 page
 pageSize
 totalItems
 totalPages
 items {{
 productId
 sku
 name
 salePrice
 importPrice
 stockQuantity
 categoryName
 }}
 }}
 }}
}}";
 }
}
