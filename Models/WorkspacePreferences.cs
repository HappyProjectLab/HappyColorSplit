namespace HappyColorSplit.Models;

public sealed class WorkspacePreferences
{
    public bool SideBySide { get; set; }
    public bool ImagesCollapsed { get; set; }
    public bool RightCollapsed { get; set; }
    public bool SynchronizePreviews { get; set; }
    public bool ReduceMotion { get; set; }
    public double HorizontalSplit { get; set; } = .5;
    public double VerticalSplit { get; set; } = .5;
}
