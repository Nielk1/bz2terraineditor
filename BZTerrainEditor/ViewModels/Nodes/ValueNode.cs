using BZTerrainEditor.Records;
using BZTerrainEditor.ViewModels.Editors;
using DynamicData;
using NodeNetwork;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using ReactiveUI;
using Splat;
using System.Reactive.Linq;

namespace BZTerrainEditor.ViewModels.Nodes;

public class ValueNode<T, E> : NodeViewModel where E : ValueEditorViewModel<T>
{
    public ValueNodeInputViewModel<T> ValueIn { get; } = new() { Name = "Value" };

    public ValueNodeOutputViewModel<T> ValueOut { get; } = new() { Name = "Value" };

    static ValueNode()
    {
        Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<ValueNode<T, E>>));
    }

    public ValueNode(E editor)
    {
        Name = "Value Node";

        ValueIn.Editor = editor;
        ValueOut.Value = Observable.Return<T>(default);

        Inputs.Add(ValueIn);
        Outputs.Add(ValueOut);
    }
}
