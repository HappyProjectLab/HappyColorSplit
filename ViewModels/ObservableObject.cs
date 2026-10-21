using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HappyColorSplit.ViewModels;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value; Changed(name); return true;
    }
}

public sealed class AsyncCommand(Func<Task> action, Func<bool> canExecute) : ICommand
{
    private bool _running;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !_running && canExecute();
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        _running = true; Refresh();
        try { await action(); }
        finally { _running = false; Refresh(); }
    }
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
