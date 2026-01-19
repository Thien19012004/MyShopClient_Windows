namespace MyShopClient.Infrastructure.GraphQL
{
    public static class CustomerQueries
    {
        public const string GetCustomersQuery = @"
query GetCustomers($page:Int!, $pageSize:Int!, $search:String) {
 customers(
 pagination: { page: $page, pageSize: $pageSize }
 filter: { search: $search }
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
 customerId
 name
 phone
 email
 address
 orderCount
 }
 }
 }
}";

        public const string GetCustomerByIdQuery = @"
query GetCustomerById($customerId:Int!) {
 customerById(customerId: $customerId) {
 statusCode
 success
 message
 data {
 customerId
 name
 phone
 email
 address
 orderCount
 }
 }
}";

        public const string CreateCustomerMutation = @"
mutation CreateCustomer($name:String!, $phone:String!, $email:String!, $address:String!) {
 createCustomer(input: {
 name: $name,
 phone: $phone,
 email: $email,
 address: $address
 }) {
 statusCode
 success
 message
 data {
 customerId
 name
 phone
 email
 address
 orderCount
 }
 }
}";

        public const string UpdateCustomerMutation = @"
mutation UpdateCustomer($customerId:Int!, $name:String!, $phone:String!, $email:String!, $address:String!) {
 updateCustomer(customerId: $customerId, input: {
 name: $name,
 phone: $phone,
 email: $email,
 address: $address
 }) {
 statusCode
 success
 message
 data {
 customerId
 name
 phone
 email
 address
 orderCount
 }
 }
}";

        public const string DeleteCustomerMutation = @"
mutation DeleteCustomer($customerId:Int!) {
 deleteCustomer(customerId: $customerId) {
 statusCode
 success
 message
 }
}";
    }
}
