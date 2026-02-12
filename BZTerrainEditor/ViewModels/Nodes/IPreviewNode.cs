using System.Windows.Media.Imaging;

namespace BZTerrainEditor.ViewModels.Nodes;

public interface IPreviewNode
{
    BitmapSource PreviewImage { get; }
}