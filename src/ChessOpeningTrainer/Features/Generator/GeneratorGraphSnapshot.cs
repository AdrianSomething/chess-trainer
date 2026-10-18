namespace ChessOpeningTrainer;

public sealed record GeneratorGraphNode(string Id, int Depth, bool IsLearnerMove = false, bool Completed = false, bool IsTerminalCounter = false);
public sealed record GeneratorGraphEdge(string From, string To);
public sealed record GeneratorGraphSnapshot(GeneratorGraphNode[] Nodes, GeneratorGraphEdge[] Edges, int Cards, int Pending, int MaxDepth, bool Finished, bool Stopped = false);
public sealed record GeneratorSearchDepth(int Achieved, int Target, int TreeDepth, int MaxTreeDepth = 0);
