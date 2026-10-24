using System.Windows;
using System.Windows.Controls;
using Bonfire.Views.Windows;

namespace ViewModels.Tests;

[Collection("WPF runtime")]
public class DeleteSeedDialogTests(WpfRuntime runtime)
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("Истёк срок", "Истёк срок")]
    public void Dialog_InitialDataAndEditing_PreserveDeletionContract(string? previous, string expected) => runtime.Invoke(() =>
    {
        var model = new DeleteSeedDialogViewModel("Бычье сердце", previous);
        var window = new DeleteSeedDialog { DataContext = model };
        var content = Assert.IsType<Grid>(window.Content);
        WpfRuntime.Layout(content, 424, 260);

        var prompt = WpfRuntime.Descendants<TextBlock>(content).Single(block => block.Text.StartsWith("Удалить запись"));
        Assert.Contains("«Бычье сердце»", prompt.Text);
        Assert.Contains("Несохранённые изменения будут сброшены", prompt.Text);
        var input = Assert.IsType<TextBox>(window.FindName("ReasonInput"));
        Assert.Equal(expected, input.Text);
        Assert.True(input.AcceptsReturn);
        input.Text = "Новая причина\nВторая строка";
        Assert.Equal(input.Text, model.Reason);
        var buttons = WpfRuntime.Descendants<Button>(content).ToArray();
        Assert.Equal(new[] { "Да, удалить", "Нет" }, buttons.Select(button => button.Content));
        Assert.True(buttons[1].IsCancel);
        Assert.False(buttons[0].IsDefault);
        Assert.Equal(WindowStartupLocation.CenterOwner, window.WindowStartupLocation);
        window.Close();
    });
}
