using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Libraries;

public sealed class ProducerDefinition
{
    public ProducerId Id { get; }
    public LibraryName Name { get; }
    private ProducerDefinition(ProducerId id, LibraryName name) => (Id, Name) = (id, name);

    public static Result<ProducerDefinition> Create(int id, string name)
        => ProducerId.Create(id).Bind(identity => LibraryName.Create(name)
            .Bind(validName => Result.Success(new ProducerDefinition(identity, validName))));

    public Result<ProducerDefinition> WithName(string name) => Create(Id.Value, name);
}
