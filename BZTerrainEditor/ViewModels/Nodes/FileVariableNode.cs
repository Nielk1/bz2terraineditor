using BZTerrainEditor.Records;
using ControlzEx.Standard;
using DynamicData;
using MahApps.Metro.Converters;
using NodeNetwork;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using ReactiveUI;
using Splat;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace BZTerrainEditor.ViewModels.Nodes;

public class FileVariableNode : NodeViewModel
{
    public ValueNodeOutputViewModel<string?> FilePath { get; } = new() { Name = "File Path" };

    public UIElement? LeadingContent { get; set; }

    static FileVariableNode()
    {
        Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<FileVariableNode>));
    }

    public FileVariableNode()
    {
        Name = "Open File";

        //FilePath.ReturnType = typeof(string?);
        //FilePath.Value = this.WhenAnyValue(vm => vm.Input.Value, vm => vm.OperationInput.Value)
        //    .Select(t => (t.Item1 == null || t.Item2 == null) ? null : BuildMathOperation(t.Item1, (MathOperation)t.Item2));
        FilePath.Value = Observable.Return<string?>(null); // or your initial value
        Outputs.Add(FilePath);




        // Create a WriteableBitmap (100x100 pixels)
        var bitmap = new WriteableBitmap(100, 100, 96, 96, PixelFormats.Bgra32, null);

        // Fill with solid red
        int stride = bitmap.PixelWidth * 4;
        byte[] pixels = new byte[bitmap.PixelHeight * stride];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i + 0] = 0;   // Blue
            pixels[i + 1] = 0;   // Green
            pixels[i + 2] = 255; // Red
            pixels[i + 3] = 255; // Alpha
        }
        bitmap.WritePixels(
            new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
            pixels, stride, 0);


        LeadingContent = new Image
        {
            //Source = new BitmapImage(new Uri("preview.png", UriKind.Relative)),
            Source = bitmap,
            Width = 100,
            Height = 100
        };

    }
}