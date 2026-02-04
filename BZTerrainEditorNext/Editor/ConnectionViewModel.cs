using CommunityToolkit.Mvvm.ComponentModel;

namespace BZTerrainEditorNext.Editor;

public partial class ConnectionViewModel : ObservableObject
{
    [ObservableProperty]
    private NodeViewModel source;

    [ObservableProperty]
    private NodeViewModel target;

    public ConnectionViewModel(NodeViewModel source, NodeViewModel target)
    {
        Source = source;
        Target = target;
    }
}