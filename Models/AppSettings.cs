namespace HappyColorSplit.Models;

public sealed class AppSettings
{
    public string Language { get; set; } = "de";
    public double Tolerance { get; set; } = 40;
    public int MinimumAlpha { get; set; } = 128;
    public string LastExportFolder { get; set; } = "";
    public ExportArea ExportArea { get; set; } = ExportArea.White;
    public ExportBackground ExportBackground { get; set; } = ExportBackground.Black;
}
