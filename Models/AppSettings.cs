namespace HappyColorSplit.Models;

public sealed class AppSettings
{
    public string Language { get; set; } = "de";
    public bool TutorialCompleted { get; set; }
    public bool TutorialTipsDisabled { get; set; }
    public double Tolerance { get; set; } = 40;
    public int MinimumAlpha { get; set; } = 128;
    public string LastExportFolder { get; set; } = "";
    public ExportArea ExportArea { get; set; } = ExportArea.White;
    public ExportBackground ExportBackground { get; set; } = ExportBackground.Black;
    public bool SmoothContours { get; set; }
    public int DefaultExportDpi { get; set; } = 300;
    public int ScaleUnit { get; set; }
    public LaserSettings Laser { get; set; } = new();
    public WorkspacePreferences Workspace { get; set; } = new();
}
