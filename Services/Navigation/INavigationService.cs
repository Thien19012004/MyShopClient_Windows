using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyShopClient.Services.Navigation
{
    public interface INavigationService
    {
        void NavigateToLogin();
        void NavigateToMainShell();
        void NavigateToConfig();
        void NavigateToProducts();
        void NavigateToOrders();
        void NavigateToCustomers();
        void NavigateToReports();
        void NavigateToPromotions();
    }
}
