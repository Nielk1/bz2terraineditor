using DynamicData;
using NodeNetwork;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using ReactiveUI;
using Splat;

namespace BZTerrainEditor.ViewModels.Nodes
{
    public class TestNodeViewModel : NodeViewModel
    {
        static TestNodeViewModel()
        {
            NNViewRegistrar.RegisterSplat();
            Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<TestNodeViewModel>));
        }

        public TestNodeViewModel()
        {
            Name = "Test Node";

            var node1Input = new NodeInputViewModel();
            node1Input.Name = "Node 1 input";
            Inputs.Add(node1Input);

            var node2Output = new NodeOutputViewModel();
            node2Output.Name = "Node 1 output";
            Outputs.Add(node2Output);
        }
    }
}