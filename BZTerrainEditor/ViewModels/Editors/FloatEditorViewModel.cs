/*using System.Globalization;
using System.Reactive.Linq;
using NodeNetwork.Toolkit.ValueNode;
using ReactiveUI;

namespace BZTerrainEditor.ViewModels.Editors;

public class FloatEditorViewModel : ValueEditorViewModel<ShaderFunc>
{
    static FloatEditorViewModel()
    {
        Splat.Locator.CurrentMutable.Register(() => new FloatEditorView(), typeof(IViewFor<FloatEditorViewModel>));
    }

    #region FloatValue
    private float _floatValue;
    public float FloatValue
    {
        get => _floatValue;
        set => this.RaiseAndSetIfChanged(ref _floatValue, value);
    }
    #endregion

    public FloatEditorViewModel()
    {
        this.WhenAnyValue(vm => vm.FloatValue)
            .Select(v => new ShaderFunc(() => v.ToString(CultureInfo.InvariantCulture)))
            .BindTo(this, vm => vm.Value);
    }
}*/