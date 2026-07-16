using System.Windows;
using System.Windows.Controls;
using YakuMado.Core.Translation;
using ListBox = System.Windows.Controls.ListBox;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Orientation = System.Windows.Controls.Orientation;
using MessageBox = System.Windows.MessageBox;

namespace YakuMado.App;

/// <summary>設定画面(F6: エンジン優先順位/有効無効、F7: APIキー、F9: 対応言語表示)のView。</summary>
public sealed class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly ITranslationSettingsRepository _repository;
    private readonly ListBox _engineListBox;
    private readonly TextBox _azureApiKeyBox;
    private readonly TextBox _azureRegionBox;

    public SettingsWindow(SettingsViewModel viewModel, ITranslationSettingsRepository repository)
    {
        _viewModel = viewModel;
        _repository = repository;

        Title = "YakuMado 設定";
        Width = 480;
        Height = 420;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var rootPanel = new StackPanel { Margin = new Thickness(12) };

        rootPanel.Children.Add(new TextBlock
        {
            Text = "翻訳エンジンの優先順位(上ほど優先)・有効/無効・対応言語ペア",
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8),
        });

        _engineListBox = new ListBox { Height = 160, ItemsSource = _viewModel.Engines };
        _engineListBox.DisplayMemberPath = null;
        _engineListBox.ItemTemplateSelector = null;
        // シンプルにテキスト表現で表示する(詳細なXAMLテンプレートは今後のUI改善で対応)
        _engineListBox.ItemTemplate = BuildEngineRowTemplate();
        rootPanel.Children.Add(_engineListBox);

        var reorderPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 12) };
        var upButton = new Button { Content = "上へ", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
        upButton.Click += (_, _) => MoveSelected(-1);
        var downButton = new Button { Content = "下へ", Width = 80 };
        downButton.Click += (_, _) => MoveSelected(1);
        reorderPanel.Children.Add(upButton);
        reorderPanel.Children.Add(downButton);
        rootPanel.Children.Add(reorderPanel);

        rootPanel.Children.Add(new TextBlock { Text = "Azure Translator APIキー", Margin = new Thickness(0, 8, 0, 2) });
        _azureApiKeyBox = new TextBox { Text = _viewModel.AzureApiKey };
        rootPanel.Children.Add(_azureApiKeyBox);

        rootPanel.Children.Add(new TextBlock { Text = "Azure Translatorリージョン", Margin = new Thickness(0, 8, 0, 2) });
        _azureRegionBox = new TextBox { Text = _viewModel.AzureRegion };
        rootPanel.Children.Add(_azureRegionBox);

        var saveButton = new Button { Content = "保存(反映には再起動が必要です)", Margin = new Thickness(0, 16, 0, 0) };
        saveButton.Click += async (_, _) => await SaveAsync();
        rootPanel.Children.Add(saveButton);

        Content = rootPanel;
    }

    private static DataTemplate BuildEngineRowTemplate()
    {
        var factory = new System.Windows.FrameworkElementFactory(typeof(StackPanel));
        factory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

        var checkBoxFactory = new System.Windows.FrameworkElementFactory(typeof(CheckBox));
        checkBoxFactory.SetBinding(CheckBox.IsCheckedProperty, new System.Windows.Data.Binding(nameof(TranslationEngineSettingViewModel.IsEnabled)) { Mode = System.Windows.Data.BindingMode.TwoWay });
        checkBoxFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));

        var textFactory = new System.Windows.FrameworkElementFactory(typeof(TextBlock));
        textFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(TranslationEngineSettingViewModel.EngineName)));

        factory.AppendChild(checkBoxFactory);
        factory.AppendChild(textFactory);

        return new DataTemplate { VisualTree = factory };
    }

    private void MoveSelected(int direction)
    {
        if (_engineListBox.SelectedItem is not TranslationEngineSettingViewModel selected) return;

        if (direction < 0) _viewModel.MoveEngineUp(selected.EngineName);
        else _viewModel.MoveEngineDown(selected.EngineName);

        _engineListBox.SelectedItem = selected;
    }

    private async Task SaveAsync()
    {
        _viewModel.AzureApiKey = _azureApiKeyBox.Text;
        _viewModel.AzureRegion = _azureRegionBox.Text;

        await _repository.SaveAsync(_viewModel.ToTranslationSettings(), CancellationToken.None);
        MessageBox.Show("設定を保存しました。優先順位・APIキーの変更を反映するにはアプリの再起動が必要です。", "YakuMado", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
