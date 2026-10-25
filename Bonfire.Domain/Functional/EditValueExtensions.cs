namespace Bonfire.Domain.Functional;

public static class EditValueExtensions
{
    public static T WithChangeFrom<T>(this T current, T original, T edited)
        => EqualityComparer<T>.Default.Equals(original, edited) ? current : edited;
}
