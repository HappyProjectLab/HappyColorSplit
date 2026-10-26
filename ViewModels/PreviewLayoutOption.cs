using HappyColorSplit.Services;

namespace HappyColorSplit.ViewModels;

public sealed class PreviewLayoutOption(string textKey) : ObservableObject
{
    private string _label = "";
    public string Label { get => _label; private set => Set(ref _label, value); }
    public void ChangeLanguage(Texts texts) => Label = texts[textKey];
}
