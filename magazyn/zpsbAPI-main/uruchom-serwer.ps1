$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$venvUvicorn = Join-Path $PSScriptRoot "venv\Scripts\uvicorn.exe"
Write-Host "ctrl+c by wylaczyc" -ForegroundColor Green

try {
    & $venvUvicorn server:app --host 127.0.0.1 --port 8000
}
finally {
    Read-Host "Nacisnij Enter, aby zamknac okno"
}
