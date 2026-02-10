using BZTerrainEditor.ViewModels.Nodes;
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
    [NodeRegistration]
    public static void RegisterNode(GlobalNodeManager manager)
    {
        // we put this into the editor instead of the node because it exists because the editor exists, the node is generic
        manager.Register(typeof(ValueNode<string?, FilePathEditorViewModel>), "Value Node: FilePath", null, () => {
            var editor = new FilePathEditorViewModel();
            var node = new ValueNode<string?, FilePathEditorViewModel>(editor);
            node.Name = "Value Node: FilePath";
            return node;
        });
    }

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
