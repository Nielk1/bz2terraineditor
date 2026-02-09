using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BZTerrainEditor.ViewModels;

public record struct GlobalNodeType(string Name, Type NodeType);

public class GlobalNodeManager : INotifyPropertyChanged
{
    private static GlobalNodeManager? _instance;
    public static GlobalNodeManager Instance => _instance ??= new GlobalNodeManager();

    public ObservableCollection<GlobalNodeType> GlobalNodeTypes { get; } = new();
    
    public void Register(string name, Type node)
    {
        var globalNodeType = new GlobalNodeType(name, node);
        if (!GlobalNodeTypes.Contains(globalNodeType))
            GlobalNodeTypes.Add(globalNodeType);
        OnPropertyChanged(nameof(GlobalNodeTypes));
    }

    public void Unregister(string name, Type node)
    {
        var globalNodeType = new GlobalNodeType(name, node);
        if (GlobalNodeTypes.Contains(globalNodeType))
            GlobalNodeTypes.Remove(globalNodeType);
        OnPropertyChanged(nameof(GlobalNodeTypes));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
