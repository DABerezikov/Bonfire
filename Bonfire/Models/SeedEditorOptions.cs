using System.Collections.Generic;
using Bonfire.Application.Libraries;

namespace Bonfire.Models;

public sealed record SeedEditorOptions(
    IReadOnlyList<string> Colors,
    IReadOnlyList<string> Maturities,
    IReadOnlyList<ProducerDetailsFromViewModel> Producers);
