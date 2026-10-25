using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using HappyColorSplit.Models;
using HappyColorSplit.Services;

namespace HappyColorSplit.ViewModels;

public sealed class LaserMaterialViewModel(LaserMaterial material)
{
    public LaserMaterial Material { get; } = material;
    public string Name => Material.Name;
    public string Hex => Material.Hex;
    public IBrush Swatch { get; } = new SolidColorBrush(Color.FromRgb(material.Color.R, material.Color.G, material.Color.B));
}

public sealed class LaserPageViewModel(LaserPage page, Bitmap thumbnail, Texts texts) : ObservableObject, IDisposable
{
    private Texts _texts = texts;
    public LaserPage Page { get; private set; } = page;
    public Bitmap Thumbnail { get; private set; } = thumbnail;
    public string Title => string.Format(_texts["LaserPageTitle"], Page.Material.Name, Page.MaterialPage);
    public string Info => string.Format(_texts["LaserPageInfo"], Page.Settings.WidthCm, Page.Settings.HeightCm, Page.Placements.Count);
    public string Groups => string.Join(Environment.NewLine, Page.Placements.GroupBy(p => p.Part.SourceIndex).OrderBy(g => g.Key)
        .Select(g => string.Format(_texts["LaserSourceInfo"], g.Key, g.First().Part.SourceName,
            string.Join(", ", g.Select(p => p.Part.Region).Distinct().Order()), g.Count())));
    public void SetTexts(Texts value) { _texts = value; Changed(nameof(Title)); Changed(nameof(Info)); Changed(nameof(Groups)); }
    public void Update(LaserPage value, Bitmap thumbnail)
    {
        var old = Thumbnail; Page = value; Thumbnail = thumbnail;
        Changed(nameof(Page)); Changed(nameof(Thumbnail)); Changed(nameof(Groups)); Changed(nameof(Info)); old.Dispose();
    }
    public void Dispose() => Thumbnail.Dispose();
}

public sealed partial class LaserWorkspaceViewModel : ObservableObject, IDisposable
{
    private readonly MainViewModel _owner;
    private readonly LaserSettings _settings;
    private LaserMaterialViewModel? _selectedMaterial;
    private LaserPageViewModel? _selectedPage;
    private string _materialName = "", _materialHex = "#000000";
    private Bitmap? _preview;
    private bool _current, _showGroups = true;
    public LaserWorkspaceViewModel(MainViewModel owner, LaserSettings settings)
    {
        _owner = owner; _settings = settings;
        foreach (var material in settings.Materials) Materials.Add(new(material));
        AddMaterialCommand = new(AddMaterialAsync, () => IsIdle && Materials.Count < 256);
        RemoveMaterialCommand = new(RemoveMaterialAsync, () => IsIdle && SelectedMaterial is not null);
        ArrangeCommand = new(() => owner.RunLaserAsync(ArrangeAsync), () => IsIdle && owner.HasBatchSelection && Materials.Count > 0);
        ExportPageCommand = new(() => owner.RunLaserAsync(token => ExportAsync(token, false, false)), () => CanExport && SelectedPage is not null);
        ExportAllCommand = new(() => owner.RunLaserAsync(token => ExportAsync(token, true, false)), () => CanExport);
        ExportMergedCommand = new(() => owner.RunLaserAsync(token => ExportAsync(token, true, true)), () => CanExport);
        UndoMoveCommand = new(() => owner.RunLaserAsync(UndoMoveAsync), () => CanEdit && _moves.Count > 0);
    }
    public Texts Text => _owner.Text;
    public bool IsIdle => _owner.IsIdle;
    public ObservableCollection<LaserMaterialViewModel> Materials { get; } = [];
    public ObservableCollection<LaserPageViewModel> Pages { get; } = [];
    public bool HasPages => Pages.Count > 0;
    public bool IsCurrent => _current;
    public bool IsStale => HasPages && !IsCurrent;
    private bool CanExport => IsIdle && IsCurrent && HasPages;
    public decimal WidthCm { get => _settings.WidthCm; set => ChangeSettings(() => _settings.WidthCm = value); }
    public decimal HeightCm { get => _settings.HeightCm; set => ChangeSettings(() => _settings.HeightCm = value); }
    public decimal MarginCm { get => _settings.MarginCm; set => ChangeSettings(() => _settings.MarginCm = value); }
    public decimal CavityGapCm { get => _settings.CavityGapCm; set => ChangeSettings(() => _settings.CavityGapCm = value); }
    public decimal ElementGapCm { get => _settings.ElementGapCm; set => ChangeSettings(() => _settings.ElementGapCm = value); }
    public decimal Dpi
    {
        get => _settings.Dpi;
        set
        {
            if (value == decimal.Truncate(value) && value is >= 1 and <= 2400) ChangeSettings(() => _settings.Dpi = (int)value);
            else { Changed(); _owner.SetLaserStatus("LaserInvalidSettings"); }
        }
    }
    public string MaterialName { get => _materialName; set => Set(ref _materialName, value); }
    public string MaterialHex { get => _materialHex; set => Set(ref _materialHex, value); }
    public LaserMaterialViewModel? SelectedMaterial
    { get => _selectedMaterial; set { if (Set(ref _selectedMaterial, value)) RemoveMaterialCommand.Refresh(); } }
    public LaserPageViewModel? SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (!IsIdle || !Set(ref _selectedPage, value)) return;
            Changed(nameof(PageInfo)); RefreshCommands();
            _ = _owner.RunLaserAsync(UpdatePreviewAsync);
        }
    }
    public string PageInfo => SelectedPage is null ? Text["LaserIntro"] : SelectedPage.Title + " · " + SelectedPage.Info;
    public Bitmap? Preview
    { get => _preview; private set { var old = _preview; Set(ref _preview, value); old?.Dispose(); } }
    public bool ShowGroups
    { get => _showGroups; set { if (Set(ref _showGroups, value)) RefreshAppearance(); } }
    public AsyncCommand AddMaterialCommand { get; }
    public AsyncCommand RemoveMaterialCommand { get; }
    public AsyncCommand ArrangeCommand { get; }
    public AsyncCommand ExportPageCommand { get; }
    public AsyncCommand ExportAllCommand { get; }
    public AsyncCommand ExportMergedCommand { get; }

    private void ChangeSettings(Action change)
    {
        if (!IsIdle) return;
        var old = (_settings.WidthCm, _settings.HeightCm, _settings.Dpi, _settings.MarginCm, _settings.CavityGapCm, _settings.ElementGapCm);
        try
        {
            change(); _settings.GetSize();
            if (old != (_settings.WidthCm, _settings.HeightCm, _settings.Dpi, _settings.MarginCm, _settings.CavityGapCm, _settings.ElementGapCm)) { Invalidate(); _owner.SaveLaserSettings(); }
        }
        catch (LaserSettingsException)
        { (_settings.WidthCm, _settings.HeightCm, _settings.Dpi, _settings.MarginCm, _settings.CavityGapCm, _settings.ElementGapCm) = old; _owner.SetLaserStatus("LaserInvalidSettings"); }
        Changed(nameof(WidthCm)); Changed(nameof(HeightCm)); Changed(nameof(Dpi));
        Changed(nameof(MarginCm)); Changed(nameof(CavityGapCm)); Changed(nameof(ElementGapCm));
    }
    private Task AddMaterialAsync()
    {
        if (!LaserMaterial.TryRgb(MaterialHex, out var color)) { _owner.SetLaserStatus("LaserInvalidColor"); return Task.CompletedTask; }
        string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        if (Materials.Any(m => m.Hex == hex)) { _owner.SetLaserStatus("LaserDuplicateColor"); return Task.CompletedTask; }
        string name = string.IsNullOrWhiteSpace(MaterialName) ? hex : MaterialName.Trim();
        if (name.Length > 80) name = name[..80];
        var material = new LaserMaterialViewModel(new(name, hex)); Materials.Add(material); SelectedMaterial = material;
        MaterialName = ""; SaveMaterials(); _owner.SetLaserStatus("LaserMaterialAdded", name); return Task.CompletedTask;
    }
    private Task RemoveMaterialAsync()
    {
        if (SelectedMaterial is not null) Materials.Remove(SelectedMaterial);
        SelectedMaterial = null; SaveMaterials(); return Task.CompletedTask;
    }
    private void SaveMaterials()
    { _settings.Materials = Materials.Select(m => m.Material).ToList(); Invalidate(); _owner.SaveLaserSettings(); }

    private async Task ArrangeAsync(CancellationToken token)
    {
        Invalidate();
        var inputs = _owner.GetLaserSources(); var settings = _settings.Snapshot();
        double tolerance = _owner.Tolerance; byte alpha = (byte)_owner.MinimumAlpha;
        var progress = new Progress<int>(p => { _owner.SetLaserProgress(p); _owner.SetLaserStatus(p >= 80 ? "LaserOptimizing" : "LaserArranging", p); });
        var layout = await Task.Run(() => new LaserLayoutService().Arrange(inputs, tolerance, alpha, settings, progress, token), token);
        var thumbnails = await Task.Run(() => layout.Pages.Select(page =>
            new LaserPageRenderer().Render(page, new(ExportArea.GroupColor, ExportBackground.Transparent), token, 190, 130)).ToArray(), token);
        token.ThrowIfCancellationRequested();
        var created = new List<LaserPageViewModel>();
        try
        {
            for (int i = 0; i < layout.Pages.Count; i++)
            { using var stream = new MemoryStream(thumbnails[i]); created.Add(new(layout.Pages[i], new Bitmap(stream), Text)); }
        }
        catch { foreach (var page in created) page.Dispose(); throw; }
        foreach (var page in Pages) page.Dispose(); Pages.Clear(); Preview = null;
        foreach (var page in created) Pages.Add(page);
        Set(ref _selectedPage, Pages.FirstOrDefault(), nameof(SelectedPage));
        Changed(nameof(HasPages)); Changed(nameof(PageInfo));
        await UpdatePreviewAsync(token);
        _current = true; Changed(nameof(IsCurrent)); Changed(nameof(IsStale)); RefreshCommands();
        _owner.SetLaserStatus("LaserArranged", layout.PartCount, Pages.Count);
    }
    private async Task UpdatePreviewAsync(CancellationToken token)
    {
        if (SelectedPage is null) { Preview = null; return; }
        var page = SelectedPage.Page; var options = _owner.LaserAppearance; bool groups = ShowGroups;
        var png = await Task.Run(() => new LaserPageRenderer().Render(page, options, token, 1200, 900, groups), token);
        token.ThrowIfCancellationRequested(); using var stream = new MemoryStream(png); Preview = new Bitmap(stream);
    }
    private async Task ExportAsync(CancellationToken token, bool all, bool merged)
    {
        string? folder = await _owner.ChooseLaserOutputAsync(); if (folder is null) return;
        var pages = all ? Pages.Select(p => p.Page).ToArray() : [SelectedPage!.Page];
        var options = _owner.LaserAppearance; var progress = new Progress<int>(_owner.SetLaserProgress);
        string result = await Task.Run(() => new LaserExportService().Export(pages, folder, options, progress, token, merged), token);
        _owner.SetLaserStatus("Exported", pages.Length, result); _owner.RememberLaserFolder(result);
    }
    public void Invalidate()
    { _current = false; _moves.Clear(); Changed(nameof(IsCurrent)); Changed(nameof(IsStale)); RefreshCommands(); }
    public void RefreshAppearance()
    { if (SelectedPage is not null && IsIdle && _owner.IsLaserMode) _ = _owner.RunLaserAsync(UpdatePreviewAsync); }
    public void RefreshCommands()
    {
        Changed(nameof(IsIdle));
        Changed(nameof(CanEdit)); UndoMoveCommand.Refresh();
        AddMaterialCommand.Refresh(); RemoveMaterialCommand.Refresh(); ArrangeCommand.Refresh();
        ExportPageCommand.Refresh(); ExportAllCommand.Refresh(); ExportMergedCommand.Refresh();
    }
    public void ChangeLanguage()
    { Changed(nameof(Text)); foreach (var page in Pages) page.SetTexts(Text); Changed(nameof(PageInfo)); }
    public void ClearLayout()
    {
        Preview = null; Set(ref _selectedPage, null, nameof(SelectedPage));
        foreach (var page in Pages) page.Dispose(); Pages.Clear(); _current = false; _moves.Clear();
        Changed(nameof(HasPages)); Changed(nameof(PageInfo)); Changed(nameof(IsCurrent)); Changed(nameof(IsStale)); RefreshCommands();
    }
    public void Dispose()
        => ClearLayout();
}
