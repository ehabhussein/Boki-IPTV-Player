using System.Windows;
using BokiIPTV.App.ViewModels;

namespace BokiIPTV.App;

public partial class LoginView : Window
{
    public LoginView() => InitializeComponent();

    private void Pwd_OnChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm) vm.Password = Pwd.Password;
    }
}
