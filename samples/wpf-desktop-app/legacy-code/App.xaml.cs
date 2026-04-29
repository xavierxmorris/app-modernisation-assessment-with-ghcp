using System.Configuration;
using System.Windows;

namespace Contoso.OrderClient
{
    public partial class App : Application
    {
        public static string ApiBaseUrl { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ApiBaseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"];

            var window = new MainWindow();
            window.DataContext = new OrderViewModel();
            window.Show();
        }
    }
}
