using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork;
using DynamicData;
using BZTerrainEditor.Records;
using Splat;
using NodeNetwork.Views;
using ReactiveUI;

namespace BZTerrainEditor.Nodes
{
    public class Bz2TerImportViewModel : NodeViewModel
    {

        public ValueNodeOutputViewModel<HeightmapF32> Height { get; } = new() { Name = "Height" };
        //public ValueNodeOutputViewModel<FlagsMap<TerFlags>> NodeFlags { get; } = new() { Name = "Node Flags" };
        public ValueNodeOutputViewModel<IndexMap4Bit> TextureLayer0 { get; } = new() { Name = "Layer 0 Texture Index" };
        public ValueNodeOutputViewModel<IndexMap4Bit> TextureLayer1 { get; } = new() { Name = "Layer 1 Texture Index" };
        public ValueNodeOutputViewModel<IndexMap4Bit> TextureLayer2 { get; } = new() { Name = "Layer 2 Texture Index" };
        public ValueNodeOutputViewModel<IndexMap4Bit> TextureLayer3 { get; } = new() { Name = "Layer 3 Texture Index" };
        public ValueNodeOutputViewModel<ColorMapRgb24> Color { get; } = new() { Name = "Color" };
        public ValueNodeOutputViewModel<AlphaMap8> AlphaLayer1 { get; } = new() { Name = "Layer 1 Alpha" };
        public ValueNodeOutputViewModel<AlphaMap8> AlphaLayer2 { get; } = new() { Name = "Layer 2 Alpha" };
        public ValueNodeOutputViewModel<AlphaMap8> AlphaLayer3 { get; } = new() { Name = "Layer 3 Alpha" };

        static Bz2TerImportViewModel()
        {
            Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<Bz2TerImportViewModel>));
        }

        public Bz2TerImportViewModel()
        {
            Name = "Battlezone Combat Commander TER";
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
}
