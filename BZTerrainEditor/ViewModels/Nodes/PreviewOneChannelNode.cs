using BZTerrainEditor.Types;
using BZTerrainEditor.ViewModels.Editors;
using BZTerrainEditor.Views;
using ControlzEx.Standard;
using DynamicData;
using NodeNetwork;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using ReactiveUI;
using Splat;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BZTerrainEditor.ViewModels.Nodes;

public enum ERangeMode
{
    Extents,
    Type,
}

public enum EColorMode
{
    Greyscale,
    Hue,
}

public class PreviewOneChannelNode : NodeViewModel, IPreviewNode
{
    [NodeRegistration]
    public static void RegisterNode(GlobalNodeManager manager)
    {
        manager.Register(typeof(PreviewOneChannelNode), $"Single Channel Preview", $"Single Channel Preview image", () => new PreviewOneChannelNode());
    }

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
            InputTypes.Add(elementType);
        }
    }
    private static HashSet<Type> InputTypes = new HashSet<Type>();


    public UIElement? LeadingContent { get; set; }

    public Dictionary<Type, NodeInputViewModel> TypedInputs { get; } = new();
    //public ValueNodeInputViewModel<T[,]> HeightMap { get; } = new() { Name = $"{typeof(T).GetNiceTypeName()}[,]" };
    public ValueNodeInputViewModel<ERangeMode> RangeMode { get; private set; }
    public ValueNodeInputViewModel<EColorMode> ColorMode { get; private set; }

    private BitmapSource _previewImage;
    public BitmapSource PreviewImage
    {
        get => _previewImage;
        private set => this.RaiseAndSetIfChanged(ref _previewImage, value);
    }

    private Views.PreviewWindow? _previewWindow; // Added to track the open window for updates and reuse

    static PreviewOneChannelNode()
    {
        Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<PreviewOneChannelNode>));
    }

    public PreviewOneChannelNode()
    {
        Name = "Single Channel Preview";

        var inputTypesList = InputTypes.OrderBy(dr => dr.GetNiceTypeName()).ToList(); // Ensure consistent order
        foreach (var elementType in inputTypesList)
        {
            var arrayType = elementType.MakeArrayType(2); // Create T[,]
            var genericType = typeof(ValueNodeInputViewModel<>).MakeGenericType(arrayType);
            // Get the constructor with optional parameters
            var constructor = genericType.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 2);
            if (constructor == null) throw new InvalidOperationException("Expected constructor not found.");

            // Fetch the default values for the parameters
            var parameters = constructor.GetParameters();
            var defaultArgs = parameters.Select(p => p.DefaultValue).ToArray();

            // Create the instance with the default arguments
            var input = (NodeInputViewModel)Activator.CreateInstance(genericType, defaultArgs)!;

            genericType.GetProperty("Name")?.SetValue(input, $"{elementType.GetNiceTypeName()}[,]");
            genericType.GetProperty("Editor")?.SetValue(input, null);

            TypedInputs[elementType] = input; // Key by element type
            Inputs.Add(input);
        }

        RangeMode = new ValueNodeInputViewModel<ERangeMode>();
        RangeMode.Name = "Range Mode";
        RangeMode.Editor = new EnumEditorViewModel<ERangeMode>();
        Inputs.Add(RangeMode);

        ColorMode = new ValueNodeInputViewModel<EColorMode>();
        ColorMode.Name = "Color Mode";
        ColorMode.Editor = new EnumEditorViewModel<EColorMode>();
        Inputs.Add(ColorMode);

        // Create observables for each input's Value property
        var inputValueObservables = TypedInputs.Values
            .Select(input =>
            {
                var inputType = input.GetType();
                var valueProp = inputType.GetProperty("Value");
                var genericArg = inputType.GenericTypeArguments[0];

                // Get the WhenAnyValue extension method from ReactiveUI
                var reactiveExtensionsType = typeof(ReactiveUI.WhenAnyMixin);
                var whenAnyValueMethod = reactiveExtensionsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "WhenAnyValue" && m.IsGenericMethod && m.GetParameters().Length == 2);

                if (whenAnyValueMethod != null && valueProp != null)
                {
                    // Create lambda: vm => vm.Value
                    var param = System.Linq.Expressions.Expression.Parameter(inputType, "vm");
                    var body = System.Linq.Expressions.Expression.Property(param, valueProp);
                    var lambdaType = typeof(System.Linq.Expressions.Expression<>).MakeGenericType(
                        typeof(Func<,>).MakeGenericType(inputType, valueProp.PropertyType));
                    var lambda = System.Linq.Expressions.Expression.Lambda(lambdaType.GetGenericArguments()[0], body, param);

                    // Make generic method for TOwner, TValue
                    var genericMethod = whenAnyValueMethod.MakeGenericMethod(inputType, valueProp.PropertyType);

                    // Invoke WhenAnyValue<TOwner, TValue>(input, lambda)
                    return (IObservable<object?>)genericMethod.Invoke(null, new object[] { input, lambda });
                }
                else
                {
                    // fallback: just return Observable.Return(null)
                    return Observable.Return<object?>(null);
                }
            })
            .ToList();

        // Combine latest values and select the first non-null one
        var combinedInputsObs = Observable.CombineLatest(inputValueObservables)
            .Select(values =>
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i] != null)
                    {
                        return (TypedInputs.Values.ElementAt(i), values[i]);
                    }
                }
                return (null, null);
            });

        // Observe RangeMode.Value
        var rangeModeObs = RangeMode.WhenAnyValue(vm => vm.Value);

        // Observe ColorMode.Value
        var colorModeObs = ColorMode.WhenAnyValue(vm => vm.Value);

        // Combine with RangeMode and ColorMode
        var combinedWithRangeObs = Observable.CombineLatest<(NodeInputViewModel, object?), ERangeMode, EColorMode, (NodeInputViewModel, object?, ERangeMode, EColorMode)>(
            combinedInputsObs, rangeModeObs, colorModeObs,
            (inputValueTuple, rangeMode, colorMode) =>
            {
                var (input, value) = inputValueTuple;
                return (input, value, rangeMode, colorMode);
            });

        // Process the first non-null input
        var imageObs = combinedWithRangeObs.Select(tuple =>
        {
            var (input, value, rangeMode, colorMode) = tuple;
            if (input == null || value == null)
                return (BitmapSource?)null;

            var arrayType = input.GetType().GenericTypeArguments[0]; // T[,]
            var elementType = arrayType.GetElementType()!; // T (element type)
            // value is expected to be T[,]
            var array = value;
            // Use reflection to call FindMinMax and CreatePreviewImage
            var findMinMaxMethod = typeof(PreviewOneChannelNode).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "FindMinMax" && m.IsGenericMethod && m.GetParameters().Length == 2)
                ?.MakeGenericMethod(elementType);
            var createPreviewImageMethod = typeof(PreviewOneChannelNode).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "CreatePreviewImage" && m.IsGenericMethod && m.GetParameters().Length == 4)
                ?.MakeGenericMethod(elementType);

            var minMax = findMinMaxMethod?.Invoke(null, new object[] { array, rangeMode });
            if (minMax == null) return null;
            var min = minMax.GetType().GetField("Item1")?.GetValue(minMax);
            var max = minMax.GetType().GetField("Item2")?.GetValue(minMax);

            return (BitmapSource?)createPreviewImageMethod?.Invoke(null, new object[] { array, min, max, colorMode });
        });

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

    [DynamicDependency("FindMinMax`1", typeof(PreviewOneChannelNode))]
    private static (T min, T max) FindMinMax<T>(T[,] array, ERangeMode mode) where T: INumber<T>
    {
        if (mode == ERangeMode.Type)
        {
            T min = (T)typeof(T).GetField("MinValue").GetValue(null);
            T max = (T)typeof(T).GetField("MaxValue").GetValue(null);
            return (min, max);
        }
        else // ERangeMode.Extents
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
    }

    [DynamicDependency("CreatePreviewImage`1", typeof(PreviewOneChannelNode))]
    private static BitmapSource CreatePreviewImage<T>(T[,] array, T min, T max, EColorMode colorMode) where T : INumber<T>
    {
        if (array == null || array.Length == 0) return null;
        int height = array.GetLength(0);
        int width = array.GetLength(1);
        var bitmap = colorMode switch
        {
            EColorMode.Hue => new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Rgb24, null),
            EColorMode.Greyscale => new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Gray8, null),
            _ => new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Gray8, null),
        };
        var pixels = colorMode switch
        {
            EColorMode.Hue => new byte[width * height * 3],
            EColorMode.Greyscale => new byte[width * height],
            _ => new byte[width * height],
        };
        double dmin = Convert.ToDouble(min);
        double dmax = Convert.ToDouble(max);
        double drange = dmax - dmin;
        if (drange == 0) drange = 1; // Avoid division by zero
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double dval = Convert.ToDouble(array[y, x]);
                switch (colorMode)
                {
                    case EColorMode.Hue:
                        Color c = GetColorFromValue(Math.Clamp((dval - dmin) / drange, 0.0D, 1.0D));
                        int idx = (y * width + x) * 3;
                        pixels[idx] = c.R;
                        pixels[idx + 1] = c.G;
                        pixels[idx + 2] = c.B;
                        break;
                    case EColorMode.Greyscale:
                    default:
                        byte grey = (byte)Math.Clamp((dval - dmin) / drange * 255, 0, 255);
                        pixels[y * width + x] = grey;
                        break;
                }
            }
        }
        switch (colorMode)
        {
            case EColorMode.Hue:
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 3, 0);
                break;
            case EColorMode.Greyscale:
            default:
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width, 0);
                break;
        }
        return bitmap;
    }

    // Converts HSL to RGB Color
    private static Color HslToRgb(double h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = l - c / 2;
        double r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }
        return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
    }

    // Generates a 24-bit color from a normalized value (0.0 to 1.0) using hue rotation
    private static Color GetColorFromValue(double value)
    {
        // Clamp value to 0.0-1.0
        value = Math.Clamp(value, 0.0, 1.0);
        double hue = value * 360; // Map 0.0-1.0 to 0-360 degrees
        return HslToRgb(hue, 1.0, 0.5); // Full saturation, medium lightness for vibrant colors
    }
}