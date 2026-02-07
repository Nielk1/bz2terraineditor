using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace BZTerrainEditor.Views.Controls;
public partial class ArrowToggleButton : ToggleButton
{
    public static readonly DependencyProperty ArrowCheckedColorProperty =
        DependencyProperty.Register(
            nameof(ArrowCheckedColor),
            typeof(Color),
            typeof(ArrowToggleButton),
            new PropertyMetadata(Color.FromRgb(0x55, 0x55, 0x55)));

    public static readonly DependencyProperty ArrowUncheckedColorProperty =
        DependencyProperty.Register(
            nameof(ArrowUncheckedColor),
            typeof(Color),
            typeof(ArrowToggleButton),
            new PropertyMetadata(Color.FromRgb(0x33, 0x33, 0x33)));

    public Color ArrowCheckedColor
    {
        get => (Color)GetValue(ArrowCheckedColorProperty);
        set => SetValue(ArrowCheckedColorProperty, value);
    }

    public Color ArrowUncheckedColor
    {
        get => (Color)GetValue(ArrowUncheckedColorProperty);
        set => SetValue(ArrowUncheckedColorProperty, value);
    }

    public ArrowToggleButton()
    {
        InitializeComponent();
    }
}
