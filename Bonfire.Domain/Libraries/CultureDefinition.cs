using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Libraries;

public sealed class CultureDefinition
{
    public CultureId Id { get; }
    public string Name { get; }
    public string Class { get; }

    private CultureDefinition(CultureId id, string name, string plantClass)
        => (Id, Name, Class) = (id, name, plantClass);

    public static Result<CultureDefinition> Create(int id, string name, string plantClass)
        => CultureId.Create(id).Bind(identity => string.IsNullOrWhiteSpace(name)
            ? Result.Failure<CultureDefinition>("Название не может быть пустым")
            : string.IsNullOrWhiteSpace(plantClass)
                ? Result.Failure<CultureDefinition>("Класс должен быть выбран")
                : Result.Success(new CultureDefinition(identity, name, plantClass)));

    public Result<CultureDefinition> WithDetails(string name, string plantClass) => Create(Id.Value, name, plantClass);
}
