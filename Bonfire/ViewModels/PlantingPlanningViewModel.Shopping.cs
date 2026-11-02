using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Infrastructure.Commands;
using Bonfire.Services.Interfaces;

namespace Bonfire.ViewModels;

public sealed partial class PlantingPlanningViewModel
{
    private readonly IReportService _reports;
    public ICommand ExportShoppingListCommand => field ??= new LambdaCommandAsync(ExportShoppingListAsync,
        () => !IsBusy && OpenedPlan is { Batches.Length: > 0 });

    public async Task ExportShoppingListAsync()
    {
        var plan = OpenedPlan;
        if (IsBusy || plan is null) return;
        IsBusy = true;
        try
        {
            (await _service.ReadShoppingListAsync(plan.Id)).Match(shopping =>
            {
                if (shopping.Items.Length == 0) { Status = "По сохранённым партиям плана нет дефицита семян для покупки"; return 0; }
                return _reports.CreateSeedShoppingReport(shopping).Match(path =>
                {
                    Status = $"Список покупки для плана «{shopping.PlanName}» выгружен: {path}";
                    _dialog.Information(Status, "Покупка семян"); return 0;
                }, Error);
            }, Error);
        }
        finally { IsBusy = false; CommandManager.InvalidateRequerySuggested(); }
    }
}
