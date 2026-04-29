using System.Windows;

namespace Contoso.OrderClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrderViewModel vm)
            {
                vm.LoadOrdersSync();
            }
        }
    }
}
