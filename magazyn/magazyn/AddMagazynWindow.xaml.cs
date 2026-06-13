using System.Windows;
using magazyn.Models;
using magazyn.Services;

namespace magazyn;

public partial class AddMagazynWindow : Window
{
    private readonly ApiClient _api;

    public AddMagazynWindow(ApiClient api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void ZapiszButton_Click(object sender, RoutedEventArgs e)
    {
        var nazwa = NazwaTextBox.Text.Trim();
        if (string.IsNullOrEmpty(nazwa))
        {
            MessageBox.Show("Podaj nazwę magazynu.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _api.AddMagazynAsync(new MagazynCreate { nazwa = nazwa });
            DialogResult = true;
            Close();
        }
        catch (Exception)
        {
            MessageBox.Show("Nie udało się dodać magazynu.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
