using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;

namespace MyShopClient.Controls
{
 public sealed partial class OnboardingOverlay : UserControl
 {
 public event EventHandler? NextRequested;
 public event EventHandler? BackRequested;
 public event EventHandler? SkipRequested;

 public OnboardingOverlay()
 {
 InitializeComponent();
 Root.Tapped += (_, __) => { /* swallow taps */ };
 SizeChanged += (_, __) => { /* reposition handled by caller */ };
 }

 public void Show(string title, string body, bool showBack, bool showSkip, string nextText)
 {
 TitleText.Text = title;
 BodyText.Text = body;
 BackButton.Visibility = showBack ? Visibility.Visible : Visibility.Collapsed;
 SkipButton.Visibility = showSkip ? Visibility.Visible : Visibility.Collapsed;
 NextButton.Content = nextText;
 Root.Visibility = Visibility.Visible;
 }

 public void Hide() => Root.Visibility = Visibility.Collapsed;

 public void PositionTo(FrameworkElement target)
 {
 if (target == null) return;
 if (Root.Visibility != Visibility.Visible) return;

 // Force measure so Card.ActualHeight is available
 Card.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
 Card.Arrange(new Windows.Foundation.Rect(0,0, Card.DesiredSize.Width, Card.DesiredSize.Height));

 // Translate target bounds to *this overlay's* coordinate space
 GeneralTransform t = target.TransformToVisual(Root);
 var topLeft = t.TransformPoint(new Windows.Foundation.Point(0,0));
 var rect = new Windows.Foundation.Rect(topLeft.X, topLeft.Y, target.ActualWidth, target.ActualHeight);

 // highlight
 const double highlightPad =6;
 Canvas.SetLeft(Highlight, rect.X - highlightPad);
 Canvas.SetTop(Highlight, rect.Y - highlightPad);
 Highlight.Width = Math.Max(0, rect.Width + highlightPad *2);
 Highlight.Height = Math.Max(0, rect.Height + highlightPad *2);

 // Tip card placement
 double pad =12;
 double cardX = rect.X + rect.Width + pad;
 double cardY = rect.Y;

 double cardWidth = Card.Width;
 double cardHeight = Math.Max(Card.ActualHeight, Card.DesiredSize.Height);

 // if not enough space on right => below
 double availableRight = Root.ActualWidth - cardX - pad;
 if (availableRight < cardWidth)
 {
 cardX = Math.Max(pad, rect.X);
 cardY = rect.Y + rect.Height + pad;
 }

 // clamp
 cardX = Math.Max(pad, Math.Min(cardX, Root.ActualWidth - cardWidth - pad));
 cardY = Math.Max(pad, Math.Min(cardY, Root.ActualHeight - cardHeight - pad));

 Card.Margin = new Thickness(cardX, cardY,0,0);
 }

 private void Next_Click(object sender, RoutedEventArgs e) => NextRequested?.Invoke(this, EventArgs.Empty);
 private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke(this, EventArgs.Empty);
 private void Skip_Click(object sender, RoutedEventArgs e) => SkipRequested?.Invoke(this, EventArgs.Empty);
 }
}
