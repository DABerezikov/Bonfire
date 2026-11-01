using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Libraries;

public sealed class ProducerDefinition
{
    private ProducerDefinition(ProducerId id, LibraryName name) => (Id, Name) = (id, name);
    public ProducerId Id { get; }
    public LibraryName Name { get; }

    public static Result<ProducerDefinition> Create(int id, string name)
        => ProducerId.Create(id).Bind(identity => LibraryName.Create(name)
            .Bind(validName => Result.Success(new ProducerDefinition(identity, validName))));

    public Result<ProducerDefinition> WithName(string name) => Create(Id.Value, name);
}