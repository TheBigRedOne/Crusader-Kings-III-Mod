using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

internal static class Ck3AchievementEnabler
{
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_VM_OPERATION = 0x0008;
    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_VM_WRITE = 0x0020;
    private const uint THREAD_SUSPEND_RESUME = 0x0002;
    private const uint PAGE_EXECUTE_READWRITE = 0x40;

    private const string ExpectedExeSha256 =
        "2D00FF3101EF70B566F2FCBAE292F09263199C80E9DC8F139B82D7D96F83DB86";

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(
        uint processAccess,
        bool inheritHandle,
        int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenThread(
        uint desiredAccess,
        bool inheritHandle,
        int threadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SuspendThread(IntPtr thread);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(IntPtr thread);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(
        IntPtr process,
        UIntPtr baseAddress,
        [Out] byte[] buffer,
        UIntPtr size,
        out UIntPtr bytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(
        IntPtr process,
        UIntPtr baseAddress,
        byte[] buffer,
        UIntPtr size,
        out UIntPtr bytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualProtectEx(
        IntPtr process,
        UIntPtr address,
        UIntPtr size,
        uint newProtect,
        out uint oldProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FlushInstructionCache(
        IntPtr process,
        UIntPtr baseAddress,
        UIntPtr size);

    private sealed class Patch
    {
        public string Name;
        public ulong Rva;
        public byte[] Original;
        public byte[] Replacement;
        public bool AlreadyApplied;
        public bool ChangedThisRun;
    }

    private static readonly Patch[] Patches =
    {
        new Patch
        {
            Name = "Persist eligible state",
            Rva = 0xAA669C,
            Original = Bytes("84 D2 0F 94 C0"),
            Replacement = Bytes("31 C0 90 90 90")
        },
        new Patch
        {
            Name = "Achievement event eligibility (blocked state)",
            Rva = 0xA9C3CA,
            Original = Bytes("32 DB"),
            Replacement = Bytes("B3 01")
        },
        new Patch
        {
            Name = "Achievement event eligibility (checksum state)",
            Rva = 0xA9C3EA,
            Original = Bytes("32 DB"),
            Replacement = Bytes("B3 01")
        },
        new Patch
        {
            Name = "CK3 CanGetAchievements result",
            Rva = 0xA0E2D1,
            Original = Bytes("32 C0"),
            Replacement = Bytes("B0 01")
        },
        new Patch
        {
            Name = "Jomini achievement availability result",
            Rva = 0x32A95C7,
            Original = Bytes("32 C0"),
            Replacement = Bytes("B0 01")
        },
        new Patch
        {
            Name = "Save-header achievement eligibility",
            Rva = 0x259F3EE,
            Original = Bytes("40 32 FF"),
            Replacement = Bytes("40 B7 01")
        }
    };

    private static readonly List<string> LogLines = new List<string>();

    private static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Log("CK3 Achievement Enabler v1.0");
        Log("Target: CK3 1.19.0.6 / Steam build 23530548");
        Log("Changes 6 verified instruction sites (16 bytes) in the current CK3 process.");
        Log("Does not change ck3.exe or existing saves, and does not call Steam unlock APIs.");
        Log("");

        int exitCode = 1;
        Process game = null;
        IntPtr processHandle = IntPtr.Zero;
        List<IntPtr> suspendedThreads = new List<IntPtr>();

        try
        {
            game = FindGameProcess();
            if (game == null)
            {
                throw new InvalidOperationException("No running ck3.exe process was found.");
            }

            ProcessModule mainModule = game.MainModule;
            string modulePath = mainModule.FileName;
            ulong moduleBase = unchecked((ulong)mainModule.BaseAddress.ToInt64());
            Log("Found PID " + game.Id + ": " + modulePath);

            string actualHash = ComputeSha256(modulePath);
            Log("ck3.exe SHA-256: " + actualHash);
            if (!string.Equals(
                    actualHash,
                    ExpectedExeSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "ck3.exe version mismatch; no memory changes were made.");
            }

            processHandle = OpenProcess(
                PROCESS_QUERY_INFORMATION |
                PROCESS_VM_OPERATION |
                PROCESS_VM_READ |
                PROCESS_VM_WRITE,
                false,
                game.Id);
            if (processHandle == IntPtr.Zero)
            {
                throw Win32("Could not open the CK3 process");
            }

            VerifyAllPatches(processHandle, moduleBase);
            Log("Executable hash and all original instruction bytes verified.");

            SuspendAllThreads(game, suspendedThreads);
            Log("Suspended " + suspendedThreads.Count + " CK3 threads.");

            VerifyAllPatches(processHandle, moduleBase);
            ApplyAllPatches(processHandle, moduleBase);
            Log("Applied all 6 patch sites.");

            VerifyReplacements(processHandle, moduleBase);
            Log("Read-back verification passed; the patch is active in this CK3 process.");
            exitCode = 0;
        }
        catch (Exception error)
        {
            Log("ERROR: " + error.Message);
            if (processHandle != IntPtr.Zero && game != null)
            {
                try
                {
                    RollBackChanges(
                        processHandle,
                        unchecked((ulong)game.MainModule.BaseAddress.ToInt64()));
                }
                catch (Exception rollbackError)
                {
                    Log("Rollback error: " + rollbackError.Message);
                }
            }
        }
        finally
        {
            ResumeAndCloseThreads(suspendedThreads);
            if (suspendedThreads.Count > 0)
            {
                Log("CK3 threads resumed.");
            }
            if (processHandle != IntPtr.Zero)
            {
                CloseHandle(processHandle);
            }

            string reportPath = WriteReport();
            Log("Report: " + reportPath);
            if (exitCode == 0)
            {
                Log("");
                Log("Save once after toggling debug mode, then check the new save in the load menu.");
            }
            Log("Press Enter to close. The patch remains active until CK3 exits.");
            Console.ReadLine();
        }

        return exitCode;
    }

    private static Process FindGameProcess()
    {
        Process[] games = Process.GetProcessesByName("ck3");
        if (games.Length == 0)
        {
            return null;
        }
        if (games.Length != 1)
        {
            throw new InvalidOperationException(
                "Multiple ck3.exe processes were found; aborting to avoid selecting the wrong one.");
        }
        return games[0];
    }

    private static void VerifyAllPatches(IntPtr process, ulong moduleBase)
    {
        foreach (Patch patch in Patches)
        {
            byte[] actual = ReadExact(
                process,
                moduleBase + patch.Rva,
                patch.Original.Length);
            if (Equal(actual, patch.Original))
            {
                patch.AlreadyApplied = false;
            }
            else if (Equal(actual, patch.Replacement))
            {
                patch.AlreadyApplied = true;
            }
            else
            {
                throw new InvalidOperationException(
                    patch.Name +
                    " bytes do not match at RVA 0x" +
                    patch.Rva.ToString("X") +
                    " (actual: " +
                    Hex(actual) +
                    "); no new changes were made.");
            }
        }
    }

    private static void VerifyReplacements(IntPtr process, ulong moduleBase)
    {
        foreach (Patch patch in Patches)
        {
            byte[] actual = ReadExact(
                process,
                moduleBase + patch.Rva,
                patch.Replacement.Length);
            if (!Equal(actual, patch.Replacement))
            {
                throw new InvalidOperationException(
                    patch.Name + " failed read-back verification.");
            }
        }
    }

    private static void ApplyAllPatches(IntPtr process, ulong moduleBase)
    {
        foreach (Patch patch in Patches)
        {
            if (patch.AlreadyApplied)
            {
                Log(patch.Name + " is already patched; skipping.");
                continue;
            }

            patch.ChangedThisRun = true;
            WriteCode(
                process,
                moduleBase + patch.Rva,
                patch.Replacement);
            Log(
                "Patched " +
                patch.Name +
                " (RVA 0x" +
                patch.Rva.ToString("X") +
                ").");
        }
    }

    private static void RollBackChanges(IntPtr process, ulong moduleBase)
    {
        bool any = false;
        for (int index = Patches.Length - 1; index >= 0; index--)
        {
            Patch patch = Patches[index];
            if (!patch.ChangedThisRun)
            {
                continue;
            }
            WriteCode(process, moduleBase + patch.Rva, patch.Original);
            patch.ChangedThisRun = false;
            any = true;
        }
        if (any)
        {
            Log("Rolled back every instruction changed by this run.");
        }
    }

    private static void SuspendAllThreads(
        Process game,
        List<IntPtr> suspendedThreads)
    {
        foreach (ProcessThread thread in game.Threads)
        {
            IntPtr handle = OpenThread(
                THREAD_SUSPEND_RESUME,
                false,
                thread.Id);
            if (handle == IntPtr.Zero)
            {
                throw Win32(
                    "Could not open CK3 thread " + thread.Id + "; aborting");
            }

            uint previousCount = SuspendThread(handle);
            if (previousCount == uint.MaxValue)
            {
                int error = Marshal.GetLastWin32Error();
                CloseHandle(handle);
                throw new Win32Exception(
                    error,
                    "Could not suspend CK3 thread " + thread.Id + "; aborting");
            }
            suspendedThreads.Add(handle);
        }
    }

    private static void ResumeAndCloseThreads(List<IntPtr> threads)
    {
        for (int index = threads.Count - 1; index >= 0; index--)
        {
            IntPtr thread = threads[index];
            ResumeThread(thread);
            CloseHandle(thread);
        }
    }

    private static byte[] ReadExact(
        IntPtr process,
        ulong address,
        int length)
    {
        byte[] result = new byte[length];
        UIntPtr bytesRead;
        bool ok = ReadProcessMemory(
            process,
            new UIntPtr(address),
            result,
            new UIntPtr((uint)length),
            out bytesRead);
        if (!ok || bytesRead.ToUInt64() != (ulong)length)
        {
            throw Win32(
                "Could not read CK3 memory at address 0x" + address.ToString("X"));
        }
        return result;
    }

    private static void WriteCode(
        IntPtr process,
        ulong address,
        byte[] bytes)
    {
        uint oldProtect;
        if (!VirtualProtectEx(
                process,
                new UIntPtr(address),
                new UIntPtr((uint)bytes.Length),
                PAGE_EXECUTE_READWRITE,
                out oldProtect))
        {
            throw Win32(
                "Could not change code-page protection at address 0x" +
                address.ToString("X"));
        }

        bool writeOk = false;
        try
        {
            UIntPtr bytesWritten;
            writeOk = WriteProcessMemory(
                process,
                new UIntPtr(address),
                bytes,
                new UIntPtr((uint)bytes.Length),
                out bytesWritten);
            if (!writeOk || bytesWritten.ToUInt64() != (ulong)bytes.Length)
            {
                throw Win32(
                    "Could not write CK3 code at address 0x" +
                    address.ToString("X"));
            }
            if (!FlushInstructionCache(
                    process,
                    new UIntPtr(address),
                    new UIntPtr((uint)bytes.Length)))
            {
                throw Win32(
                    "Could not flush the instruction cache at address 0x" +
                    address.ToString("X"));
            }
        }
        finally
        {
            uint ignored;
            if (!VirtualProtectEx(
                    process,
                    new UIntPtr(address),
                    new UIntPtr((uint)bytes.Length),
                    oldProtect,
                    out ignored) &&
                writeOk)
            {
                throw Win32(
                    "Could not restore code-page protection at address 0x" +
                    address.ToString("X"));
            }
        }
    }

    private static string ComputeSha256(string path)
    {
        using (FileStream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete))
        using (SHA256 algorithm = SHA256.Create())
        {
            return Hex(algorithm.ComputeHash(stream)).Replace(" ", "");
        }
    }

    private static byte[] Bytes(string text)
    {
        string[] parts = text.Split(
            new[] { ' ' },
            StringSplitOptions.RemoveEmptyEntries);
        byte[] result = new byte[parts.Length];
        for (int index = 0; index < parts.Length; index++)
        {
            result[index] = Convert.ToByte(parts[index], 16);
        }
        return result;
    }

    private static bool Equal(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }
        for (int index = 0; index < left.Length; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }
        return true;
    }

    private static string Hex(byte[] bytes)
    {
        StringBuilder text = new StringBuilder(bytes.Length * 3);
        for (int index = 0; index < bytes.Length; index++)
        {
            if (index > 0)
            {
                text.Append(' ');
            }
            text.Append(bytes[index].ToString("X2"));
        }
        return text.ToString();
    }

    private static Exception Win32(string message)
    {
        return new Win32Exception(
            Marshal.GetLastWin32Error(),
            message);
    }

    private static void Log(string message)
    {
        Console.WriteLine(message);
        LogLines.Add(message);
    }

    private static string WriteReport()
    {
        string path = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "CK3AchievementEnabler-report.txt");
        File.WriteAllLines(
            path,
            LogLines.ToArray(),
            new UTF8Encoding(false));
        return path;
    }
}
