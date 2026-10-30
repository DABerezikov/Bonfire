using System.Windows.Controls;
using Bonfire.Domain.Seedlings;
using Bonfire.Views.Windows;

namespace ViewModels.Tests;

public class DeleteSeedlingChoiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Choice_ExplicitCommand_ReturnsSelectedIntentAndConfirmsOnce(bool correction)
    {
        var model = new DeleteSeedlingDialogViewModel("Черри");
        Assert.True(model.Choice.Match(_ => false, () => true));
        var confirmations = 0;
        model.Confirmed += () => confirmations++;
        (correction ? model.CorrectSowingCommand : model.DiscardCommand).Execute(null);
        Assert.Equal(correction, model.Choice.Match(choice => choice is SeedlingRemoval.CorrectSowing, () => throw new Exception("Выбор отсутствует")));
        Assert.Equal(1, confirmations);
    }
}

[Collection("WPF runtime")]
public class DeleteSeedlingDialogTests(WpfRuntime runtime)
{
    [Fact]
    public void Dialog_Xaml_BindsExplicitChoicesAndAllowsCancellation() => runtime.Invoke(() =>
    {
        var model = new DeleteSeedlingDialogViewModel("Бамбина Вайт китайская белая");
        var window = new DeleteSeedlingDialog { DataContext = model };
        var content = Assert.IsType<StackPanel>(window.Content);
        WpfRuntime.Layout(content, 504, 300);
        Assert.Contains(WpfRuntime.Descendants<TextBlock>(content), text => text.Text.Contains("«Бамбина Вайт китайская белая»"));
        var buttons = WpfRuntime.Descendants<Button>(content).ToArray();
        Assert.Equal(3, buttons.Length);
        Assert.Same(model.CorrectSowingCommand, buttons[0].Command);
        Assert.Same(model.DiscardCommand, buttons[1].Command);
        Assert.True(buttons[2].IsCancel);
        Assert.All(buttons, button => Assert.False(button.IsDefault));
        window.Close();
        Assert.True(model.Choice.Match(_ => false, () => true));
    });
}
