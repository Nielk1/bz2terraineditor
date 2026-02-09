using NodeNetwork.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BZTerrainEditor.ViewModels;

public class NodeCreateCommand : ICommand
{
    private readonly Action<Func<NodeViewModel>> _execute;
    private readonly Predicate<Func<NodeViewModel>> _canExecute;

    public NodeCreateCommand(Action<Func<NodeViewModel>> execute, Predicate<Func<NodeViewModel>> canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter as Func<NodeViewModel>);
    public void Execute(object parameter) => _execute((Func<NodeViewModel>)parameter);
    public event EventHandler CanExecuteChanged;
}

public record struct GlobalNodeType(string Name, string Description, Func<NodeViewModel> Factory);

public class GlobalNodeManager : INotifyPropertyChanged
{
    private static GlobalNodeManager? _instance;
    public static GlobalNodeManager Instance => _instance ??= new GlobalNodeManager();

    //public ObservableCollection<GlobalNodeType> GlobalNodeTypes { get; } = new();
    public ObservableDictionary<Type, GlobalNodeType> GlobalNodeTypes { get; } = new();

    public void Register(Type type, string name, string description, Func<NodeViewModel> factory)
    {
        var globalNodeType = new GlobalNodeType(name, description, factory);
        if (!GlobalNodeTypes.ContainsKey(type))
            GlobalNodeTypes.Add(type, globalNodeType);
        OnPropertyChanged(nameof(GlobalNodeTypes));
    }

    public void Unregister(Type type)
    {
        if (GlobalNodeTypes.ContainsKey(type))
            GlobalNodeTypes.Remove(type);
        OnPropertyChanged(nameof(GlobalNodeTypes));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
