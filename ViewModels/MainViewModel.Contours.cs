using HappyColorSplit.Models;

namespace HappyColorSplit.ViewModels;

public sealed partial class MainViewModel
{
    private bool CanTripleContourResolution
    {
        get
        {
            if (!IsIdle || SelectedImage is null || ScaleDpi > 800) return false;
            try { new ImageScale(ScalePercent * 3, (int)ScaleDpi * 3).GetSize(SelectedImage.SourceWidth, SelectedImage.SourceHeight); return true; }
            catch (ExportSizeLimitException) { return false; }
        }
    }
    private Task TripleContourResolutionAsync()
    {
        if (!CanTripleContourResolution) return Task.CompletedTask;
        _settings.SmoothContours = true; Changed(nameof(SmoothContours));
        int dpi = (int)ScaleDpi * 3;
        if (ApplyScale(new(ScalePercent * 3, dpi)))
        {
            SaveSettings(); StatusMessage("ContourResolutionTripled");
        }
        return Task.CompletedTask;
    }
}
