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
    static FilePathEditorViewModel()
    {
        Splat.Locator.CurrentMutable.Register(() => new FilePathEditorView(), typeof(IViewFor<FilePathEditorViewModel>));
    }

    public FilePathEditorViewModel()
    {
        Value = null;
    }
}
