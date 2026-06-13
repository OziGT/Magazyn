using System.Net.Http;
using System.Net.Http.Json;
using magazyn.Config;
using magazyn.Models;

namespace magazyn.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient()
    {
        _http = new HttpClient { BaseAddress = new Uri(AppConfig.ApiUrl) };
        _http.DefaultRequestHeaders.Add("x-api-key", AppConfig.ApiKey);
    }

    public async Task<List<Pracownik>> GetPracownicyAsync()
    {
        return await _http.GetFromJsonAsync<List<Pracownik>>("/pracownicy")
               ?? new List<Pracownik>();
    }

    public async Task<List<Produkt>> GetProduktyAsync()
    {
        return await _http.GetFromJsonAsync<List<Produkt>>("/produkty")
               ?? new List<Produkt>();
    }

    public async Task<Produkt> AddProduktAsync(ProduktCreate data)
    {
        var response = await _http.PostAsJsonAsync("/produkty", data);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Produkt>())
               ?? throw new InvalidOperationException("Pusta odpowiedź API przy tworzeniu produktu.");
    }

    public async Task<List<Magazyn>> GetMagazynyAsync()
    {
        return await _http.GetFromJsonAsync<List<Magazyn>>("/magazyny")
               ?? new List<Magazyn>();
    }

    public async Task<Magazyn> AddMagazynAsync(MagazynCreate data)
    {
        var response = await _http.PostAsJsonAsync("/magazyny", data);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Magazyn>())
               ?? throw new InvalidOperationException("Pusta odpowiedź API przy tworzeniu magazynu.");
    }

    public async Task<List<ProduktMagazyn>> GetProduktMagazynAsync()
    {
        return await _http.GetFromJsonAsync<List<ProduktMagazyn>>("/produktMagazyn")
               ?? new List<ProduktMagazyn>();
    }

    public async Task AddProduktMagazynAsync(ProduktMagazynUpdate data)
    {
        var response = await _http.PostAsJsonAsync("/produktMagazyn", data);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateProduktMagazynAsync(long id, ProduktMagazynUpdate data)
    {
        var response = await _http.PutAsJsonAsync($"/produktMagazyn/{id}", data);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteProduktMagazynAsync(long id)
    {
        var response = await _http.DeleteAsync($"/produktMagazyn/{id}");
        response.EnsureSuccessStatusCode();
    }
}
