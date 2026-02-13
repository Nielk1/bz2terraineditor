using System;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BZTerrainEditor.Views;
using DynamicData;
using NodeNetwork;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using ReactiveUI;
using Splat;

namespace BZTerrainEditor.ViewModels.Nodes;

public static class PreviewNodeResistration
{
    [NodeRegistration(IsTypeBased = true)]
    public static void RegisterNode(GlobalNodeManager manager, Type t)
    {
        // Check if T is an array of INumber<T> elements
        Type elementType = t;
        bool inArray = false;
        while (elementType.IsArray)
        {
            elementType = elementType.GetElementType()!;
            inArray = true;
        }
        if (inArray && typeof(INumber<>).MakeGenericType(elementType).IsAssignableFrom(elementType))
        {
            Type nodeType = typeof(PreviewNode<>).MakeGenericType(elementType);
            manager.Register(nodeType, $"Auto-Scaled Greyscale Preview {t.Name}", $"Preview greyscale image from {t.Name}.", () => (NodeViewModel)Activator.CreateInstance(nodeType));
        }
    }
}

public class PreviewNode<T> : NodeViewModel, IPreviewNode where T : INumber<T>
{
    public UIElement? LeadingContent { get; set; }
    public ValueNodeInputViewModel<T[,]> HeightMap { get; } = new() { Name = $"{typeof(T).Name}[,]" };

    private BitmapSource _previewImage;
    public BitmapSource PreviewImage
    {
        get => _previewImage;
        private set => this.RaiseAndSetIfChanged(ref _previewImage, value);
    }

    private Views.PreviewWindow? _previewWindow; // Added to track the open window for updates and reuse

    static PreviewNode()
    {
        Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<PreviewNode<T>>));
    }

    public PreviewNode()
    {
        Name = "Auto-Scaled Greyscale Preview";

        Inputs.Add(HeightMap);

        var heightMapObs = HeightMap.WhenAnyValue(vm => vm.Value);

        var minMaxObs = heightMapObs.Where(v => v != null).Select(FindMinMax);
        var imageObs = heightMapObs.CombineLatest(minMaxObs, (array, minMax) => array != null ? CreatePreviewImage(array, minMax.min, minMax.max) : null);

        imageObs.ObserveOn(RxApp.MainThreadScheduler).Subscribe(img => PreviewImage = img);

        var previewImage = new Image
        {
            Stretch = Stretch.Uniform,
            MaxWidth = 200,
            MaxHeight = 200,
            Cursor = Cursors.Hand
        };
        previewImage.SetBinding(Image.SourceProperty, new Binding("PreviewImage"));

        previewImage.PreviewMouseLeftButtonDown += (s, e) => OpenFullPreview();

        LeadingContent = previewImage;
    }

    private void OpenFullPreview()
    {
        if (PreviewImage == null) return;
        
        if (_previewWindow == null || !_previewWindow.IsVisible)
        {
            _previewWindow = new Views.PreviewWindow(this); // Pass the node for binding
            _previewWindow.Owner = Application.Current.MainWindow;
            _previewWindow.Show(); // Non-modal
        }
        else
        {
            _previewWindow.Activate(); // Bring to front if already open
        }
    }

    private static (T min, T max) FindMinMax(T[,] array)
    {
        if (array == null || array.Length == 0) return (default, default);
        T min = array[0, 0];
        T max = array[0, 0];
        foreach (T val in array)
        {
            if (val < min) min = val;
            if (val > max) max = val;
        }
        return (min, max);
    }

    private static BitmapSource CreatePreviewImage(T[,] array, T min, T max)
    {
        if (array == null || array.Length == 0) return null;
        int height = array.GetLength(0);
        int width = array.GetLength(1);
        var bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Gray8, null);
        var pixels = new byte[width * height];
        double dmin = Convert.ToDouble(min);
        double dmax = Convert.ToDouble(max);
        double drange = dmax - dmin;
        if (drange == 0) drange = 1; // Avoid division by zero
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double dval = Convert.ToDouble(array[y, x]);
                byte grey = (byte)Math.Clamp((dval - dmin) / drange * 255, 0, 255);
                pixels[y * width + x] = grey;
            }
        }
        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width, 0);
        return bitmap;
    }
}