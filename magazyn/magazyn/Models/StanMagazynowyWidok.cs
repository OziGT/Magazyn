namespace magazyn.Models;

public class StanMagazynowyWidok
{
    public List<long> Ids { get; set; } = [];
    public long Id => Ids.Count > 0 ? Ids[0] : 0;
    public long IdProduktu { get; set; }
    public long IdMagazynu { get; set; }
    public string Produkt { get; set; } = string.Empty;
    public string Magazyn { get; set; } = string.Empty;
    public int Ilosc { get; set; }
    public decimal Cena { get; set; }
    public decimal Wartosc => Cena * Ilosc;
}
