using BonfireDB.Entities.Base;

namespace Services.Tests;

internal static class UnitOfWorkTestHelpers
{
    // Фабрика-заглушка, чей Create() всегда возвращает переданный UoW.
    public static IUnitOfWorkFactory ToFactory(this IUnitOfWork uow)
    {
        var factory = Substitute.For<IUnitOfWorkFactory>();
        factory.Create().Returns(uow);
        return factory;
    }
}
