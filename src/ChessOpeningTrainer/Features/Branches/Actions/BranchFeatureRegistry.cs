using System.Reflection;

namespace ChessOpeningTrainer;
internal sealed record BranchFeatureContext(Form Owner, OpeningDefinition Definition, StockfishEngine Engine, BoardControl Board, Label Info, CancellationTokenSource LifetimeCancellation, Func<BranchVertex?> Selection, Action Rebuild, Func<string, int, Button> AddButton, HashSet<string> Collapsed, HashSet<string> Compact, CheckBox HideSubbranches, Func<OpeningDefinition>? OpeningProvider = null, Func<CancellationTokenSource>? CancellationProvider = null)
{
    public OpeningDefinition Opening => OpeningProvider?.Invoke() ?? Definition;
    public CancellationTokenSource Cancellation => CancellationProvider?.Invoke() ?? LifetimeCancellation;
}
internal interface IBranchFeature
{
    void Install(BranchFeatureContext context);
}

[AttributeUsage(AttributeTargets.Class)]
internal sealed class BranchFeatureAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}

internal static class BranchFeatureRegistry
{
    private static readonly Type[] Features = typeof(BranchFeatureRegistry).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(IBranchFeature).IsAssignableFrom(t) && t.GetCustomAttribute<BranchFeatureAttribute>()is not null).OrderBy(t => t.GetCustomAttribute<BranchFeatureAttribute>()!.Order).ThenBy(t => t.FullName).ToArray();
    public static void Install(BranchFeatureContext context)
    {
        foreach (var type in Features)
            ((IBranchFeature)Activator.CreateInstance(type)!).Install(context);
    }
}
