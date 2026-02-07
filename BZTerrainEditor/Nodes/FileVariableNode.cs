using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork;
using DynamicData;
using BZTerrainEditor.Records;
using Splat;
using NodeNetwork.Views;
using ReactiveUI;

namespace BZTerrainEditor.Nodes;

public class FileVariableNode : NodeViewModel
{
    public ValueNodeOutputViewModel<string> FilePath { get; } = new() { Name = "File Path" };

    static FileVariableNode()
    {
        Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<FileVariableNode>));
    }

    public FileVariableNode()
    {
        Name = "Open File";
        Outputs.Add(FilePath);

        
    }
}