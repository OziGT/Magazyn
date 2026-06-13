using System.Windows;
using magazyn.Config;

namespace magazyn
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppConfig.Load();
            base.OnStartup(e);
        }
    }

}
