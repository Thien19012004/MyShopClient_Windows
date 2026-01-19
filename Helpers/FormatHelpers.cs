namespace MyShopClient.Helpers
{
    public static class FormatHelpers
    {
        public static string FormatOrderId(int id) => $"#{id}";

        public static string FormatOrderDate(System.DateTime date)
        {
          
            System.DateTime local;

            if (date.Kind == System.DateTimeKind.Local)
            {
                local = date;
            }
            else if (date.Kind == System.DateTimeKind.Utc)
            {
                local = date.ToLocalTime();
            }
            else 
            {
                local = System.DateTime.SpecifyKind(date, System.DateTimeKind.Utc).ToLocalTime();
            }

            return local.ToString("dd MMM, yyyy");
        }
    }
}
