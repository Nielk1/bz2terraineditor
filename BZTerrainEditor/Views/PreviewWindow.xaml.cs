using System.Windows;
using System.Windows.Media.Imaging;
   
namespace BZTerrainEditor.Views;
   
public partial class PreviewWindow : Window
{
    public PreviewWindow(BitmapSource image)
    {
        InitializeComponent();
        DataContext = new { Image = image };
    }
}