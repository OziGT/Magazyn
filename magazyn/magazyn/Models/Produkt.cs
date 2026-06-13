namespace magazyn.Models;

public class Produkt
{
    public long id_produktu { get; set; }
    public string nazwa { get; set; } = string.Empty;
    public decimal cena { get; set; }
}
