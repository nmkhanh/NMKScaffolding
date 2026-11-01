using System.ComponentModel;
using System.Windows;
using RevitScaffoldingTool.ViewModels;

namespace RevitScaffoldingTool.Views
{
    public partial class ScaffoldingWindow : Window
    {
        public ScaffoldingWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            (DataContext as ScaffoldingViewModel)?.SaveSettings();
        }
    }
}
