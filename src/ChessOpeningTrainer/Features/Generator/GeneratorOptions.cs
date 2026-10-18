namespace ChessOpeningTrainer;
public sealed class GeneratorOptions
{
    public List<DepthSection> Sections { get; set; } = [];

    public GeneratorOptions AtDepth(int depth)
    {
        var effective = (GeneratorOptions)MemberwiseClone();
        if (Sections.Count > 0)
        {
            var section = Sections.FirstOrDefault(s => depth < s.EndDepth) ?? Sections[^1];
            effective.ComplexThreshold = section.ComplexThreshold;
            effective.RetainBest = section.RetainBest;
            effective.RetainMinEval = section.RetainMinEval;
            effective.EasyThreshold = section.EasyThreshold;
            effective.SearchDepth = section.SearchDepth;
            effective.MaxReplyLoss = section.MaxReplyLoss;
            effective.CompressionLoss = section.CompressionLoss;
            effective.LearnerVariationLoss = section.LearnerVariationLoss;
            effective.TwoStages = false;
        }

        return effective;
    }

    public bool LearnWhite { get; set; }
    public int MinDepth { get; set; } = 5;
    public int MaxDepth { get; set; } = 10;
    public double EasyThreshold { get; set; } = -2;
    public int ComplexThreshold { get; set; } = 4;
    public int RetainBest { get; set; } = 0;
    public double RetainMinEval { get; set; } = 0;
    public int MaxPositions { get; set; } = 200;
    public int SearchDepth { get; set; } = 14;
    public double MaxReplyLoss { get; set; } = 2;
    public double CompressionLoss { get; set; } = .1;
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public double LearnerVariationLoss { get; set; }
    public int Threads { get; set; } = Math.Max(1, Math.Min(12, Environment.ProcessorCount - 2));
    public int Workers { get; set; } = 2;
    public bool MergeTranspositions { get; set; } = true;
    public bool HumanFilter { get; set; }
    public int HumanDepth { get; set; } = 8;
    public int MiddleDepth { get; set; } = 10;
    public bool TwoStages { get; set; }
    public int LateComplexThreshold { get; set; } = 3;
    public double LateReplyLoss { get; set; } = 1;

    public void Validate()
    {
        if (Sections.Count > 5)
            throw new ArgumentException("Höchstens fünf Tiefenabschnitte.");
        int previous = 0;
        foreach (var s in Sections)
        {
            if (!double.IsFinite(s.LearnerVariationLoss) || s.LearnerVariationLoss < 0 || s.LearnerVariationLoss > 20)
                throw new ArgumentException("Eigener Variationsverlust muss zwischen 0 und 20 Bauern liegen.");
            if (s.RetainBest < 0 || s.RetainBest > 50 || s.RetainMinEval < -20 || s.RetainMinEval > 20 || s.EndDepth <= previous || s.EndDepth > 60 || s.ComplexThreshold < 2 || s.EasyThreshold > 0 || s.SearchDepth < 5 || s.SearchDepth > 24 || s.MaxReplyLoss < 0 || s.CompressionLoss < 0 || s.CompressionLoss > 5)
                throw new ArgumentException("Abschnittsgrenzen müssen aufsteigend sein; Wertebereich prüfen.");
            previous = s.EndDepth;
        }

        if (Sections.Count > 0)
            MaxDepth = Sections[^1].EndDepth;
        if (!double.IsFinite(LearnerVariationLoss) || LearnerVariationLoss < 0 || LearnerVariationLoss > 20)
            throw new ArgumentException("Eigener Variationsverlust muss zwischen 0 und 20 Bauern liegen.");
        if (RetainBest < 0 || RetainBest > 50 || RetainMinEval < -20 || RetainMinEval > 20 || MinDepth < 0 || MaxDepth < 1 || MinDepth > MaxDepth || MaxDepth > 60 || ComplexThreshold < 2 || MaxPositions < 1 || SearchDepth < 5 || SearchDepth > 24 || EasyThreshold > 0 || MaxReplyLoss < 0 || CompressionLoss < 0 || CompressionLoss > 5 || Threads < 1 || Threads > 128 || Workers < 1 || Workers > Threads || HumanDepth < 1 || TwoStages && (MiddleDepth < MinDepth || MiddleDepth > MaxDepth) || LateComplexThreshold < 2 || LateReplyLoss < 0)
            throw new ArgumentException("Ungültige Generatorgrenzen. Arbeiter dürfen das Threadbudget nicht überschreiten; mittlere Tiefe muss zwischen Min und Max liegen.");
    }
}
