$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../../scripts/Update-BonfireInteractive.ps1')
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
$reader = [Xml.XmlReader]::Create((Join-Path $PSScriptRoot '../../scripts/UpdateConfirmation.xaml'))
try { $window = [Windows.Markup.XamlReader]::Load($reader) }
finally { $reader.Dispose() }
$window.DataContext = [pscustomobject]@{ Version = 'Версия: тест'; Destination = 'Установка: MONOBLOCK / Bonfire' }
$window.WindowStartupLocation = 'Manual'
$window.Left = -10000
$window.Top = -10000
$window.Show()
try {
    $window.UpdateLayout()
    if ($window.FindName('Approve').IsDefault) { throw 'Подтверждение не должно выбираться по Enter.' }
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new([int]$window.ActualWidth, [int]$window.ActualHeight, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($window)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $outputDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../artifacts/update-dialog'))
    [IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
    $stream = [IO.File]::Create((Join-Path $outputDirectory 'confirmation.png'))
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
}
finally { $window.Close() }
if (Request-BonfireUpdateApproval 'Тест диалога, установка не выполняется' 'Проверка тайм-аута: 1 секунда' 1) { throw 'Тайм-аут ошибочно разрешил установку.' }
Write-Host 'WPF_CONFIRMATION_OK: разметка, кнопка по умолчанию и отказ по тайм-ауту.'
