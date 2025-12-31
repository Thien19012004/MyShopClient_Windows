using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyShopClient.Services.AppSettings
{
    public interface IAppSettingsService
    {
        int ProductsPageSize { get; set; }
        int OrdersPageSize { get; set; }
        int CustomersPageSize { get; set; }
        int PromotionsPageSize { get; set; }
        int ReportsPageSize { get; set; }

        string? LastVisitedPage { get; set; }
    }
}
