using System;
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

public class PreviewNode : NodeViewModel
{
    [NodeRegistration]
    public static void RegisterNode(GlobalNodeManager manager)
    {
        manager.Register(typeof(PreviewNode), "Terrain Preview", "Preview greyscale image from height map.", () => new PreviewNode());
    }

    public UIElement? LeadingContent { get; set; }
    public ValueNodeInputViewModel<float[,]> HeightMap { get; } = new() { Name = "Height Map" };

    private BitmapSource _previewImage;
    public BitmapSource PreviewImage
    {
        get => _previewImage;
        private set => this.RaiseAndSetIfChanged(ref _previewImage, value);
    }

    public ReactiveCommand<Unit, Unit> OpenFullPreviewCommand { get; }

    static PreviewNode()
    {
        Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<PreviewNode>));
    }

    public PreviewNode()
    {
        Name = "Terrain Preview";

        Inputs.Add(HeightMap);

        var heightMapObs = HeightMap.WhenAnyValue(vm => vm.Value).Where(v => v != null);
        var minMaxObs = heightMapObs.Select(FindMinMax);
        var imageObs = heightMapObs.CombineLatest(minMaxObs, (array, minMax) => CreatePreviewImage(array, minMax.min, minMax.max));

        imageObs.ObserveOn(RxApp.MainThreadScheduler).Subscribe(img => PreviewImage = img);

        OpenFullPreviewCommand = ReactiveCommand.Create(OpenFullPreview);

        // Set up the preview image in LeadingContent
        var previewImage = new Image
        {
            Stretch = Stretch.Uniform,
            MaxWidth = 200,
            MaxHeight = 200,
            Cursor = Cursors.Hand
        };
        previewImage.SetBinding(Image.SourceProperty, new Binding("PreviewImage"));
        previewImage.MouseLeftButtonDown += (s, e) => OpenFullPreview();
        LeadingContent = previewImage;
    }

    private void OpenFullPreview()
    {
        if (PreviewImage == null) return;
        var window = new Views.PreviewWindow(PreviewImage);
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
    }

    private static (float min, float max) FindMinMax(float[,] array)
    {
        if (array == null || array.Length == 0) return (0f, 0f);
        float min = float.MaxValue;
        float max = float.MinValue;
        foreach (float val in array)
        {
            if (val < min) min = val;
            if (val > max) max = val;
        }
        return (min, max);
    }

    private static BitmapSource CreatePreviewImage(float[,] array, float min, float max)
    {
        if (array == null || array.Length == 0) return null;
        int height = array.GetLength(0);
        int width = array.GetLength(1);
        var bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Gray8, null);
        var pixels = new byte[width * height];
        float range = max - min;
        if (range == 0) range = 1; // Avoid division by zero
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float val = array[y, x];
                byte grey = (byte)Math.Clamp((val - min) / range * 255, 0, 255);
                pixels[y * width + x] = grey;
            }
        }
        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width, 0);
        return bitmap;
    }
}