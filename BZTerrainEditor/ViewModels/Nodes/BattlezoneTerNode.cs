using Bz2TerFile;
using BZTerrainEditor.Records;
using BZTerrainEditor.ViewModels.Editors;
using ControlzEx.Standard;
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

    public ValueNodeOutputViewModel<Int16[,]?> Height { get; } = new() { Name = "Height (Int16)" };
    public ValueNodeOutputViewModel<float[,]?> HeightFloat { get; } = new() { Name = "Height (Float)" };
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

        //Height.Value = Observable.Return<Int16[,]>(null); // temporary
        //HeightFloat.Value = Observable.Return<float[,]>(null); // temporary

        Inputs.Add(FilePath);

        Outputs.Add(Height);
        Outputs.Add(HeightFloat);
        //Outputs.Add(NodeFlags);
        Outputs.Add(TextureLayer0);
        Outputs.Add(TextureLayer1);
        Outputs.Add(TextureLayer2);
        Outputs.Add(TextureLayer3);
        Outputs.Add(Color);
        Outputs.Add(AlphaLayer1);
        Outputs.Add(AlphaLayer2);
        Outputs.Add(AlphaLayer3);

        var terObservable = FilePath
            .WhenAnyValue(vm => vm.Value)
            .Where(value => value != null)
            .Select(value => TerFileBase.Read(value));

        Height.Value = terObservable.Select(ter =>
        {
            if (ter is BZ2TerFile bz2ter)
                return bz2ter.HeightMap;
            return null;
        });
        HeightFloat.Value = terObservable.Select(ter =>
        {
            if (ter is BZCCTerFile bzccter)
                return bzccter.HeightMap;
            return null;
        });

        // Keep outputs visible but update names to indicate active/inactive status
        terObservable.Select(ter => ter is BZ2TerFile).Subscribe(isActive =>
        {
            Height.Name = isActive ? "Height (Int16)" : "[Inactive] Height (Int16)";
        });
        terObservable.Select(ter => ter is BZCCTerFile).Subscribe(isActive =>
        {
            HeightFloat.Name = isActive ? "Height (Float)" : "[Inactive] Height (Float)";
        });

        // Reset names if no file is loaded
        FilePath.WhenAnyValue(vm => vm.Value)
            .Select(value => string.IsNullOrEmpty(value))
            .Subscribe(isEmpty =>
            {
                if (isEmpty)
                {
                    Height.Name = "Height (Int16)";
                    HeightFloat.Name = "Height (Float)";
                }
            });

    }
}
