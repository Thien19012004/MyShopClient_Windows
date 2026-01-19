namespace MyShopClient.Infrastructure.GraphQL
{
    public static class CategoryQueries
    {
        public const string GetCategoriesQuery = @"
query GetCategories($page:Int!, $pageSize:Int!, $search:String) {
 categories(
 pagination: { page: $page, pageSize: $pageSize }
 search: $search
 ) {
 statusCode
 success
 message
 data {
 page
 pageSize
 totalItems
 totalPages
 items {
 categoryId
 name
 description
 productCount
 }
 }
 }
}";

        public const string CreateCategoryMutation = @"
mutation CreateCategory($name:String!, $description:String) {
 createCategory(
 input: {
 name: $name,
 description: $description
 }
 ) {
 statusCode
 success
 message
 data {
 categoryId
 name
 description
 }
 }
}";

        public const string UpdateCategoryMutation = @"
mutation UpdateCategory($id:Int!, $name:String!, $description:String) {
 updateCategory(
 categoryId: $id,
 input: {
 name: $name,
 description: $description
 }
 ) {
 statusCode
 success
 message
 data {
 categoryId
 name
 description
 }
 }
}";

        public const string DeleteCategoryMutation = @"
mutation DeleteCategory($id:Int!) {
 deleteCategory(categoryId: $id) {
 statusCode
 success
 message
 data {
 categoryId
 name
 }
 }
}";
    }
}
