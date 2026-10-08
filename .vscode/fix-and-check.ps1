# Пересоздание миграций и базы данных + проверка запуска приложения.
# Запускается как задача VS Code из папки .vscode.

$root = Split-Path -Parent $PSScriptRoot
$proj = (Get-ChildItem -Path $root -Recurse -Filter *.csproj | Select-Object -First 1).DirectoryName
Set-Location $proj
Write-Output ("project=" + $proj)

# Лишний файл конвертера больше не нужен
$converter = Join-Path $proj 'Infrastructure\MoneyConverter.cs'
if (Test-Path $converter) { Remove-Item $converter -Force; Write-Output 'MoneyConverter.cs removed' }

# Модель изменилась (деньги -> REAL), поэтому миграцию создаём заново
$migrations = Join-Path $proj 'Migrations'
if (Test-Path $migrations) { Remove-Item $migrations -Recurse -Force; Write-Output 'old migrations removed' }

dotnet ef migrations add InitialCreate
Write-Output ("ef_exit=" + $LASTEXITCODE)

$out = Join-Path $proj 'bin\Debug\net10.0-windows'
Get-ChildItem (Join-Path $out 'hotel.db*') -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

dotnet build
Write-Output ("build_exit=" + $LASTEXITCODE)

# Запускаем приложение и смотрим, открылось ли окно входа
$exe = (Get-ChildItem (Join-Path $out '*.exe') | Select-Object -First 1).FullName
$p = Start-Process $exe -PassThru
Start-Sleep -Seconds 7
$p.Refresh()
Write-Output ("alive=" + (-not $p.HasExited))
Write-Output ("title=" + $p.MainWindowTitle)
if (-not $p.HasExited) { $p.Kill() }
Start-Sleep -Seconds 1

# Проверяем схему созданной базы: суммы должны лежать как REAL
$text = ''
foreach ($name in 'hotel.db', 'hotel.db-wal') {
    $f = Join-Path $out $name
    if (Test-Path $f) { $text += [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($f)) }
}
Write-Output ("schema_money_REAL=" + $text.Contains('PricePerNight REAL'))
Write-Output ("schema_has_COLLATE=" + $text.Contains('COLLATE'))
Write-Output ("schema_has_seed=" + $text.Contains('manager'))
