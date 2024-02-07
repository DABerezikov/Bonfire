using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;

namespace Bonfire.ViewModels
{
    public class SeedlingsViewModel : ViewModel
    {
        private readonly ISeedlingsService _seedlings;
        private readonly IUserDialog _dialog;

        public SeedlingsViewModel(ISeedlingsService seedlings, IUserDialog dialog)
        {
            _seedlings = seedlings;
            _dialog = dialog;
        }
    }
}
