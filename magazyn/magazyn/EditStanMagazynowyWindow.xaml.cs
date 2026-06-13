using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using magazyn.Models;
using magazyn.Services;

namespace magazyn;

public partial class EditStanMagazynowyWindow : Window
{
    private readonly ApiClient _api;
    private readonly List<Produkt> _produkty;
    private readonly long? _id;
    private readonly List<long> _pozostaleIds;

    public EditStanMagazynowyWindow(
        ApiClient api,
        List<Produkt> produkty,
        List<Magazyn> magazyny,
        StanMagazynowyWidok? pozycja = null)
    {
        InitializeComponent();
        _api = api;
        _produkty = produkty;
        _id = pozycja is { Id: > 0 } ? pozycja.Id : null;
        _pozostaleIds = pozycja?.Ids.Skip(1).ToList() ?? [];

        Title = pozycja is null ? "Dodaj pozycję" : "Edytuj pozycję";

        ProduktComboBox.ItemsSource = _produkty;
        MagazynComboBox.ItemsSource = magazyny;
        ProduktComboBox.SelectionChanged += (_, _) => AktualizujPoleCeny();
        Loaded += OnLoaded;

        if (pozycja is not null)
        {
            ProduktComboBox.SelectedValue = pozycja.IdProduktu;
            MagazynComboBox.SelectedValue = pozycja.IdMagazynu;
            IloscTextBox.Text = pozycja.Ilosc.ToString();
        }

        if (pozycja is null && magazyny.Count > 0)
            MagazynComboBox.SelectedIndex = 0;

        AktualizujPoleCeny();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ProduktComboBox.Template.FindName("PART_EditableTextBox", ProduktComboBox) is TextBox textBox)
            textBox.TextChanged += (_, _) => AktualizujPoleCeny();
    }

    private Produkt? ZnajdzProduktPoNazwie()
    {
        var nazwa = ProduktComboBox.Text.Trim();
        if (string.IsNullOrEmpty(nazwa))
            return null;

        if (ProduktComboBox.SelectedItem is Produkt wybrany &&
            string.Equals(wybrany.nazwa.Trim(), nazwa, StringComparison.OrdinalIgnoreCase))
            return wybrany;

        return _produkty.FirstOrDefault(p =>
            string.Equals(p.nazwa.Trim(), nazwa, StringComparison.OrdinalIgnoreCase));
    }

    private void AktualizujPoleCeny()
    {
        var produkt = ZnajdzProduktPoNazwie();
        if (produkt is not null)
        {
            CenaTextBox.Text = produkt.cena.ToString("0.##", CultureInfo.CurrentCulture);
            CenaTextBox.IsEnabled = false;
            return;
        }

        if (!CenaTextBox.IsEnabled)
            CenaTextBox.Clear();

        CenaTextBox.IsEnabled = true;
    }
    //string na decimal
    private static bool TryParseCena(string text, out decimal cena)
    {
        text = text.Trim();
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out cena)
               || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out cena);
    }

    private async Task<long?> GetOrCreateProduktIdAsync()
    {
        //pusty tekst
        var nazwa = ProduktComboBox.Text.Trim();
        if (string.IsNullOrEmpty(nazwa))
            return null;

        //zwraca id istniejacego produktu
        var istniejacy = ZnajdzProduktPoNazwie();
        if (istniejacy is not null)
            return istniejacy.id_produktu;

        //sprawdza cene
        if (!TryParseCena(CenaTextBox.Text, out var cena) || cena <= 0)
            return null;

        // nowy produkt przez API
        var nowy = await _api.AddProduktAsync(new ProduktCreate { nazwa = nazwa, cena = cena });
        _produkty.Add(nowy);
        return nowy.id_produktu;
    }

    private async void ZapiszButton_Click(object sender, RoutedEventArgs e)
    {
        var nazwa = ProduktComboBox.Text.Trim();
        if (string.IsNullOrEmpty(nazwa) || MagazynComboBox.SelectedValue is not long)
        {
            MessageBox.Show("Podaj nazwę produktu i wybierz magazyn.",
                "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ZnajdzProduktPoNazwie() is null &&
            (!TryParseCena(CenaTextBox.Text, out var cena) || cena <= 0))
        {
            MessageBox.Show("Podaj poprawną cenę nowego produktu (liczba większa od 0).",
                "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        long? idProduktu;
        try
        {
            idProduktu = await GetOrCreateProduktIdAsync();
        }
        catch (Exception)
        {
            MessageBox.Show("Nie udało się utworzyć produktu.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (idProduktu is not long idProdukt ||
            MagazynComboBox.SelectedValue is not long idMagazynu)
        {
            MessageBox.Show("Podaj nazwę produktu i wybierz magazyn.",
                "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(IloscTextBox.Text.Trim(), out var ilosc) || ilosc < 0)
        {
            MessageBox.Show("Podaj poprawną ilość (liczba całkowita ≥ 0).", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var data = new ProduktMagazynUpdate
        {
            id_produktu = idProdukt,
            id_magazynu = idMagazynu,
            ilosc = ilosc
        };

        try
        {
            // jak jest produkt, to zmień info, jak nie ma to dodaj w api
            if (_id is long id)
            {
                await _api.UpdateProduktMagazynAsync(id, data);
                foreach (var extraId in _pozostaleIds)
                    await _api.DeleteProduktMagazynAsync(extraId);
            }
            else
                await _api.AddProduktMagazynAsync(data);

            DialogResult = true;
            Close();
        }
        catch (Exception)
        {
            var komunikat = _id is null ? "Nie udało się dodać pozycji." : "Nie udało się zapisać zmian.";
            MessageBox.Show(komunikat, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
