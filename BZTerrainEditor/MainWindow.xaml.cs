using BZTerrainEditor.ViewModels;
using BZTerrainEditor.ViewModels.Editors;
using BZTerrainEditor.ViewModels.Nodes;
using DynamicData;
using MahApps.Metro.Controls;
using NodeNetwork.ViewModels;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace BZTerrainEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this; // until we get a view model and reorganize stuff

            //Create a new viewmodel for the NetworkView
            var network = new NetworkViewModel();

            var nodeTerFile = new FileVariableNode();
            network.Nodes.Add(nodeTerFile);
            nodeTerFile.Position = new Point(5, 5);

            var nodeBZTer = new BattlezoneTerNode();
            network.Nodes.Add(nodeBZTer);
            nodeBZTer.Position = new Point(200, 5);

            NodeCreateCommand = new NodeCreateCommand(OnNodeCreateCommand);


            ////Create the node for the first node, set its name and add it to the network.
            //var node1 = new NodeViewModel();
            //node1.Name = "Node 1";
            //network.Nodes.Add(node1);
            //
            ////Create the viewmodel for the input on the first node, set its name and add it to the node.
            //var node1Input = new NodeInputViewModel();
            //node1Input.Name = "Node 1 input";
            //node1.Inputs.Add(node1Input);
            //
            ////Create the second node viewmodel, set its name, add it to the network and add an output in a similar fashion.
            //var node2 = new NodeViewModel();
            //node2.Name = "Node 2";
            //network.Nodes.Add(node2);
            //
            //var node2Output = new NodeOutputViewModel();
            //node2Output.Name = "Node 2 output";
            //node2.Outputs.Add(node2Output);
            //
            //var node3 = new NodeViewModel();
            //node3.Name = "Node 3";
            //network.Nodes.Add(node3);
            //
            //var node3InputA = new NodeInputViewModel();
            //node3InputA.Name = "Node 3 input A";
            //node3.Inputs.Add(node3InputA);
            //
            //var node3InputB = new NodeInputViewModel();
            //node3InputB.Name = "Node 3 input B";
            //node3.Inputs.Add(node3InputB);
            //
            //var node3OutputB = new NodeOutputViewModel();
            //node3OutputB.Name = "Node 3 output B";
            //node3.Outputs.Add(node3OutputB);
            //
            //nodeBZTer.Position = new Point(50, 50);
            //node1.Position = new Point(300, 50);
            //node2.Position = new Point(300, 200);




            //Assign the viewmodel to the view.
            networkView.ViewModel = network;

        }

        public ICommand NodeCreateCommand { get; }

        private void OnNodeCreateCommand(Func<NodeViewModel> factory)
        {
            var node = factory();
            var network = networkView.ViewModel as NetworkViewModel;

            // 1. Get mouse position relative to the networkView
            var mousePos = Mouse.GetPosition(networkView);

            // 2. Convert to network coordinates
            //    (mousePos - DragOffset) / Zoom
            var networkX = (0 - network.DragOffset.X) / network.ZoomFactor;
            var networkY = (0 - network.DragOffset.Y) / network.ZoomFactor;

            node.Position = new Point(networkX, networkY);
            network.Nodes.Add(node);
        }
    }
}