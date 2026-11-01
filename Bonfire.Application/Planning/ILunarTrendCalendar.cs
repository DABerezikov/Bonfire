using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;

namespace Bonfire.Application.Planning;

public interface ILunarTrendCalendar
{
    /// <summary>Фаза в местный полдень региона; ошибка не является третьей фазой.</summary>
    Result<LunarTrend> GetTrend(DateOnly date, TimeZoneInfo timeZone);
}
