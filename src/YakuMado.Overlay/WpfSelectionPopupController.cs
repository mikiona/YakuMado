using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YakuMado.Core.Overlay;

namespace YakuMado.Overlay;

/// <summary>選択テキスト翻訳結果を表示するポップアップウィンドウ(SelectionPopupViewModelにバインド)。</summary>
public sealed class WpfSelectionPopupController : ISelectionPopupController
{
    private readonly SelectionPopupViewModel _viewModel;
    private Window? _popupWindow;
    private TextBlock? _textBlock;

    public WpfSelectionPopupController(SelectionPopupViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.PropertyChanged += (_, _) => UpdateWindow();
    }

    public void ShowPopup(string translatedText, ScreenPoint anchor)
    {
        EnsureWindowCreated();

        _popupWindow!.Left = anchor.X;
        _popupWindow.Top = anchor.Y;
        _viewModel.Show(translatedText);
    }

    private void EnsureWindowCreated()
    {
        if (_popupWindow != null) return;

        _textBlock = new TextBlock
        {
            Foreground = Brushes.White,
            FontSize = 14,
            Padding = new Thickness(8),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 400,
        };

        _popupWindow = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = new SolidColorBrush(Color.FromArgb(230, 30, 30, 30)),
            Topmost = true,
            ShowInTaskbar = false,
            SizeToContent = SizeToContent.WidthAndHeight,
            Content = _textBlock,
        };
    }

    private void UpdateWindow()
    {
        if (_popupWindow == null || _textBlock == null) return;

        _textBlock.Text = _viewModel.TranslatedText;
        _popupWindow.Visibility = _viewModel.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        if (_viewModel.IsVisible)
        {
            _popupWindow.Show();
        }
    }
}
