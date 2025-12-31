using MyShopClient.Controls;
using MyShopClient.Views;
using System;
using System.Collections.Generic;
using Microsoft.Win32;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace MyShopClient.Services.OnBoarding
{
    public interface IOnboardingService
    {
        Task TryRunIfFirstLaunchAsync(DashboardPage dashboardPage, Frame contentFrame, SidebarControl sidebar, OnboardingOverlay overlay);
        Task StartTourAsync(DashboardPage dashboardPage, Frame contentFrame, SidebarControl sidebar, OnboardingOverlay overlay);
        void MarkCompleted();
        bool IsCompleted { get; }
    }
}
