using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using magazyn.Models;
using magazyn.Services;
using Microsoft.Win32;

namespace magazyn;

public partial class MainWindow : Window
{
    private readonly ApiClient _api = new();
    private List<Produkt> _produkty = [];
    private List<Magazyn> _magazyny = [];
    private List<StanMagazynowyWidok> _wszystkieWiersze = [];
    private bool _aktualizujeFiltry;

    public MainWindow()
    {
        InitializeComponent();
        PodlaczFiltry();
    }

    //Obsługa pól tekstowych filtrowania
    private void PodlaczFiltry()
    {
        FiltrProduktComboBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) => ZastosujFiltr()), true);
        FiltrMagazynComboBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) => ZastosujFiltr()), true);
        FiltrProduktComboBox.PreviewKeyUp += (_, _) => ZastosujFiltr();
        FiltrMagazynComboBox.PreviewKeyUp += (_, _) => ZastosujFiltr();
        FiltrProduktComboBox.SelectionChanged += (_, _) => ZastosujFiltr();
        FiltrMagazynComboBox.SelectionChanged += (_, _) => ZastosujFiltr();
        FiltrProduktComboBox.DropDownClosed += (_, _) => ZastosujFiltr();
        FiltrMagazynComboBox.DropDownClosed += (_, _) => ZastosujFiltr();
    }

    private void ZastosujFiltr()
    {
        if (_aktualizujeFiltry)
            return;

        var filtrProdukt = FiltrProduktComboBox.Text.Trim();
        var filtrMagazyn = FiltrMagazynComboBox.Text.Trim();

        IEnumerable<StanMagazynowyWidok> wynik = _wszystkieWiersze;

        //sprawdza dla każdego wiersza w, czy jest tekst z filtra
        if (!string.IsNullOrEmpty(filtrProdukt))
            wynik = wynik.Where(w => w.Produkt.Contains(filtrProdukt, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(filtrMagazyn))
            wynik = wynik.Where(w => w.Magazyn.Contains(filtrMagazyn, StringComparison.OrdinalIgnoreCase));

        ProduktyDataGrid.ItemsSource = wynik.ToList();
    }

    private void UstawFiltryComboBox()
    {
        //Zabezpieczenie przed zmianą filtrów podczas aktualizacji
        _aktualizujeFiltry = true;
        try
        {
            FiltrProduktComboBox.ItemsSource = _produkty
                .Select(p => p.nazwa)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();

            FiltrMagazynComboBox.ItemsSource = _magazyny
                .Select(m => m.nazwa)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();
        }
        finally
        {
            _aktualizujeFiltry = false;
        }
    }

    private void PrzywrocFiltry(string filtrProdukt, string filtrMagazyn)
    {
        _aktualizujeFiltry = true;
        try
        {
            FiltrProduktComboBox.Text = filtrProdukt;
            FiltrMagazynComboBox.Text = filtrMagazyn;
        }
        finally
        {
            _aktualizujeFiltry = false;
        }

        ZastosujFiltr();
    }

    //Obsługa logowania
    private async void ZalogujButton_Click(object sender, RoutedEventArgs e)
    {
        await ZalogujAsync();
    }

    private async void NazwiskoTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await ZalogujAsync();
    }

    private async void EdytujButton_Click(object sender, RoutedEventArgs e)
    {
        //Tag obsługuje który wiersz został kliknięty
        if (sender is not System.Windows.Controls.Button { Tag: StanMagazynowyWidok pozycja })
            return;

        var okno = new EditStanMagazynowyWindow(_api, _produkty, _magazyny, pozycja)
        {
            Owner = this
        };

        if (okno.ShowDialog() == true)
            await OdswiezTabeleAsync();
    }

    private async void DodajProduktButton_Click(object sender, RoutedEventArgs e)
    {
        var okno = new EditStanMagazynowyWindow(_api, _produkty, _magazyny)
        {
            Owner = this
        };

        if (okno.ShowDialog() == true)
            await OdswiezTabeleAsync();
    }

    private async void DodajMagazynButton_Click(object sender, RoutedEventArgs e)
    {
        var okno = new AddMagazynWindow(_api)
        {
            Owner = this
        };

        if (okno.ShowDialog() == true)
            await OdswiezTabeleAsync();
    }

    //Eksport do CSV
    private void EksportButton_Click(object sender, RoutedEventArgs e)
    {
        List<StanMagazynowyWidok> lista;
        if (ProduktyDataGrid.ItemsSource is IEnumerable<StanMagazynowyWidok> wiersze)
            lista = wiersze.ToList();
        else
            lista = [];

        var dialog = new SaveFileDialog
        {
            Filter = "Plik CSV (*.csv)|*.csv",
            FileName = "magazyn.csv"
        };

        if (dialog.ShowDialog() != true)
            return;

        var csv = new StringBuilder();
        csv.AppendLine("Produkt;Magazyn;Ilość;Cena;Wartość");

        foreach (var wiersz in lista)
        {
            csv.Append(wiersz.Produkt).Append(';');
            csv.Append(wiersz.Magazyn).Append(';');
            csv.Append(wiersz.Ilosc).Append(';');
            csv.Append(wiersz.Cena.ToString("0.00", CultureInfo.InvariantCulture)).Append(';');
            csv.AppendLine(wiersz.Wartosc.ToString("0.00", CultureInfo.InvariantCulture));
        }

        File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        MessageBox.Show("CSV wyeksportowany", "Eksport", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void UsunButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: StanMagazynowyWidok pozycja })
            return;

        var potwierdzenie = MessageBox.Show(
            $"Czy na pewno usunąć pozycję:\n{pozycja.Produkt} — {pozycja.Magazyn} (ilość: {pozycja.Ilosc})?",
            "Potwierdzenie usunięcia",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (potwierdzenie != MessageBoxResult.Yes)
            return;

        try
        {
            foreach (var id in pozycja.Ids)
                await _api.DeleteProduktMagazynAsync(id);

            await OdswiezTabeleAsync();
        }
        catch (Exception)
        {
            MessageBox.Show("Nie udało się usunąć pozycji.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ZalogujAsync()
    {
        //Ukrywa panel błędu i panel tabeli
        BladTextBlock.Visibility = Visibility.Collapsed;
        TabelaPanel.Visibility = Visibility.Collapsed;

        var imie = ImieTextBox.Text.Trim();
        var nazwisko = NazwiskoTextBox.Text.Trim();

        if (string.IsNullOrEmpty(imie) || string.IsNullOrEmpty(nazwisko))
        {
            BladTextBlock.Text = "Błędny pracownik";
            BladTextBlock.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            var pracownicy = await _api.GetPracownicyAsync();
            var pracownik = pracownicy.FirstOrDefault(p =>
                string.Equals(p.imie.Trim(), imie, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.nazwisko.Trim(), nazwisko, StringComparison.OrdinalIgnoreCase));

            if (pracownik is null)
            {
                BladTextBlock.Text = "Błędny pracownik";
                BladTextBlock.Visibility = Visibility.Visible;
                return;
            }

            await OdswiezTabeleAsync();

            LoginPanel.Visibility = Visibility.Collapsed;
            LoggedInUserTextBlock.Text = $"{pracownik.imie} {pracownik.nazwisko}";
            LoggedInUserTextBlock.Visibility = Visibility.Visible;
            TabelaPanel.Visibility = Visibility.Visible;
        }
        catch (Exception)
        {
            BladTextBlock.Text = "Nie udało się połączyć z API";
            BladTextBlock.Visibility = Visibility.Visible;
        }
    }
    
    private async Task OdswiezTabeleAsync()
    {
        var filtrProdukt = FiltrProduktComboBox.Text;
        var filtrMagazyn = FiltrMagazynComboBox.Text;

        //pobieranie danych z API
        _produkty = await _api.GetProduktyAsync();
        _magazyny = await _api.GetMagazynyAsync();
        var stany = await _api.GetProduktMagazynAsync();

        //konwersja na słowniki
        var produktyDict = _produkty.ToDictionary(p => p.id_produktu, p => p.nazwa);
        var cenyDict = _produkty.ToDictionary(p => p.id_produktu, p => p.cena);
        var magazynyDict = _magazyny.ToDictionary(m => m.id_magazynu, m => m.nazwa);

        //wszystkie stany
        _wszystkieWiersze = stany
            //grupowanie każdego produktu w magazynie
            .GroupBy(s => new { s.id_produktu, s.id_magazynu })
            //Tworzenie jednego wiersza z grupy
            .Select(g =>
            {
                var idProduktu = g.Key.id_produktu;
                var idMagazynu = g.Key.id_magazynu;
                //nowy obiekt stanu magazynu o połączonych wartościach
                return new StanMagazynowyWidok
                {
                    Ids = g.Select(x => x.id).ToList(),
                    IdProduktu = idProduktu,
                    IdMagazynu = idMagazynu,
                    Produkt = produktyDict.GetValueOrDefault(idProduktu, $"Produkt #{idProduktu}"),
                    Magazyn = magazynyDict.GetValueOrDefault(idMagazynu, $"Magazyn #{idMagazynu}"),
                    Ilosc = g.Sum(x => x.ilosc),
                    Cena = cenyDict.GetValueOrDefault(idProduktu, 0)
                };
            })
            .OrderBy(s => s.Produkt)
            .ThenBy(s => s.Magazyn)
            .ToList();

        UstawFiltryComboBox();
        PrzywrocFiltry(filtrProdukt, filtrMagazyn);
    }
}
