using BZTerrainEditor.Types;
using BZTerrainEditor.ViewModels;
using BZTerrainEditor.ViewModels.Editors;
using NodeNetwork.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reactive.Disposables;
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

namespace BZTerrainEditor.Views;

public partial class EnumEditorView : UserControl, IViewFor<IEnumEditorViewModel>
{
    #region ViewModel
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
        typeof(IEnumEditorViewModel), typeof(EnumEditorView), new PropertyMetadata(null, OnViewModelChanged));

    public IEnumEditorViewModel ViewModel
    {
        get => (IEnumEditorViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    object IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (IEnumEditorViewModel)value;
    }
    #endregion

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (EnumEditorView)d;
        view.DataContext = e.NewValue;
    }

    public EnumEditorView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.OptionLabels, v => v.valueComboBox.ItemsSource).DisposeWith(d);
            this.Bind(ViewModel, vm => vm.SelectedOptionIndex, v => v.valueComboBox.SelectedIndex).DisposeWith(d);
        });
    }
}