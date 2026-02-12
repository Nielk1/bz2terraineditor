using System.Numerics;
using System.Windows;
using System.Windows.Media.Imaging;
using BZTerrainEditor.ViewModels.Nodes;

namespace BZTerrainEditor.Views;

public partial class PreviewWindow : Window
{
    public PreviewWindow(IPreviewNode node)
    {
        InitializeComponent();
        DataContext = node; // Bind to the interface for PreviewImage updates
    }
}