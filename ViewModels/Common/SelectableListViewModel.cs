using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Services.AppSettings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    // Generic base for selection and bulk actions
    public abstract partial class SelectableListViewModel<TItem> : PagedListViewModel<TItem>
    where TItem : class, INotifyPropertyChanged
    {
        // Public collection of currently selected items
        public ObservableCollection<TItem> SelectedItems { get; } = new();

        private readonly Dictionary<TItem, PropertyChangedEventHandler> _handlers = new();

        [ObservableProperty]
        private bool isBulkDeleteConfirmOpen;

        public int SelectedItemsCount => SelectedItems.Count;
        public bool HasSelectedItems => SelectedItemsCount > 0;

        protected SelectableListViewModel(IAppSettingsService? appSettings = null, Func<IAppSettingsService, int>? pageSizeSelector = null)
        : base(appSettings, pageSizeSelector)
        {
        }

        // Attach selection tracker to the provided observable collection of items.
        // Derived classes should call this for their collection after it's created.
        protected void AttachSelectionTracker(ObservableCollection<TItem> items)
        {
            if (items == null) return;

            // subscribe existing
            foreach (var it in items)
                SubscribeItem(it);

            // collection changed
            items.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (TItem it in e.NewItems)
                        SubscribeItem(it);
                }

                if (e.OldItems != null)
                {
                    foreach (TItem it in e.OldItems)
                        UnsubscribeItem(it);
                }

                // Ensure SelectedItems only contains items still present
                var removed = SelectedItems.Where(si => !items.Contains(si)).ToList();
                foreach (var r in removed) SelectedItems.Remove(r);

                OnPropertyChanged(nameof(SelectedItemsCount));
                OnPropertyChanged(nameof(HasSelectedItems));
            };
        }

        private void SubscribeItem(TItem item)
        {
            if (item == null) return;
            if (_handlers.ContainsKey(item)) return;

            void Handler(object? sender, PropertyChangedEventArgs e)
            {
                // react only to IsSelected changes (or null == any change)
                if (!string.IsNullOrEmpty(e.PropertyName) && !string.Equals(e.PropertyName, "IsSelected", StringComparison.OrdinalIgnoreCase))
                    return;

                var prop = item.GetType().GetProperty("IsSelected", BindingFlags.Public | BindingFlags.Instance);
                if (prop == null || prop.PropertyType != typeof(bool)) return;

                var val = (bool?)prop.GetValue(item) ?? false;
                if (val)
                {
                    if (!SelectedItems.Contains(item))
                        SelectedItems.Add(item);
                }
                else
                {
                    if (SelectedItems.Contains(item))
                        SelectedItems.Remove(item);
                }

                OnPropertyChanged(nameof(SelectedItemsCount));
                OnPropertyChanged(nameof(HasSelectedItems));
            }

            item.PropertyChanged += Handler;
            _handlers[item] = Handler;

            // initialize
            var p = item.GetType().GetProperty("IsSelected", BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(bool))
            {
                var v = (bool?)p.GetValue(item) ?? false;
                if (v && !SelectedItems.Contains(item)) SelectedItems.Add(item);
            }
        }

        private void UnsubscribeItem(TItem item)
        {
            if (item == null) return;
            if (_handlers.TryGetValue(item, out var handler))
            {
                item.PropertyChanged -= handler;
                _handlers.Remove(item);
            }

            if (SelectedItems.Contains(item)) SelectedItems.Remove(item);
            OnPropertyChanged(nameof(SelectedItemsCount));
            OnPropertyChanged(nameof(HasSelectedItems));
        }

        [RelayCommand]
        private void OpenBulkDeleteConfirm()
        {
            if (!HasSelectedItems) return;
            IsBulkDeleteConfirmOpen = true;
        }

        [RelayCommand]
        private void CancelBulkDeleteConfirm()
        {
            IsBulkDeleteConfirmOpen = false;
        }

        [RelayCommand]
        private async Task ConfirmBulkDeleteAsync()
        {
            IsBulkDeleteConfirmOpen = false;
            if (IsBusy) return;

            IsBusy = true;
            ErrorMessage = string.Empty;
            var shouldReload = false;

            try
            {
                var items = SelectedItems.ToArray();
                if (items.Length == 0) return;

                var ok = await DeleteItemsAsync(items);
                shouldReload = ok;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                shouldReload = true;
            }
            finally
            {
                // clear selection and reset busy
                SelectedItems.Clear();
                IsBusy = false;
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(SelectedItemsCount));
                OnPropertyChanged(nameof(HasSelectedItems));
            }

            if (shouldReload)
            {
                await LoadPageAsync(CurrentPage);
            }
        }

        // Derived classes implement actual deletion logic for selected items and return true if reload needed
        protected abstract Task<bool> DeleteItemsAsync(TItem[] items);
    }
}
