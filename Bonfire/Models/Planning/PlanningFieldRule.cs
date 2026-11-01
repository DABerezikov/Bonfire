using Bonfire.Domain.Functional;
using System.Collections.Generic;
using System.Linq;

namespace Bonfire.Models.Planning;

/// <summary>Единый контракт подписи поля и результата его проверки.</summary>
public sealed record PlanningFieldRule(string Name, bool IsRequired, Result<bool> Validation)
{
    public string Caption => Name + (IsRequired ? " *" : "");
    public string Error => PlanningInputParser.Error(Validation);

    public static Result<bool> ValidateAll(IEnumerable<PlanningFieldRule> rules)
    {
        var errors = new List<string>();
        foreach (var rule in rules)
            rule.Validation.Match(_ => 0, error => { errors.Add(error); return 0; });
        return errors.Count == 0 ? Result.Success(true) : Result.Failure<bool>(string.Join("; ", errors.Distinct()));
    }

    public static PlanningFieldRule Checked<T>(string name, bool required, Result<T> validation)
        => new(name, required, validation.Bind(_ => Result.Success(true)));

    public static PlanningFieldRule Optional<T>(string name, bool required, Result<Option<T>> validation)
        => new(name, required, validation.Bind(value => required && value is Option<T>.None
            ? Result.Failure<bool>(name + ": заполните обязательное поле или задайте значение в справочнике")
            : Result.Success(true)));
}
