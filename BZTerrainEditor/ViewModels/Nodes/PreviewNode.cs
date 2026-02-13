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

public class PreviewNode : NodeViewModel, IPreviewNode
{
    [NodeRegistration]
    public static void RegisterNode(GlobalNodeManager manager)
    {
        manager.Register(typeof(PreviewNode), $"Preview", $"Preview image", () => new PreviewNode());
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

    private BitmapSource _previewImage;
    public BitmapSource PreviewImage
    {
        get => _previewImage;
        private set => this.RaiseAndSetIfChanged(ref _previewImage, value);
    }

    private Views.PreviewWindow? _previewWindow; // Added to track the open window for updates and reuse

    static PreviewNode()
    {
        Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<PreviewNode>));
    }

    public PreviewNode()
    {
        Name = "Preview";

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

        // Process the first non-null input
        var imageObs = combinedInputsObs.Select(tuple =>
        {
            var (input, value) = tuple;
            if (input == null || value == null)
                return (BitmapSource?)null;

            var arrayType = input.GetType().GenericTypeArguments[0]; // T[,]
            var elementType = arrayType.GetElementType()!; // T (element type)
            // value is expected to be T[,]
            var array = value;
            // Use reflection to call FindMinMax and CreatePreviewImage
            var findMinMaxMethod = typeof(PreviewNode).GetMethod("FindMinMax", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.MakeGenericMethod(elementType);
            var createPreviewImageMethod = typeof(PreviewNode).GetMethod("CreatePreviewImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.MakeGenericMethod(elementType);

            var minMax = findMinMaxMethod?.Invoke(null, new object[] { array });
            if (minMax == null) return null;
            var min = minMax.GetType().GetField("Item1")?.GetValue(minMax);
            var max = minMax.GetType().GetField("Item2")?.GetValue(minMax);

            return (BitmapSource?)createPreviewImageMethod?.Invoke(null, new object[] { array, min, max });
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

    private static (T min, T max) FindMinMax<T>(T[,] array) where T: INumber<T>
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

    private static BitmapSource CreatePreviewImage<T>(T[,] array, T min, T max) where T : INumber<T>
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