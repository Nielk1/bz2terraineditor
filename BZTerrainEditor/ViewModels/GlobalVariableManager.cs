using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BZTerrainEditor.ViewModels;

public class GlobalVariableManager : INotifyPropertyChanged
{
    private static GlobalVariableManager? _instance;
    public static GlobalVariableManager Instance => _instance ??= new GlobalVariableManager();

    public ObservableCollection<object> GlobalInputs { get; } = new();

    public void Register(object editor)
    {
        if (!GlobalInputs.Contains(editor))
            GlobalInputs.Add(editor);
        OnPropertyChanged(nameof(GlobalInputs));
    }

    public void Unregister(object editor)
    {
        if (GlobalInputs.Contains(editor))
            GlobalInputs.Remove(editor);
        OnPropertyChanged(nameof(GlobalInputs));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}