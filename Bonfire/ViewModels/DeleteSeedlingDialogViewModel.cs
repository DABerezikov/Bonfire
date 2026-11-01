using System;
using System.Windows.Input;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;
using Bonfire.Infrastructure.Commands;
using Bonfire.ViewModels.Base;

namespace Bonfire.ViewModels;

public sealed class DeleteSeedlingDialogViewModel(string sortName) : ViewModel
{
    public string Prompt { get; } = $"Удалить рассаду сорта «{sortName}»?";
    public Option<SeedlingRemoval> Choice { get; private set; } = Option.None<SeedlingRemoval>();

    public ICommand CorrectSowingCommand =>
        field ??= new LambdaCommand(() => Select(new SeedlingRemoval.CorrectSowing()));

    public ICommand DiscardCommand => field ??= new LambdaCommand(() => Select(new SeedlingRemoval.Discard()));
    public event Action? Confirmed;

    private void Select(SeedlingRemoval removal)
    {
        Choice = Option.Some(removal);
        Confirmed?.Invoke();
    }
}