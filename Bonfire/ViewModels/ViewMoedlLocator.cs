using Microsoft.Extensions.DependencyInjection;

namespace Bonfire.ViewModels
{
    internal class ViewModelLocator
    {
        public MainWindowViewModel MainWindowModel => App.Services.GetRequiredService<MainWindowViewModel>();
        public SeedsViewModel SeedsModel => App.Services.GetRequiredService<SeedsViewModel>();
        public SeedlingsViewModel SeedlingsModel => App.Services.GetRequiredService<SeedlingsViewModel>();
        public LibraryEditorViewModel LibraryEditorModel => App.Services.GetRequiredService<LibraryEditorViewModel>();
    }
}
