using BZTerrainEditor.ViewModels.Nodes;
using BZTerrainEditor.Views;
using Microsoft.Win32;
using NodeNetwork.Toolkit.ValueNode;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;

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

    public bool IsSaveMode { get; set; } = false; // false for open, true for save

    public ReactiveCommand<Unit, Unit> SelectFileCommand { get; }

    private readonly ObservableAsPropertyHelper<string> _filename;
    public string Filename => _filename.Value;

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
        SelectFileCommand = ReactiveCommand.Create(SelectFile);

        // Calculate Preview reactively
        _filename = this.WhenAnyValue(vm => vm.Value)
            .Select(v => string.IsNullOrEmpty(v) ? "No file" : Path.GetFileName(v))
            .ToProperty(this, vm => vm.Filename);
    }

    private void SelectFile()
    {
        FileDialog dialog = IsSaveMode ? new SaveFileDialog() : new OpenFileDialog();
        if (dialog.ShowDialog() == true)
        {
            Value = dialog.FileName;
        }
    }
}
