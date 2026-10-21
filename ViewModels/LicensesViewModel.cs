using HappyColorSplit.Models;
using HappyColorSplit.Services;

namespace HappyColorSplit.ViewModels;

public sealed class LicensesViewModel : ObservableObject, IDisposable
{
    private readonly ILicenseDocumentService _documents;
    private LicenseDocument? _selected;
    private string _documentText = "";
    private bool _loading;
    private bool _disposed;
    private int _request;
    private CancellationTokenSource? _cancellation;

    public LicensesViewModel(Texts texts, ILicenseDocumentService documents)
    {
        Text = texts; _documents = documents; Documents = documents.GetDocuments(texts);
        SelectedDocument = Documents.FirstOrDefault();
    }

    public Texts Text { get; }
    public string Title => $"HappyColorSplit · {Text["Licenses"]}";
    public IReadOnlyList<LicenseDocument> Documents { get; }
    public string DocumentText { get => _documentText; private set => Set(ref _documentText, value); }
    public bool IsLoading { get => _loading; private set => Set(ref _loading, value); }
    public LicenseDocument? SelectedDocument
    {
        get => _selected;
        set { if (!_disposed && Set(ref _selected, value)) _ = LoadAsync(value); }
    }

    private async Task LoadAsync(LicenseDocument? document)
    {
        int request = ++_request;
        _cancellation?.Cancel();
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        IsLoading = true; DocumentText = Text["LicenseLoading"];
        try
        {
            string text = document is null ? "" : await _documents.ReadAsync(document, cancellation.Token);
            if (request == _request) DocumentText = text;
        }
        catch (OperationCanceledException) { }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            if (request == _request) DocumentText = string.Format(Text["LicenseReadError"], document?.Title);
        }
        finally
        {
            if (request == _request) { _cancellation = null; IsLoading = false; }
        }
    }

    public void Dispose() { _disposed = true; _request++; _cancellation?.Cancel(); _cancellation = null; }
}
