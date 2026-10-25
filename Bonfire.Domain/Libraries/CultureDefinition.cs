using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Libraries;

public sealed class CultureDefinition
{
    public CultureId Id { get; }
    public LibraryName Name { get; }
    public string Class { get; }

    private CultureDefinition(CultureId id, LibraryName name, string plantClass)
        => (Id, Name, Class) = (id, name, plantClass);

    public static Result<CultureDefinition> Create(int id, string name, string plantClass)
        => CultureId.Create(id).Bind(identity => LibraryName.Create(name).Bind(validName =>
            string.IsNullOrWhiteSpace(plantClass)
                ? Result.Failure<CultureDefinition>("Класс должен быть выбран")
                : Result.Success(new CultureDefinition(identity, validName, plantClass))));

    public Result<CultureDefinition> WithDetails(string name, string plantClass) => Create(Id.Value, name, plantClass);
}
