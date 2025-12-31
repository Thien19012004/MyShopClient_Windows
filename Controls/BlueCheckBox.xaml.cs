using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MyShopClient.Controls
{
    public sealed partial class BlueCheckBox : UserControl
    {
        public static readonly DependencyProperty IsCheckedProperty =
   DependencyProperty.Register(
     nameof(IsChecked),
       typeof(bool),
      typeof(BlueCheckBox),
     new PropertyMetadata(false, OnIsCheckedChanged));

        public BlueCheckBox()
        {
    this.InitializeComponent();
 }

        public bool IsChecked
        {
      get => (bool)GetValue(IsCheckedProperty);
          set => SetValue(IsCheckedProperty, value);
    }

      private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
if (d is BlueCheckBox checkBox)
     {
       bool isChecked = (bool)e.NewValue;
                checkBox.RootToggleButton.IsChecked = isChecked;
                checkBox.UpdateVisualState(isChecked);
     }
        }

public event RoutedEventHandler? Click;

        private void RootToggleButton_Checked(object sender, RoutedEventArgs e)
        {
    IsChecked = true;
        UpdateVisualState(true);
     Click?.Invoke(this, e);
        }

     private void RootToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
  IsChecked = false;
         UpdateVisualState(false);
    Click?.Invoke(this, e);
        }

        private void UpdateVisualState(bool isChecked)
        {
  if (isChecked)
        {
       // N?n xanh, d?u tick tr?ng
    CheckBoxBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 212)); // #0078D4
           CheckBoxBorder.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 212));
     CheckMark.Opacity = 1;
            }
  else
  {
      // N?n trong su?t, vi?n xám
         CheckBoxBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
          CheckBoxBorder.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 204, 204, 204));
      CheckMark.Opacity = 0;
         }
     }
    }
}
