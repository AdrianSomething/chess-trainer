namespace ChessOpeningTrainer;

internal sealed record TutorialStep(Func<Form?> Window, Func<Form, Control?> Target, string Text,
    bool Interaction = false, Func<bool>? Completed = null, Action? Enter = null, Action? Leave = null,
    bool AllowInput = false, Action? SpaceAction = null, Action? Skip = null);
