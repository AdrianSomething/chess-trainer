using System.Reflection;

namespace ChessOpeningTrainer;
/// <summary>UI-only extension seam; the session owns rules, clocks and cancellation.</summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class PuzzleTabAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}

internal static class PuzzleTabRegistry
{
    private static readonly MethodInfo[] Builders = typeof(PuzzleSessionForm).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Where(m => m.GetCustomAttribute<PuzzleTabAttribute>()is not null).OrderBy(m => m.GetCustomAttribute<PuzzleTabAttribute>()!.Order).ThenBy(m => m.Name, StringComparer.Ordinal).ToArray();
    public static void Build(PuzzleSessionForm host, string dataDirectory)
    {
        foreach (var method in Builders)
        {
            if (method.ReturnType != typeof(void) || !method.GetParameters().Select(p => p.ParameterType).SequenceEqual([typeof(string)]))
                throw new InvalidOperationException($"Invalid puzzle tab builder: {method.Name}");
            method.Invoke(host, [dataDirectory]);
        }
    }
}
