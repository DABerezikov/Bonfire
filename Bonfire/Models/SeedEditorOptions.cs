using System.Collections.Generic;
using BonfireDB.Entities;

namespace Bonfire.Models;

public sealed record SeedEditorOptions(
    IReadOnlyList<string> Colors,
    IReadOnlyList<string> Maturities,
    IReadOnlyList<Producer> Producers);
