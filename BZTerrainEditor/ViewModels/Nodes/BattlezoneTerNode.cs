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
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Media.Media3D;

namespace BZTerrainEditor.ViewModels.Nodes;

public class BattlezoneTerNode : NodeViewModel, IDisposable
{
    [NodeRegistration]
    public static void RegisterNode(GlobalNodeManager manager)
    {
        manager.Register(typeof(BattlezoneTerNode), "Battlezone TER Import", "TER import from BZ2 or BZCC", () => { return new BattlezoneTerNode(); });
    }

    public ValueNodeInputViewModel<string?> FilePath { get; } = new() { Name = "File Path" };

    public ValueNodeOutputViewModel<Int16[,]?> Height { get; } = new() { Name = "Height (Int16)" };
    public ValueNodeOutputViewModel<float[,]?> HeightFloat { get; } = new() { Name = "Height (Single)" };
    //public ValueNodeOutputViewModel<FlagsMap<TerFlags>> NodeFlags { get; } = new() { Name = "Node Flags" };
    public ValueNodeOutputViewModel<IndexMap4Bit> TextureLayer0 { get; } = new() { Name = "Layer 0 Texture Index" };
    public ValueNodeOutputViewModel<IndexMap4Bit> TextureLayer1 { get; } = new() { Name = "Layer 1 Texture Index" };
    public ValueNodeOutputViewModel<IndexMap4Bit> TextureLayer2 { get; } = new() { Name = "Layer 2 Texture Index" };
    public ValueNodeOutputViewModel<IndexMap4Bit> TextureLayer3 { get; } = new() { Name = "Layer 3 Texture Index" };
    public ValueNodeOutputViewModel<ColorMapRgb24> Color { get; } = new() { Name = "Color" };
    public ValueNodeOutputViewModel<AlphaMap8> AlphaLayer1 { get; } = new() { Name = "Layer 1 Alpha" };
    public ValueNodeOutputViewModel<AlphaMap8> AlphaLayer2 { get; } = new() { Name = "Layer 2 Alpha" };
    public ValueNodeOutputViewModel<AlphaMap8> AlphaLayer3 { get; } = new() { Name = "Layer 3 Alpha" };


    private readonly CompositeDisposable _disposables = new();

    static BattlezoneTerNode()
    {
        Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<BattlezoneTerNode>));
    }

    public BattlezoneTerNode()
    {
        Name = "Battlezone TER Import";

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

        var filePathObservable = FilePath.WhenAnyValue(vm => vm.Value);

        var terObservable = filePathObservable
            .Where(value => value != null)
            .Select(value => TerFileBase.Read(value));

        Height.Value = terObservable
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(ter => ter is BZ2TerFile bz2ter ? bz2ter.HeightMap : null);

        HeightFloat.Value = terObservable
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(ter => ter is BZCCTerFile bzccter ? bzccter.HeightMap : null);

        // Keep outputs visible but update names to indicate active/inactive status
        terObservable.Select(ter => ter is BZ2TerFile)
            .Subscribe(isActive => Height.Name = isActive ? "Height (Int16)" : "[Inactive] Height (Int16)")
            .DisposeWith(_disposables);
        terObservable.Select(ter => ter is BZCCTerFile)
            .Subscribe(isActive => HeightFloat.Name = isActive ? "Height (Single)" : "[Inactive] Height (Single)")
            .DisposeWith(_disposables);

        // Reset names if no file is loaded
        filePathObservable
            .Select(value => string.IsNullOrWhiteSpace(value))
            .Subscribe(isEmpty =>
            {
                if (isEmpty)
                {
                    Height.Name = "Height (Int16)";
                    HeightFloat.Name = "Height (Single)";
                }
            }).DisposeWith(_disposables);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposables.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
