using Microsoft.Maui.Controls;

namespace Dawidek
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            SetValue(NavigationPage.HasNavigationBarProperty, false);
        }
    }
}
