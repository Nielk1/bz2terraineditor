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
using System.Windows.Media.Media3D;

namespace BZTerrainEditor.ViewModels.Nodes;

public class BattlezoneTerNode : NodeViewModel
{
    [NodeRegistration]
    public static void RegisterNode(GlobalNodeManager manager)
    {
        manager.Register(typeof(BattlezoneTerNode), "BattlezoneTerNode", "TER import from BZ2 or BZCC", () => { return new BattlezoneTerNode(); });
    }

    public ValueNodeInputViewModel<string?> FilePath { get; } = new() { Name = "File Path" };

    public ValueNodeOutputViewModel<float[,]> Height { get; } = new() { Name = "Height" };
    //public ValueNodeOutputViewModel<FlagsMap<TerFlags>> NodeFlags { get; } = new() { Name = "Node Flags" };
    public ValueNodeOutputViewModel<IndexMap4Bit> TextureLayer0 { get; } = new() { Name = "Layer 0 Texture Index" };
    public ValueNodeOutputViewModel<IndexMap4Bit> TextureLayer1 { get; } = new() { Name = "Layer 1 Texture Index" };
    public ValueNodeOutputViewModel<IndexMap4Bit> TextureLayer2 { get; } = new() { Name = "Layer 2 Texture Index" };
    public ValueNodeOutputViewModel<IndexMap4Bit> TextureLayer3 { get; } = new() { Name = "Layer 3 Texture Index" };
    public ValueNodeOutputViewModel<ColorMapRgb24> Color { get; } = new() { Name = "Color" };
    public ValueNodeOutputViewModel<AlphaMap8> AlphaLayer1 { get; } = new() { Name = "Layer 1 Alpha" };
    public ValueNodeOutputViewModel<AlphaMap8> AlphaLayer2 { get; } = new() { Name = "Layer 2 Alpha" };
    public ValueNodeOutputViewModel<AlphaMap8> AlphaLayer3 { get; } = new() { Name = "Layer 3 Alpha" };

    static BattlezoneTerNode()
    {
        Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<BattlezoneTerNode>));
    }

    public BattlezoneTerNode()
    {
        Name = "Battlezone TER";

        FilePath.Editor = new FilePathEditorViewModel
        {
            //Filter = "All files (*.*)|*.*"
        };
        FilePath.Port.IsVisible = true;

        Height.Value = Observable.Return<float[,]>(default); // temporary

        Inputs.Add(FilePath);

        Outputs.Add(Height);
        //Outputs.Add(NodeFlags);
        Outputs.Add(TextureLayer0);
        Outputs.Add(TextureLayer1);
        Outputs.Add(TextureLayer2);
        Outputs.Add(TextureLayer3);
        Outputs.Add(Color);
        Outputs.Add(AlphaLayer1);
        Outputs.Add(AlphaLayer2);
        Outputs.Add(AlphaLayer3);
    }
}
