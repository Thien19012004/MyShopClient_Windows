using Microsoft.Win32;
using MyShopClient.Services.AppSettings;
using System;
using System.Diagnostics;

namespace MyShopClient.Services.AppSettings
{
  

    public class AppSettingsService : IAppSettingsService
    {
        private const string CompanyKeyPath = @"Software\\MyShopClient";

        private const int DefaultPageSize = 10;

        public int ProductsPageSize { get => ReadInt(nameof(ProductsPageSize), DefaultPageSize); set => WriteInt(nameof(ProductsPageSize), value); }
        public int OrdersPageSize { get => ReadInt(nameof(OrdersPageSize), DefaultPageSize); set => WriteInt(nameof(OrdersPageSize), value); }
        public int CustomersPageSize { get => ReadInt(nameof(CustomersPageSize), DefaultPageSize); set => WriteInt(nameof(CustomersPageSize), value); }
        public int PromotionsPageSize { get => ReadInt(nameof(PromotionsPageSize), DefaultPageSize); set => WriteInt(nameof(PromotionsPageSize), value); }
        public int ReportsPageSize { get => ReadInt(nameof(ReportsPageSize), DefaultPageSize); set => WriteInt(nameof(ReportsPageSize), value); }

        public string? LastVisitedPage
        {
            get => ReadString(nameof(LastVisitedPage), null);
            set => WriteString(nameof(LastVisitedPage), value);
        }

        // Expose the registry path for debugging
        public static string GetRegistryPath() => CompanyKeyPath;

        private static int ReadInt(string name, int fallback)
        {
            try
            {
                Debug.WriteLine($"[AppSettings] ReadInt: opening registry key at '{CompanyKeyPath}', name='{name}'");
                using var key = Registry.CurrentUser.CreateSubKey(CompanyKeyPath);
                if (key == null) return fallback;

                var raw = key.GetValue(name);
                Debug.WriteLine($"[AppSettings] ReadInt: raw value for '{name}' = '{raw}'");
                return raw switch
                {
                    int i => i,
                    string s when int.TryParse(s, out var i) => i,
                    _ => fallback
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppSettings] ReadInt: exception reading '{name}' from '{CompanyKeyPath}': {ex}");
                return fallback;
            }
        }

        private static void WriteInt(string name, int value)
        {
            try
            {
                Debug.WriteLine($"[AppSettings] WriteInt: writing '{value}' to '{CompanyKeyPath}\\{name}'");
                using var key = Registry.CurrentUser.CreateSubKey(CompanyKeyPath);
                key?.SetValue(name, value, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppSettings] WriteInt: exception writing '{name}' to '{CompanyKeyPath}': {ex}");
                // ignore
            }
        }

        private static string? ReadString(string name, string? fallback)
        {
            try
            {
                Debug.WriteLine($"[AppSettings] ReadString: opening registry key at '{CompanyKeyPath}', name='{name}'");
                using var key = Registry.CurrentUser.CreateSubKey(CompanyKeyPath);
                if (key == null) return fallback;

                var val = key.GetValue(name) as string ?? fallback;
                Debug.WriteLine($"[AppSettings] ReadString: value for '{name}' = '{val}'");
                return val;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppSettings] ReadString: exception reading '{name}' from '{CompanyKeyPath}': {ex}");
                return fallback;
            }
        }

        private static void WriteString(string name, string? value)
        {
            try
            {
                Debug.WriteLine($"[AppSettings] WriteString: writing to '{CompanyKeyPath}\\{name}' value='{value}'");
                using var key = Registry.CurrentUser.CreateSubKey(CompanyKeyPath);
                if (value == null)
                {
                    key?.DeleteValue(name, throwOnMissingValue: false);
                    Debug.WriteLine($"[AppSettings] WriteString: deleted '{name}' from '{CompanyKeyPath}'");
                    return;
                }

                key?.SetValue(name, value, RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppSettings] WriteString: exception writing '{name}' to '{CompanyKeyPath}': {ex}");
                // ignore
            }
        }
    }
}
