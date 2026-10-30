$ErrorActionPreference = 'Stop'
$workflow = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../../.github/workflows/deploy-monoblock.yml') -Raw -Encoding UTF8
$lines = $workflow -split '\r?\n'
$scripts = [Collections.Generic.List[string]]::new()
for ($index = 0; $index -lt $lines.Length; $index++) {
    if ($lines[$index] -notmatch '^        run: (.*)$') { continue }
    if ($Matches[1] -ne '|') { $scripts.Add($Matches[1]); continue }
    $body = [Collections.Generic.List[string]]::new()
    for ($next = $index + 1; $next -lt $lines.Length -and $lines[$next].StartsWith('          '); $next++) {
        $body.Add($lines[$next].Substring(10))
    }
    $scripts.Add(($body -join "`n"))
}
if ($scripts.Count -eq 0) { throw 'Не найдены команды workflow.' }
foreach ($codepage in @(1251, 1252)) {
    foreach ($source in $scripts) {
        # Runner пишет UTF-8 без BOM; Windows PowerShell 5.1 читает такой файл в ANSI.
        $decoded = [Text.Encoding]::GetEncoding($codepage).GetString([Text.Encoding]::UTF8.GetBytes($source))
        $errors = $null
        $tokens = $null
        [Management.Automation.Language.Parser]::ParseInput($decoded, [ref]$tokens, [ref]$errors) | Out-Null
        if ($errors.Count -gt 0) { throw "Workflow не разбирается в CP${codepage}: $($errors[0].Message)" }
    }
}
Write-Host "WORKFLOW_ENCODING_OK: $($scripts.Count) команд, CP1251 и CP1252."
