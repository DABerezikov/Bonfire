using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Planning;

public enum LunarTrend
{
    Growing,
    Waning
}

public static class LunarSowingPolicy
{
    public const int Version = 1;

    public static Result<LunarTrend> PreferredTrend(LunarGroup group) => group switch
    {
        LunarGroup.AboveGround => Result.Success(LunarTrend.Growing),
        LunarGroup.RootCrop => Result.Success(LunarTrend.Waning),
        LunarGroup.Unspecified => Result.Failure<LunarTrend>("Не задана лунная группа культуры"),
        _ => Result.Failure<LunarTrend>("Неизвестная лунная группа культуры")
    };
}