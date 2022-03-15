using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ReplaceViewSheet
{
    public partial class ReplaceViewSheetWindow
    {
        private ReplaceViewSheetViewModel _viewModel;

        public ReplaceViewSheetWindow(ReplaceViewSheetViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = viewModel;
        }



    }
}
