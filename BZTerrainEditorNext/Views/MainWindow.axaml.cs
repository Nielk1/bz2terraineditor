using Avalonia.Controls;
using System;

namespace BZTerrainEditorNext.Views
{
    public partial class MainWindow : Window
    {
        private readonly Random _rand = new Random();

        public MainWindow()
        {
            InitializeComponent();
        }
    }
}