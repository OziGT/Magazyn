# Uzupelnia baze danych przez API (max 5 rekordow na tabele).
$ErrorActionPreference = "Stop"

$BaseUrl = "http://127.0.0.1:8000"
$ApiKey = "tajny-klucz-123"
$MaxPerTable = 5

$Headers = @{
    "x-api-key"    = $ApiKey
    "Content-Type" = "application/json; charset=utf-8"
}

function Get-ApiList {
    param([string]$Path)
    $result = Invoke-RestMethod -Uri "$BaseUrl$Path" -Headers @{ "x-api-key" = $ApiKey } -Method Get
    if ($null -eq $result) { return @() }
    if ($result -is [System.Array]) { return $result }
    return @($result)
}

function Invoke-ApiPost {
    param(
        [string]$Path,
        [hashtable]$Body
    )
    $json = $Body | ConvertTo-Json -Compress
    try {
        return Invoke-RestMethod -Uri "$BaseUrl$Path" -Headers $Headers -Method Post -Body ([System.Text.Encoding]::UTF8.GetBytes($json))
    }
    catch {
        $detail = $_.ErrorDetails.Message
        if (-not $detail) { $detail = $_.Exception.Message }
        throw "POST $Path -> $detail"
    }
}

function Add-SeedRecords {
    param(
        [string]$GetPath,
        [string]$PostPath,
        [object[]]$Items,
        [scriptblock]$Label = { $_ | ConvertTo-Json -Compress }
    )

    $existing = @(Get-ApiList $GetPath)
    if ($existing.Count -ge $MaxPerTable) {
        Write-Host "  $GetPath : juz $($existing.Count) rekordow - pomijam" -ForegroundColor DarkYellow
        return $existing
    }

    $created = [System.Collections.Generic.List[object]]::new()
    foreach ($item in $existing) { [void]$created.Add($item) }

    foreach ($item in $Items) {
        if ($created.Count -ge $MaxPerTable) { break }
        $row = Invoke-ApiPost -Path $PostPath -Body $item
        [void]$created.Add($row)
        $text = & $Label $row
        Write-Host "  + $GetPath : $text" -ForegroundColor Green
    }

    return $created.ToArray()
}

Write-Host "Laczenie z API: $BaseUrl" -ForegroundColor Cyan
try {
    Invoke-RestMethod -Uri "$BaseUrl/" -Method Get | Out-Null
}
catch {
    Write-Host "Nie mozna polaczyc z API. Uruchom najpierw serwer (uruchom-serwer.ps1)." -ForegroundColor Red
    Read-Host "Nacisnij Enter, aby zamknac"
    exit 1
}

Write-Host "`nUzupelnianie bazy danych...`n" -ForegroundColor Cyan

$pracownicy = Add-SeedRecords -GetPath "/pracownicy" -PostPath "/add_pracownik" -Items @(
    @{ imie = "Jan"; nazwisko = "Kowalski"; stanowisko = "magazynier" }
    @{ imie = "Anna"; nazwisko = "Nowak"; stanowisko = "kierownik magazynu" }
    @{ imie = "Piotr"; nazwisko = "Wisniewski"; stanowisko = "magazynier" }
    @{ imie = "Maria"; nazwisko = "Lewandowska"; stanowisko = "logistyk" }
    @{ imie = "Tomasz"; nazwisko = "Zielinski"; stanowisko = "magazynier" }
) -Label { param($r) "$($r.imie) $($r.nazwisko)" }

$statusy = Add-SeedRecords -GetPath "/statusy" -PostPath "/add_status" -Items @(
    @{ nazwa = "Nowe" }
    @{ nazwa = "W realizacji" }
    @{ nazwa = "Zrealizowane" }
    @{ nazwa = "Anulowane" }
    @{ nazwa = "Oczekujace" }
) -Label { param($r) $r.nazwa }

$formyPlatnosci = Add-SeedRecords -GetPath "/formy_platnosci" -PostPath "/formy_platnosci" -Items @(
    @{ nazwa = "Gotowka" }
    @{ nazwa = "Przelew" }
    @{ nazwa = "Karta" }
    @{ nazwa = "BLIK" }
    @{ nazwa = "Pobranie" }
) -Label { param($r) $r.nazwa }

$formyDostawy = Add-SeedRecords -GetPath "/formyDostawy" -PostPath "/add_formaDostawy" -Items @(
    @{ nazwa = "Kurier" }
    @{ nazwa = "Paczkomat" }
    @{ nazwa = "Odbior osobisty" }
    @{ nazwa = "Poczta" }
    @{ nazwa = "Transport wlasny" }
) -Label { param($r) $r.nazwa }

$firmy = Add-SeedRecords -GetPath "/firmyDostawcze" -PostPath "/add_firmaDostawcza" -Items @(
    @{ nazwa = "DHL" }
    @{ nazwa = "InPost" }
    @{ nazwa = "Poczta Polska" }
    @{ nazwa = "DPD" }
    @{ nazwa = "FedEx" }
) -Label { param($r) $r.nazwa }

$klienci = Add-SeedRecords -GetPath "/klienci" -PostPath "/klienci" -Items @(
    @{ imie = "Adam"; nazwisko = "Kowalczyk"; telefon = "600100200"; email = "adam@example.com" }
    @{ imie = "Ewa"; nazwisko = "Wojcik"; telefon = "600100201"; email = "ewa@example.com" }
    @{ imie = "Krzysztof"; nazwisko = "Kaminski"; telefon = "600100202"; email = "krz@example.com" }
    @{ imie = "Joanna"; nazwisko = "Krawczyk"; telefon = "600100203"; email = "joanna@example.com" }
    @{ imie = "Michal"; nazwisko = "Piotrowski"; telefon = "600100204"; email = "michal@example.com" }
) -Label { param($r) "$($r.imie) $($r.nazwisko)" }

$produkty = Add-SeedRecords -GetPath "/produkty" -PostPath "/produkty" -Items @(
    @{ nazwa = "Marchew"; cena = 3.5 }
    @{ nazwa = "Jablka"; cena = 6.0 }
    @{ nazwa = "Mleko 1L"; cena = 4.5 }
    @{ nazwa = "Chleb"; cena = 5.5 }
    @{ nazwa = "Ryz 1kg"; cena = 8.0 }
) -Label { param($r) $r.nazwa }

$magazyny = Add-SeedRecords -GetPath "/magazyny" -PostPath "/magazyny" -Items @(
    @{ nazwa = "Magazyn Glowny" }
    @{ nazwa = "Magazyn Regionalny" }
    @{ nazwa = "Magazyn Chlodnia" }
    @{ nazwa = "Magazyn Wysylkowy" }
    @{ nazwa = "Magazyn Zapasowy" }
) -Label { param($r) $r.nazwa }

$adresyItems = for ($i = 0; $i -lt 5; $i++) {
    @{
        id_klienta   = [int64]$klienci[$i % $klienci.Count].id_klienta
        miasto       = "Warszawa"
        ulica        = "ul. Testowa $($i + 1)"
        kod_pocztowy = "00-10$i"
    }
}
[void](Add-SeedRecords -GetPath "/adresy" -PostPath "/adresy" -Items $adresyItems -Label { param($r) "$($r.miasto), $($r.ulica)" })

$dostawyItems = for ($i = 0; $i -lt 5; $i++) {
    @{
        id_formy_dostawy = [int64]$formyDostawy[$i % $formyDostawy.Count].id_formy_dostawy
        id_firmy         = [int64]$firmy[$i % $firmy.Count].id_firmy
    }
}
$dostawy = Add-SeedRecords -GetPath "/dostawy" -PostPath "/add_dostawa" -Items $dostawyItems -Label { param($r) "id=$($r.id_dostawy)" }

$pmItems = for ($i = 0; $i -lt 5; $i++) {
    @{
        id_produktu = [int64]$produkty[$i % $produkty.Count].id_produktu
        id_magazynu = [int64]$magazyny[$i % $magazyny.Count].id_magazynu
        ilosc       = ($i + 1) * 10
    }
}
[void](Add-SeedRecords -GetPath "/produktMagazyn" -PostPath "/produktMagazyn" -Items $pmItems -Label { param($r) "produkt=$($r.id_produktu), magazyn=$($r.id_magazynu), ilosc=$($r.ilosc)" })

$zamowieniaItems = for ($i = 0; $i -lt 5; $i++) {
    @{
        data               = (Get-Date "2026-03-10").AddDays($i).ToString("yyyy-MM-dd")
        id_statusu         = [int64]$statusy[$i % $statusy.Count].id_statusu
        id_pracownika      = [int64]$pracownicy[$i % $pracownicy.Count].id_pracownika
        id_klienta         = [int64]$klienci[$i % $klienci.Count].id_klienta
        id_formy_platnosci = [int64]$formyPlatnosci[$i % $formyPlatnosci.Count].id_formy_platnosci
        id_dostawy         = [int64]$dostawy[$i % $dostawy.Count].id_dostawy
    }
}
$zamowienia = Add-SeedRecords -GetPath "/zamowienia" -PostPath "/zamowienia" -Items $zamowieniaItems -Label { param($r) "id=$($r.id_zamowienia), data=$($r.data)" }

$pzItems = for ($i = 0; $i -lt 5; $i++) {
    $prod = $produkty[$i % $produkty.Count]
    @{
        id_zamowienia = [int64]$zamowienia[$i % $zamowienia.Count].id_zamowienia
        id_produktu   = [int64]$prod.id_produktu
        ilosc         = $i + 1
        cena_zakupu   = [decimal]$prod.cena
    }
}
[void](Add-SeedRecords -GetPath "/pozycje_zamowienia" -PostPath "/pozycje_zamowienia" -Items $pzItems -Label { param($r) "zamowienie=$($r.id_zamowienia), produkt=$($r.id_produktu)" })

Write-Host "`nPodsumowanie:" -ForegroundColor Cyan
$endpoints = @(
    "pracownicy", "statusy", "formy_platnosci", "formyDostawy", "firmyDostawcze",
    "klienci", "produkty", "magazyny", "adresy", "dostawy",
    "produktMagazyn", "zamowienia", "pozycje_zamowienia"
)
foreach ($ep in $endpoints) {
    $count = (Get-ApiList "/$ep").Count
    Write-Host "  ${ep}: $count rekordow"
}

Write-Host "`nGotowe." -ForegroundColor Green
Read-Host "Nacisnij Enter, aby zamknac"
