using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

public abstract record GardenLockTarget
{
    private GardenLockTarget()
    {
    }

    public sealed record Element : GardenLockTarget;

    public sealed record Greenhouse : GardenLockTarget;
}

public sealed record GardenLockChange
{
    private GardenLockChange(int id, GardenLockTarget target, bool isLocked)
        => (Id, Target, IsLocked) = (id, target, isLocked);

    public int Id { get; }
    public GardenLockTarget Target { get; }
    public bool IsLocked { get; }

    public static Result<GardenLockChange> Create(int id, GardenLockTarget target, bool isLocked)
    {
        ArgumentNullException.ThrowIfNull(target);
        return id > 0
            ? Result.Success(new GardenLockChange(id, target, isLocked))
            : Result.Failure<GardenLockChange>("Идентификатор должен быть положительным");
    }
}