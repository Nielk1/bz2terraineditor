using BZTerrainEditor.ViewModels;
using BZTerrainEditor.Views;
using NodeNetwork;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using ReactiveUI;
using Splat;
using System.Configuration;
using System.Data;
using System.Windows;

namespace BZTerrainEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            NNViewRegistrar.RegisterSplat();

            //Locator.CurrentMutable.Register(() => new SideBySideNodeView(), typeof(IViewFor<NodeViewModel>));
            //Locator.CurrentMutable.Register(() => new SideBySideNodeView(), typeof(IViewFor<Bz2TerImportViewModel>));

            //Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<Bz2TerImportViewModel>));

            NodeRegistrationSystem.RegisterAll(GlobalNodeManager.Instance);

            base.OnStartup(e);
        }
    }

}
