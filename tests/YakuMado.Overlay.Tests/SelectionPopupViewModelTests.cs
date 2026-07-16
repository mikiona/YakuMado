using System.ComponentModel;
using YakuMado.Overlay;

namespace YakuMado.Overlay.Tests;

public class SelectionPopupViewModelTests
{
    [Fact]
    public void Show_sets_TranslatedText_and_IsVisible()
    {
        var vm = new SelectionPopupViewModel();

        vm.Show("こんにちは");

        Assert.Equal("こんにちは", vm.TranslatedText);
        Assert.True(vm.IsVisible);
    }

    [Fact]
    public void Hide_sets_IsVisible_to_false_but_keeps_last_text()
    {
        var vm = new SelectionPopupViewModel();
        vm.Show("こんにちは");

        vm.Hide();

        Assert.False(vm.IsVisible);
        Assert.Equal("こんにちは", vm.TranslatedText);
    }

    [Fact]
    public void Show_raises_PropertyChanged_for_TranslatedText_and_IsVisible()
    {
        var vm = new SelectionPopupViewModel();
        var raisedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => raisedProperties.Add(e.PropertyName!);

        vm.Show("こんにちは");

        Assert.Contains(nameof(SelectionPopupViewModel.TranslatedText), raisedProperties);
        Assert.Contains(nameof(SelectionPopupViewModel.IsVisible), raisedProperties);
    }

    [Fact]
    public void Show_does_not_raise_PropertyChanged_when_value_unchanged()
    {
        var vm = new SelectionPopupViewModel();
        vm.Show("こんにちは");
        var raisedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => raisedProperties.Add(e.PropertyName!);

        vm.Show("こんにちは"); // 同じ値・同じ表示状態

        Assert.DoesNotContain(nameof(SelectionPopupViewModel.TranslatedText), raisedProperties);
        Assert.DoesNotContain(nameof(SelectionPopupViewModel.IsVisible), raisedProperties);
    }
}
