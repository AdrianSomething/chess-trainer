namespace ChessOpeningTrainer;
public sealed class SettingsForm : LocalizedForm
{
    private static SettingsForm? instance;
    private readonly TabControl tabs = new ThemedTabControl { Dock = DockStyle.Fill };
    private readonly SettingsWheelGuard wheelGuard;
    public static void OpenVision() => OpenNamed("vision");
    public static void OpenPuzzles() => OpenNamed("puzzles");
    public static void OpenGameAnalysis() => OpenNamed("analysis");
    private static void OpenNamed(string name)
    {
        if (instance is null || instance.IsDisposed) instance = new SettingsForm();
        if (instance?.tabs.TabPages[name] is { } page) instance.tabs.SelectedTab = page;
        Reveal();
    }
    public static void Open(int selectedTab = 0)
    {
        if (instance is null || instance.IsDisposed)
            instance = new SettingsForm();
        instance.tabs.SelectedIndex = Math.Clamp(selectedTab, 0, instance.tabs.TabCount - 1);
        Reveal();
    }
    private static void Reveal()
    {
        if (instance is null) return;
        if (instance.WindowState == FormWindowState.Minimized)
            instance.WindowState = FormWindowState.Normal;
        instance.Show();
        instance.BringToFront();
        instance.Activate();
    }

    public SettingsForm()
    {
        wheelGuard = new(this);
        Application.AddMessageFilter(wheelGuard);
        Text = "Einstellungen";
        ClientSize = new(550, 550);
        BackColor = UiPalette.FromArgb(31, 35, 39);
        ForeColor = UiPalette.Gainsboro;
        Font = new Font("Segoe UI", 11);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new(560, 420);
        MaximizeBox = true;
        var general = new TabPage("Allgemein")
        {
            BackColor = BackColor
        };
        var analysis = new TabPage("Partie-Analyse")
        {
            Name = "analysis",
            BackColor = BackColor
        };
        tabs.TabPages.AddRange([general, analysis]);
        Controls.Add(tabs);
        analysis.Controls.Add(GameAnalysisSettingsEditor.Create());
        var evaluation = new TabPage("Stellungsbewertung") { BackColor = BackColor };
        evaluation.Controls.Add(EvaluationSettingsEditor.Create());
        tabs.TabPages.Add(evaluation);
        var supply = new TabPage("Rätselvorrat") { Name = "puzzles", BackColor = BackColor };
        supply.Controls.Add(PuzzleSupplySettingsEditor.Create());
        tabs.TabPages.Add(supply);
        var markings = new TabPage("Markierungen") { BackColor = BackColor };
        markings.Controls.Add(new MarkingSettingsEditor { Dock = DockStyle.Fill });
        tabs.TabPages.Add(markings);
        var vision = new TabPage("Schachblick") { Name = "vision", BackColor = BackColor };
        vision.Controls.Add(new VisionSettingsEditor { Dock = DockStyle.Fill });
        tabs.TabPages.Add(vision);
        var branches = new TabPage("Branches") { Name = "branches", BackColor = BackColor };
        branches.Controls.Add(new BranchSettingsEditor { Dock = DockStyle.Fill });
        tabs.TabPages.Add(branches);
        tabs.TabPages.Clear();
        tabs.TabPages.AddRange([general, evaluation, markings, analysis, branches, vision, supply]);
        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new(20),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };
        general.Controls.Add(layout);
        var tutorial = new Button { Text = "Überblick erneut starten", Width = 480, Height = 38 };
        tutorial.Click += (_, _) => { Close(); Application.OpenForms.OfType<MainForm>().FirstOrDefault(f => f.IsHomeWindow)?.OfferTutorial(); };
        layout.Controls.Add(tutorial);
        var debug = new CheckBox { Text = "Debug-Informationen anzeigen", AutoSize = true, Checked = Preferences.Current.DebugInformation };
        debug.CheckedChanged += (_, _) => { Preferences.Current.DebugInformation = debug.Checked; Preferences.Save(); };
        layout.Controls.Add(SettingsReset.Wrap(debug, false));
        var localAnalysis = new CheckBox { Text = "Lokales Spiel während der Partie analysieren", AutoSize = true, Checked = Preferences.Current.LocalContinuousAnalysis };
        localAnalysis.CheckedChanged += (_, _) => { Preferences.Current.LocalContinuousAnalysis = localAnalysis.Checked; Preferences.Save(); };
        layout.Controls.Add(SettingsReset.Wrap(localAnalysis, new UserPreferences().LocalContinuousAnalysis));
        var customIcons = new CheckBox { Text = "Eigene Piktogramme verwenden (nach erneutem Öffnen)", AutoSize = true, Checked = Preferences.Current.UseCustomIcons };
        customIcons.CheckedChanged += (_, _) => { Preferences.Current.UseCustomIcons = customIcons.Checked; Preferences.Save(); };
        layout.Controls.Add(SettingsReset.Wrap(customIcons, true));
        void Label(string text) => layout.Controls.Add(new Label { Text = text, AutoSize = true, MaximumSize = new(490, 0), Margin = new(0, 10, 0, 4) });
        Label("Sprache");
        var language = new ComboBox
        {
            Width = 490,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        language.Items.AddRange(L.Languages.Cast<object>().ToArray());
        language.SelectedItem = L.Languages.FirstOrDefault(l => l.Id == Preferences.Current.Language);
        layout.Controls.Add(SettingsReset.Wrap(language, L.Languages.First(l => l.Id == new UserPreferences().Language)));
        language.SelectedIndexChanged += (_, _) =>
        {
            Preferences.Current.Language = ((L.Language)language.SelectedItem!).Id;
            Preferences.Save();
        };
        Label("Brett und Figuren");
        var skin = new ComboBox
        {
            Width = 490,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        skin.Items.AddRange(Skins.All.Cast<object>().ToArray());
        skin.SelectedItem = Skins.Current;
        layout.Controls.Add(SettingsReset.Wrap(skin, Skins.All.First(s => s.Id == new UserPreferences().Skin)));
        skin.SelectedIndexChanged += (_, _) =>
        {
            Preferences.Current.Skin = ((SkinDefinition)skin.SelectedItem!).Id;
            Preferences.Save();
        };
        Label("Soundpaket");
        var sounds = new ComboBox
        {
            Width = 490,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        sounds.Items.AddRange(AppSounds.Packs.Cast<object>().ToArray());
        sounds.SelectedItem = AppSounds.Packs.FirstOrDefault(p => p.Id == Preferences.Current.SoundPack);
        layout.Controls.Add(SettingsReset.Wrap(sounds, AppSounds.Packs.FirstOrDefault(p => p.Id == new UserPreferences().SoundPack) ?? sounds.SelectedItem!));
        sounds.SelectedIndexChanged += (_, _) =>
        {
            Preferences.Current.SoundPack = ((AppSounds.SoundPack)sounds.SelectedItem!).Id;
            Preferences.Save();
        };
        Label("Lautstärke");
        var volume = new TrackBar
        {
            Width = 490,
            Minimum = 0,
            Maximum = 100,
            TickFrequency = 10,
            Value = Preferences.Current.Volume
        };
        var value = new Label
        {
            Text = volume.Value + " %",
            AutoSize = true
        };
        layout.Controls.AddRange([SettingsReset.Wrap(volume, new UserPreferences().Volume), value]);
        volume.ValueChanged += (_, _) =>
        {
            Preferences.Current.Volume = volume.Value;
            value.Text = volume.Value + " %";
            Preferences.Save();
        };
        var mute = new CheckBox
        {
            Text = "Stumm",
            Checked = AppSounds.Muted,
            AutoSize = true
        };
        layout.Controls.Add(SettingsReset.Wrap(mute, false));
        mute.CheckedChanged += (_, _) =>
        {
            if (mute.Checked != AppSounds.Muted)
                AppSounds.Toggle();
        };
        Label("UI-Farbschema");
        var theme = new ComboBox
        {
            Width = 490,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        theme.Items.AddRange(UiPalette.All.Cast<object>().ToArray());
        theme.SelectedItem = UiPalette.Current;
        layout.Controls.Add(SettingsReset.Wrap(theme, UiPalette.All.First(t => t.Id == new UserPreferences().UiTheme)));
        theme.SelectedIndexChanged += (_, _) =>
        {
            Preferences.Current.UiTheme = ((UiThemeDefinition)theme.SelectedItem!).Id;
            Preferences.Save();
        };
        Label("Animationsdauer (100 % = Original, 0 % = sofort)");
        var motion = new TrackBar
        {
            Width = 490,
            Minimum = 0,
            Maximum = 200,
            TickFrequency = 25,
            Value = Math.Clamp(Preferences.Current.AnimationPercent, 0, 200)
        };
        var motionValue = new Label
        {
            Text = motion.Value + " %",
            AutoSize = true
        };
        layout.Controls.AddRange([SettingsReset.Wrap(motion, new UserPreferences().AnimationPercent), motionValue]);
        motion.ValueChanged += (_, _) =>
        {
            Preferences.Current.AnimationPercent = motion.Value;
            motionValue.Text = motion.Value + " %";
            Preferences.Save();
        };
        Label("Eigene Pakete: Ordner Customization bearbeiten, dann App neu starten.");
        var audioStatus = new Label
        {
            AutoSize = true,
            MaximumSize = new(490, 0),
            ForeColor = UiPalette.Orange
        };
        layout.Controls.Add(audioStatus);
        var timer = new System.Windows.Forms.Timer
        {
            Interval = 400
        };
        timer.Tick += (_, _) => audioStatus.Text = AppSounds.LastError ?? UiPalette.LastError ?? "";
        timer.Start();
        FormClosed += (_, _) => timer.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) wheelGuard.Dispose();
        base.Dispose(disposing);
    }
}
