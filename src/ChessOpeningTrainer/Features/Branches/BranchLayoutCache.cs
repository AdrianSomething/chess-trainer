using System.Security.Cryptography;
using System.Text;

namespace ChessOpeningTrainer;

/// <summary>Bounded memoization of pure geometry, not window/selection/progress
/// state. A structural key invalidates edits; no mutable vertices are shared.</summary>
internal static class BranchLayoutCache
{
    private static readonly object sync = new();
    private static readonly Dictionary<string, PointF[]> positions = [];
    private static readonly Queue<string> order = [];
    public static string Key(IReadOnlyList<BranchVertex> vertices, bool improved, bool fast, bool crossings)
    {
        var text = new StringBuilder().Append(improved).Append(fast).Append(crossings);
        foreach (var v in vertices)
        {
            text.Append('|').Append(v.Id).Append(':').Append(v.Height).Append(':').Append(v.IsLearnerMove);
            foreach (var c in v.Children) text.Append(',').Append(c.Id);
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }
    public static bool Apply(string key, IReadOnlyList<BranchVertex> vertices)
    {
        lock (sync)
        {
            if (!positions.TryGetValue(key, out var points) || points.Length != vertices.Count) return false;
            for (int i = 0; i < points.Length; i++) vertices[i].Location = points[i];
            return true;
        }
    }
    public static void Store(string key, IReadOnlyList<BranchVertex> vertices)
    {
        lock (sync)
        {
            if (positions.ContainsKey(key)) return;
            while (order.Count >= 12) positions.Remove(order.Dequeue());
            positions[key] = vertices.Select(v => v.Location).ToArray(); order.Enqueue(key);
        }
    }
}
