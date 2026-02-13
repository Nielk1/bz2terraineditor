using BZTerrainEditor.ViewModels.Editors;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BZTerrainEditor.Views;

public partial class FilePathEditorView : UserControl, IViewFor<FilePathEditorViewModel>
{
    #region ViewModel
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
        typeof(FilePathEditorViewModel), typeof(FilePathEditorView), new PropertyMetadata(null, OnViewModelChanged));

    public FilePathEditorViewModel ViewModel
    {
        get => (FilePathEditorViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    object IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (FilePathEditorViewModel)value;
    }
    #endregion

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (FilePathEditorView)d;
        view.DataContext = e.NewValue;
    }

    public FilePathEditorView()
    {
        InitializeComponent();

        //this.WhenActivated(d => d(
        //    this.Bind(ViewModel, vm => vm.Value, v => v.valueUpDown.Value)
        //));
    }
}