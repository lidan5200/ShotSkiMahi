using System.Windows;
using ShotSkiMahiD.ViewModels;

namespace ShotSkiMahiD.Views
{
    public partial class LogViewerWindow : Window
    {
        public LogViewerWindow(LogViewerViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
