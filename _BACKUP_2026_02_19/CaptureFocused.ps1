param($OutPath)
Add-Type @"
  using System;
  using System.Runtime.InteropServices;
  using System.Text;
  public class User32 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
  }
"@

function Get-ActiveWindowTitle {
  $hwnd = [User32]::GetForegroundWindow()
  $sb = New-Object System.Text.StringBuilder 256
  [User32]::GetWindowText($hwnd, $sb, 256) | Out-Null
  return $sb.ToString()
}

$proc = Get-Process | Where-Object { $_.MainWindowTitle -like "*Unity*" } | Select-Object -First 1
if (-not $proc) {
  Write-Error "Unity process not found!"
  exit 1
}

$attempts = 0
$maxAttempts = 10
while ($attempts -lt $maxAttempts) {
  # Force Focus
  if ([User32]::IsIconic($proc.MainWindowHandle)) {
    [User32]::ShowWindow($proc.MainWindowHandle, 9) # Restore
  }
  [User32]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
  $null = [User32]::ShowWindow($proc.MainWindowHandle, 3) # Maximize
    
  Start-Sleep -Milliseconds 1000
    
  $activeTitle = Get-ActiveWindowTitle
  if ($activeTitle -like "*Unity*") {
    Write-Host "Success: Unity window focused ($activeTitle)"
    break
  }
    
  Write-Warning "Attempt $attempts : Active window is '$activeTitle'. Retrying focus..."
  $attempts++
}

if ($activeTitle -notlike "*Unity*") {
  Write-Error "Failed to focus Unity window after $maxAttempts attempts. Active: $activeTitle"
  # Proceed anyway to capture *something* but warn
}

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
$s = [System.Windows.Forms.Screen]::PrimaryScreen
$b = New-Object System.Drawing.Bitmap $s.Bounds.Width, $s.Bounds.Height
$g = [System.Drawing.Graphics]::FromImage($b)
$g.CopyFromScreen($s.Bounds.X, $s.Bounds.Y, 0, 0, $b.Size)
$b.Save($OutPath)
$g.Dispose()
$b.Dispose()
