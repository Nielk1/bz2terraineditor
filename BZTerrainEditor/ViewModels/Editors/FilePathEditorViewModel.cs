using BZTerrainEditor.Views;
using NodeNetwork.Toolkit.ValueNode;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BZTerrainEditor.ViewModels.Editors;

public class FilePathEditorViewModel : ValueEditorViewModel<string?>
{
    public Guid EditorId { get; } = Guid.NewGuid();

    private bool _isGlobalVariable;
    public bool IsGlobalVariable
    {
        get => _isGlobalVariable;
        set
        {
            this.RaiseAndSetIfChanged(ref _isGlobalVariable, value);
            if (value)
                GlobalVariableManager.Instance.Register(this);
            else
                GlobalVariableManager.Instance.Unregister(this);
        }
    }

    public void SetValueFromString(string value)
    {
        Value = value;
    }


    static FilePathEditorViewModel()
    {
        Splat.Locator.CurrentMutable.Register(() => new FilePathEditorView(), typeof(IViewFor<FilePathEditorViewModel>));
    }

    public FilePathEditorViewModel()
    {
        Value = null;
    }
}
