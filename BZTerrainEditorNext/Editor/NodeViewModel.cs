using CommunityToolkit.Mvvm.ComponentModel;

namespace BZTerrainEditorNext.Editor;

public partial class NodeViewModel : ObservableObject
{
    [ObservableProperty]
    private string title;

    public NodeViewModel(string title)
    {
        Title = title;
    }
}