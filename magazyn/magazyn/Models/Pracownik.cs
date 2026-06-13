namespace magazyn.Models;

public class Pracownik
{
    public long id_pracownika { get; set; }
    public string imie { get; set; } = string.Empty;
    public string nazwisko { get; set; } = string.Empty;
    public string? stanowisko { get; set; }
}
