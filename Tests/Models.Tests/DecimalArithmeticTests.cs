using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using System.Runtime.ExceptionServices;

namespace Models.Tests;

public class DecimalArithmeticTests
{
    [Fact]
    public void CheckedResults_MatchNativeDecimalRoundingAcrossSignsScalesAndLimits()
    {
        decimal[] values = [decimal.MinValue, decimal.MinValue + 1, -2, -1, -.1m, 0,
            .0000000000000000000000000001m, .1m, 1, 1.01m, 2, decimal.MaxValue / 1.01m, decimal.MaxValue / 3m, decimal.MaxValue - 1, decimal.MaxValue];
        var random = new Random(20261101);
        values = values.Concat(Enumerable.Range(0, 16).Select(_ => new decimal(random.Next(), random.Next(), random.Next(), random.Next(2) == 1, (byte)random.Next(29)))).ToArray();
        foreach (var left in values) foreach (var right in values)
        {
            Compare((a, b) => a + b, DecimalArithmetic.Add(left, right));
            Compare((a, b) => a - b, DecimalArithmetic.Subtract(left, right));
            Compare((a, b) => a * b, DecimalArithmetic.Multiply(left, right));
            Compare((a, b) => a / b, DecimalArithmetic.Divide(left, right));
            void Compare(Func<decimal, decimal, decimal> native, Result<decimal> actual)
            {
                decimal expected = 0;
                var error = Record.Exception(() => expected = native(left, right));
                if (error is OverflowException or DivideByZeroException) Assert.True(actual.IsFailure);
                else { Assert.Null(error); Assert.True(actual.IsSuccess, $"{left}; {right}: {actual.Match(_ => "", x => x)}"); Assert.Equal(expected, actual.ValueOrThrow()); }
            }
        }
    }
    [Fact]
    public void BusinessFailures_DoNotRaiseCaughtArithmeticExceptions()
    {
        var observed = new List<Exception>(); var thread = Environment.CurrentManagedThreadId;
        void Observe(object? sender, FirstChanceExceptionEventArgs args)
        {
            if (Environment.CurrentManagedThreadId == thread && args.Exception is OverflowException or DivideByZeroException) observed.Add(args.Exception);
        }
        AppDomain.CurrentDomain.FirstChanceException += Observe;
        try
        {
            Assert.True(BedCapacity.Calculate(BedHarvestTests.Layout() with { Length = decimal.MaxValue }).IsFailure);
            Assert.True(HarvestProjection.Calculate(new() { PerPlantKg = Option.Some(decimal.MaxValue) }, 2).IsFailure);
            Assert.True(HarvestProjection.Calculate(new() { Mode = HarvestPlanningMode.TargetPerM2, TargetKg = Option.Some(1m), PerM2Kg = Option.Some(.0000000000000000000000000001m),
                Layout = Option.Some(BedHarvestTests.Layout()), YieldAreaM2 = Option.Some(1.8m) }, 20).IsFailure);
            Assert.True(SeedQuantityCalculation.Automatic(PlantCount.Create(int.MaxValue).ValueOrThrow(),
                GerminationPercentage.Create(.00000000000000000000000001m).ValueOrThrow(), LossPercentage.Create(0).ValueOrThrow()).IsFailure);
            Assert.True(HarvestSeasonSummary.Calculate([(new() { PerPlantKg = Option.Some(decimal.MaxValue) }, 1),
                (new() { PerPlantKg = Option.Some(decimal.MaxValue) }, 1)]).IsFailure);
        }
        finally { AppDomain.CurrentDomain.FirstChanceException -= Observe; }
        Assert.Empty(observed);
    }
    [Fact]
    public void Boundaries_ReturnFailureWithoutExecutingOverflowingOperation()
    {
        Assert.True(DecimalArithmetic.Add(decimal.MaxValue, 1m).IsFailure);
        Assert.True(DecimalArithmetic.Subtract(decimal.MinValue, 1m).IsFailure);
        Assert.True(DecimalArithmetic.Multiply(decimal.MaxValue, 2m).IsFailure);
        Assert.True(DecimalArithmetic.Divide(decimal.MaxValue, .1m).IsFailure);
        Assert.True(DecimalArithmetic.Divide(1m, 0m).IsFailure);
        Assert.True(DecimalArithmetic.ToInt32((decimal)int.MaxValue + 1).IsFailure);
    }

    [Fact]
    public void RepresentableValues_KeepDecimalRoundingAndSignedBoundaries()
    {
        Assert.Equal(decimal.MaxValue, DecimalArithmetic.Multiply(decimal.MaxValue, 1m).ValueOrThrow());
        Assert.Equal(decimal.MinValue, DecimalArithmetic.Divide(decimal.MinValue, 1m).ValueOrThrow());
        Assert.Equal(decimal.MaxValue, DecimalArithmetic.Divide(decimal.MinValue, -1m).ValueOrThrow());
        Assert.Equal(decimal.MinValue, DecimalArithmetic.Add(decimal.MinValue, 0m).ValueOrThrow());
        Assert.Equal(decimal.MaxValue, DecimalArithmetic.Subtract(decimal.MaxValue, 0m).ValueOrThrow());
        Assert.Equal(1m / 3m, DecimalArithmetic.Divide(1m, 3m).ValueOrThrow());
        Assert.Equal(.1m * .2m, DecimalArithmetic.Multiply(.1m, .2m).ValueOrThrow());
        Assert.Equal(0m, DecimalArithmetic.Multiply(.0000000000000000000000000001m, .1m).ValueOrThrow());
        Assert.True(DecimalArithmetic.Subtract(decimal.MaxValue, -1m).IsFailure);
        Assert.True(DecimalArithmetic.Add(decimal.MinValue, -1m).IsFailure);
    }
}
