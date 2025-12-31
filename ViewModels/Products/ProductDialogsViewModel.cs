using CommunityToolkit.Mvvm.ComponentModel;
using System;
using MyShopClient.ViewModels.Products.Dialogs;

namespace MyShopClient.ViewModels.Products
{
    public partial class ProductDialogsViewModel : ObservableObject
    {
        public ProductAddViewModel AddVm { get; }
        public ProductEditViewModel EditVm { get; }
        public ProductDeleteViewModel DeleteVm { get; }

        public ProductDialogsViewModel(ProductAddViewModel addVm, ProductEditViewModel editVm, ProductDeleteViewModel deleteVm)
        {
            AddVm = addVm ?? throw new ArgumentNullException(nameof(addVm));
            EditVm = editVm ?? throw new ArgumentNullException(nameof(editVm));
            DeleteVm = deleteVm ?? throw new ArgumentNullException(nameof(deleteVm));
        }
    }
}
