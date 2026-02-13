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
using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
    public ValueNodeOutputViewModel<UInt4[,]?> TextureLayer0 { get; } = new() { Name = "Layer 0 Texture Index" };
    public ValueNodeOutputViewModel<UInt4[,]?> TextureLayer1 { get; } = new() { Name = "Layer 1 Texture Index" };
    public ValueNodeOutputViewModel<UInt4[,]?> TextureLayer2 { get; } = new() { Name = "Layer 2 Texture Index" };
    public ValueNodeOutputViewModel<UInt4[,]?> TextureLayer3 { get; } = new() { Name = "Layer 3 Texture Index" };
    public ValueNodeOutputViewModel<ColorMapRgb24> Color { get; } = new() { Name = "Color" };
    public ValueNodeOutputViewModel<AlphaMap8> AlphaLayer1 { get; } = new() { Name = "Layer 1 Alpha" };
    public ValueNodeOutputViewModel<AlphaMap8> AlphaLayer2 { get; } = new() { Name = "Layer 2 Alpha" };
    public ValueNodeOutputViewModel<AlphaMap8> AlphaLayer3 { get; } = new() { Name = "Layer 3 Alpha" };

    private readonly CompositeDisposable _disposables = new();
    private FileSystemWatcher? _watcher;
    private readonly Subject<Unit> _fileChanged = new();

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

        // Set up file watcher when file path changes
        filePathObservable.Subscribe(path =>
        {
            if (_watcher != null)
            {
                _watcher.Dispose();
                _watcher = null;
            }
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                string directory = Path.GetDirectoryName(path);
                string fileName = Path.GetFileName(path);
                _watcher = new FileSystemWatcher(directory, fileName);
                _watcher.Changed += (s, e) => _fileChanged.OnNext(Unit.Default);
                _watcher.EnableRaisingEvents = true;
            }
        }).DisposeWith(_disposables);

        // Combine file path changes with file change events
        var combinedObservable = filePathObservable.Merge(_fileChanged.Select(_ => FilePath.Value));

        var terObservable = combinedObservable
            .Where(value => value != null)
            .Select(value => Observable.Start(() =>
            {
                // All heavy work (file I/O, parsing, array creation) happens here
                try
                {
                    var lastWrite = File.GetLastWriteTime(value);
                    var ter = TerFileBase.Read(value); // This is the heavy part
                    return (path: value, lastWrite, ter);
                }
                catch
                {
                    return (path: value, lastWrite: DateTime.MinValue, ter: null as TerFileBase);
                }
            }, RxApp.TaskpoolScheduler))
            .Switch()
            .DistinctUntilChanged(tuple => (tuple.path, tuple.lastWrite))
            .ObserveOn(RxApp.MainThreadScheduler) // Only UI update after this
            .Replay(1)
            .RefCount();

        Height.Value = terObservable
            .Select(tuple => tuple.ter is BZ2TerFile bz2ter ? bz2ter.HeightMap : null);

        HeightFloat.Value = terObservable
            .Select(tuple => tuple.ter is BZCCTerFile bzccter ? bzccter.HeightMap : null);

        TextureLayer0.Value = terObservable.Select(tuple => tuple.ter?.TextureLayer0 is byte[,] arr ? ConvertToUInt4Array(arr) : null);
        TextureLayer1.Value = terObservable.Select(tuple => tuple.ter?.TextureLayer1 is byte[,] arr ? ConvertToUInt4Array(arr) : null);
        TextureLayer2.Value = terObservable.Select(tuple => tuple.ter?.TextureLayer2 is byte[,] arr ? ConvertToUInt4Array(arr) : null);
        TextureLayer3.Value = terObservable.Select(tuple => tuple.ter?.TextureLayer3 is byte[,] arr ? ConvertToUInt4Array(arr) : null);

        // Keep outputs visible but update names to indicate active/inactive status
        terObservable.Select(tuple => tuple.ter is BZ2TerFile)
            .Subscribe(isActive => Height.Name = isActive ? "Height (Int16)" : "[Inactive] Height (Int16)")
            .DisposeWith(_disposables);
        terObservable.Select(tuple => tuple.ter is BZCCTerFile)
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
    public static UInt4[,] ConvertToUInt4Array(byte[,] source)
    {
        if (source == null) return null;
        int height = source.GetLength(0);
        int width = source.GetLength(1);
        var result = new UInt4[height, width];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                result[y, x] = new UInt4(source[y, x]);
        return result;
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            _watcher?.Dispose();
            _disposables.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
