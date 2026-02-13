using BZTerrainEditor.Types;
using BZTerrainEditor.Views;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BZTerrainEditor.ViewModels.Editors;

public interface IEnumEditorViewModel
{
    string[] OptionLabels { get; }
    int SelectedOptionIndex { get; set; }
}

public static class EnumEditorNodeResistration
{
    [NodeRegistration(IsTypeBased = true)]
    public static void RegisterNode(GlobalNodeManager manager, Type t)
    {
        // Check if T is an array of INumber<T> elements
        if (t != null && t.IsEnum)
        {
            Type nodeType = typeof(EnumEditorViewModel<>).MakeGenericType(t);
            manager.Register(nodeType, $"Value Node: {t.GetNiceTypeName()}", null, () => (NodeViewModel)Activator.CreateInstance(nodeType));
        }
    }
}

public class EnumEditorViewModel<T> : ValueEditorViewModel<T>, IEnumEditorViewModel where T : Enum
{
    static EnumEditorViewModel()
    {
        Splat.Locator.CurrentMutable.Register(() => new EnumEditorView(), typeof(IViewFor<EnumEditorViewModel<T>>));
    }

    public T[] Options { get; }
    public string[] OptionLabels { get; }

    #region SelectedOptionIndex
    private int _selectedOptionIndex;
    public int SelectedOptionIndex
    {
        get => _selectedOptionIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedOptionIndex, value);
    }
    #endregion

    public EnumEditorViewModel()
    {
        var enumType = typeof(T);
        Options = (T[])Enum.GetValues(enumType);
        OptionLabels = Options.Select(c => Enum.GetName(enumType, c)).ToArray();

        this.WhenAnyValue(vm => vm.SelectedOptionIndex)
            .Select(i => i == -1 ? default : Options[i])
            .BindTo(this, vm => vm.Value);
    }
}
