namespace MyShopClient.Infrastructure.GraphQL
{
 public static class AuthQueries
 {
 public const string LoginMutation = @"
mutation($username: String!, $password: String!) {
 login(input: { username: $username, password: $password }) {
 statusCode
 success
 message
 data {
 userId
 username
 fullName
 roles
 token
 }
 }
}";
 }
}
