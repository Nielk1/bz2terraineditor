using BZTerrainEditorNext.Editor;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Reflection;

namespace BZTerrainEditorNext.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public ObservableCollection<NodeViewModel> Nodes { get; } = new();
        public ObservableCollection<ConnectionViewModel> Connections { get; } = new();
        public ObservableCollection<object> SelectedItems { get; } = new();

        public string VersionString =>
#if DEBUG
            $"{Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? "VERSION READ ERROR"} - DEV";
#else
            $"{Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? "VERSION READ ERROR"}";
#endif

        public MainWindowViewModel()
        {
            // Example nodes
            var node1 = new NodeViewModel("Node 1");
            var node2 = new NodeViewModel("Node 2");
            Nodes.Add(node1);
            Nodes.Add(node2);

            // Example connection
            Connections.Add(new ConnectionViewModel(node1, node2));
        }
    }
}