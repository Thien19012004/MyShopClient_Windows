using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Promotion;
using MyShopClient.Services.Product;
using MyShopClient.Services.Category;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    public partial class PromotionListViewModel
    {
        private ViewModels.Promotions.PromotionAddViewModel? _dialogs_addVm;
        private ViewModels.Promotions.PromotionEditViewModel? _dialogs_editVm;

        public ViewModels.Promotions.PromotionAddViewModel Dialogs_AddVm => _dialogs_addVm ??= new ViewModels.Promotions.PromotionAddViewModel(_promotionService, async () => await LoadPageAsync(CurrentPage));
        public ViewModels.Promotions.PromotionEditViewModel Dialogs_EditVm => _dialogs_editVm ??= new ViewModels.Promotions.PromotionEditViewModel(_promotionService, async () => await LoadPageAsync(CurrentPage), _productService, _categoryService);

        [RelayCommand]
        private void OpenAddDialog_Command() => Dialogs_AddVm.DoOpen();

        [RelayCommand]
        private void CancelAddDialog_Command() => Dialogs_AddVm.DoCancel();

        [RelayCommand]
        private async Task ConfirmAddPromotion_Command() => await Dialogs_AddVm.DoConfirmAsync();

        [RelayCommand]
        private async Task OpenEditDialog_Command(PromotionItemDto? promotion)
        {
            if (promotion == null) return;
            var detailRes = await _promotionService.GetPromotionByIdAsync(promotion.PromotionId);
            if (!detailRes.Success || detailRes.Data == null)
            {
                ErrorMessage = detailRes.Message ?? "Cannot load promotion detail.";
                OnPropertyChanged(nameof(HasError));
                return;
            }

            await Dialogs_EditVm.DoOpenAsync(promotion, detailRes.Data);
        }

        [RelayCommand] private void CancelEditDialog_Command() => Dialogs_EditVm.DoCancel();
        [RelayCommand] private async Task ConfirmEditPromotion_Command() => await Dialogs_EditVm.DoConfirmAsync();
    }
}
