# Helpers for running test sessions of the game without disturbing the desktop.
#   offscreen.ps1 backup   - save Unity's window settings (only if no backup exists yet)
#   offscreen.ps1 windowed - switch Unity's saved display settings to a 1600x900 window (no launch arguments,
#                            so Steam doesn't ask to confirm custom options)
#   offscreen.ps1 hide     - move the game window off-screen without giving it focus
#   offscreen.ps1 restore  - put the saved window settings back (Unity writes them on exit)
param([string]$Action)

$key = 'HKCU\Software\Maracas Studio\CatMailCo'
$backup = Join-Path $PSScriptRoot 'window_settings_backup.reg'

switch ($Action) {
    'backup' {
        if (-not (Test-Path $backup)) { reg export $key $backup /y | Out-Null; 'backed up' } else { 'backup exists' }
    }
    'windowed' {
        $p = 'HKCU:\Software\Maracas Studio\CatMailCo'
        $set = @{ 'Screenmanager Fullscreen mode_h3630240806' = 3; 'Screenmanager Resolution Width_h182942802' = 1600;
                  'Screenmanager Resolution Height_h2627697771' = 900; 'Screenmanager Resolution Use Native_h1405027254' = 0 }
        foreach ($k in $set.Keys) { Set-ItemProperty -Path $p -Name $k -Value $set[$k] -Type DWord }
        'windowed'
    }
    'restore' {
        if (Test-Path $backup) { reg import $backup 2>$null | Out-Null; Remove-Item $backup; 'restored' } else { 'nothing to restore' }
    }
    'hide' {
        Add-Type -Namespace W -Name U -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetWindowPos(System.IntPtr h, System.IntPtr after, int x, int y, int cx, int cy, uint flags);
'@
        for ($i = 0; $i -lt 60; $i++) {
            $p = Get-Process CatMailCo -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
            if ($p) {
                # SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE
                [W.U]::SetWindowPos($p.MainWindowHandle, [IntPtr]::Zero, -4000, 100, 0, 0, 0x0001 -bor 0x0004 -bor 0x0010) | Out-Null
                'hidden'
                return
            }
            Start-Sleep -Milliseconds 500
        }
        'no window'
    }
}
