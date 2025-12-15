namespace MyShopClient.Helpers
{
    public static class FormatHelpers
    {
        public static string FormatOrderId(int id) => $"#{id}";

        public static string FormatOrderDate(System.DateTime date)
            => date.ToString("dd MMM, yyyy");
    }
}
