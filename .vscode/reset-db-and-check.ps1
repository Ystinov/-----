# Пересоздание базы данных после смены ролей + проверка запуска приложения.

$root = Split-Path -Parent $PSScriptRoot
$proj = (Get-ChildItem -Path $root -Recurse -Filter *.csproj | Select-Object -First 1).DirectoryName
Set-Location $proj
Write-Output ("project=" + $proj)

$out = Join-Path $proj 'bin\Debug\net10.0-windows'
Get-ChildItem (Join-Path $out 'hotel.db*') -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
Write-Output 'old database removed'

dotnet build
Write-Output ("build_exit=" + $LASTEXITCODE)

# Запускаем приложение и проверяем, что открылось окно входа
$exe = (Get-ChildItem (Join-Path $out '*.exe') | Select-Object -First 1).FullName
$p = Start-Process $exe -PassThru
Start-Sleep -Seconds 7
$p.Refresh()
Write-Output ("alive=" + (-not $p.HasExited))
Write-Output ("title=" + $p.MainWindowTitle)
if (-not $p.HasExited) { $p.Kill() }
Start-Sleep -Seconds 1

# Проверяем, какие роли попали в базу при начальном заполнении
$text = ''
foreach ($name in 'hotel.db', 'hotel.db-wal') {
    $f = Join-Path $out $name
    if (Test-Path $f) { $text += [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($f)) }
}
Write-Output ("role_manager=" + $text.Contains('Manager'))
Write-Output ("role_receptionist=" + $text.Contains('Receptionist'))
Write-Output ("role_housekeeper=" + $text.Contains('Housekeeper'))
Write-Output ("old_role_administrator=" + $text.Contains('Administrator'))
