using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MultiBloxManager
{
    public static class NativeMethods
    {
        #region Constants

        public const uint PROCESS_DUP_HANDLE = 0x0040;
        public const uint PROCESS_QUERY_INFORMATION = 0x0400;
        public const uint PROCESS_SET_INFORMATION = 0x0200;

        public const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
        public const uint DUPLICATE_SAME_ACCESS = 0x00000002;

        public const int SystemHandleInformation = 16;
        public const int SystemExtendedHandleInformation = 64;

        public const int ObjectNameInformation = 1;
        public const int ObjectTypeInformation = 2;

        public const int STATUS_SUCCESS = 0x00000000;
        public const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);
        public const int STATUS_INVALID_INFO_CLASS = unchecked((int)0xC0000003);

        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
        {
            public IntPtr Object;
            public UIntPtr UniqueProcessId;
            public UIntPtr HandleValue;
            public uint GrantedAccess;
            public ushort CreatorBackTraceIndex;
            public ushort ObjectTypeIndex;
            public uint HandleAttributes;
            public uint Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_HANDLE_TABLE_ENTRY_INFO
        {
            public ushort UniqueProcessId;
            public ushort CreatorBackTraceIndex;
            public byte ObjectTypeIndex;
            public byte HandleAttributes;
            public ushort HandleValue;
            public IntPtr Object;
            public uint GrantedAccess;
        }

        #endregion

        #region P/Invoke Signatures

        [DllImport("ntdll.dll")]
        public static extern int NtQuerySystemInformation(
            int SystemInformationClass,
            IntPtr SystemInformation,
            int SystemInformationLength,
            out int ReturnLength);

        [DllImport("ntdll.dll")]
        public static extern int NtQueryObject(
            IntPtr ObjectHandle,
            int ObjectInformationClass,
            IntPtr ObjectInformation,
            int ObjectInformationLength,
            out int ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            uint processAccess,
            bool bInheritHandle,
            int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DuplicateHandle(
            IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle,
            IntPtr hTargetProcessHandle,
            out IntPtr lpTargetHandle,
            uint dwDesiredAccess,
            bool bInheritHandle,
            uint dwOptions);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attr,
            ref int attrValue,
            int attrSize);

        #endregion

        #region Helper Methods

        /// <summary>
        /// Enables immersive dark mode for the specified window handle.
        /// </summary>
        public static void EnableImmersiveDarkMode(IntPtr handle)
        {
            try
            {
                int darkMode = 1;
                _ = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            }
            catch
            {
                // Ignored on legacy Windows versions
            }
        }

        /// <summary>
        /// Scans the target Roblox process for the single-instance mutex event ('ROBLOX_singletonEvent')
        /// and closes it using native NT/Win32 APIs.
        /// </summary>
        public static bool UnlockRobloxSingleton(int processId, out string resultMessage)
        {
            IntPtr hProcess = OpenProcess(PROCESS_DUP_HANDLE | PROCESS_QUERY_INFORMATION, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                resultMessage = $"OpenProcess failed (Win32 Error: {err}). Insufficient privileges or process terminated.";
                return false;
            }

            try
            {
                // Query system handles prioritizing 64-bit extended information
                List<UIntPtr> targetHandles = GetProcessHandles(processId);
                if (targetHandles.Count == 0)
                {
                    resultMessage = "No open handles found for target process.";
                    return false;
                }

                IntPtr currentProcess = GetCurrentProcess();

                foreach (UIntPtr rawHandle in targetHandles)
                {
                    IntPtr sourceHandle = (IntPtr)(long)rawHandle.ToUInt64();
                    IntPtr duplicatedHandle = IntPtr.Zero;

                    bool dupSuccess = DuplicateHandle(
                        hProcess,
                        sourceHandle,
                        currentProcess,
                        out duplicatedHandle,
                        0,
                        false,
                        DUPLICATE_SAME_ACCESS);

                    if (!dupSuccess || duplicatedHandle == IntPtr.Zero)
                    {
                        continue;
                    }

                    try
                    {
                        // Deadlock prevention: Query ObjectType first.
                        // Synchronous pipes or files hang if queried with ObjectNameInformation.
                        // Events and Mutants never hang.
                        string? typeName = GetObjectTypeName(duplicatedHandle);
                        if (typeName == null || (!typeName.Equals("Event", StringComparison.OrdinalIgnoreCase) &&
                                                 !typeName.Equals("Mutant", StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        // Inspect the object name
                        string? objectName = GetObjectName(duplicatedHandle);
                        if (objectName != null && objectName.IndexOf("ROBLOX_singletonEvent", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            // Target mutex found! Close it in the Roblox process using DUPLICATE_CLOSE_SOURCE
                            bool closed = DuplicateHandle(
                                hProcess,
                                sourceHandle,
                                IntPtr.Zero,
                                out _,
                                0,
                                false,
                                DUPLICATE_CLOSE_SOURCE);

                            if (closed)
                            {
                                resultMessage = $"Successfully unlocked ROBLOX_singletonEvent (Handle: 0x{rawHandle.ToUInt64():X}).";
                                return true;
                            }
                            else
                            {
                                int closeErr = Marshal.GetLastWin32Error();
                                resultMessage = $"Found singleton handle but closure failed (Win32 Error: {closeErr}).";
                                return false;
                            }
                        }
                    }
                    finally
                    {
                        CloseHandle(duplicatedHandle);
                    }
                }

                resultMessage = "Singleton event handle was not found in process handle table.";
                return false;
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        /// <summary>
        /// Retrieves handle values for a specific process ID using NtQuerySystemInformation.
        /// Handles 64-bit PIDs via SystemExtendedHandleInformation (class 64) with fallback to class 16.
        /// </summary>
        private static List<UIntPtr> GetProcessHandles(int targetPid)
        {
            var results = new List<UIntPtr>();

            // Try SystemExtendedHandleInformation (Class 64)
            if (TryGetHandlesExtended(targetPid, results))
            {
                return results;
            }

            // Fallback: SystemHandleInformation (Class 16)
            TryGetHandlesStandard(targetPid, results);
            return results;
        }

        private static bool TryGetHandlesExtended(int targetPid, List<UIntPtr> results)
        {
            int bufferSize = 0x200000; // 2 MB initial buffer
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

            try
            {
                int returnLength = 0;
                int status = NtQuerySystemInformation(SystemExtendedHandleInformation, buffer, bufferSize, out returnLength);

                while (status == STATUS_INFO_LENGTH_MISMATCH)
                {
                    Marshal.FreeHGlobal(buffer);
                    bufferSize = Math.Max(bufferSize * 2, returnLength + 0x10000);
                    if (bufferSize > 0x8000000) // 128 MB max safety cap
                    {
                        return false;
                    }
                    buffer = Marshal.AllocHGlobal(bufferSize);
                    status = NtQuerySystemInformation(SystemExtendedHandleInformation, buffer, bufferSize, out returnLength);
                }

                if (status != STATUS_SUCCESS)
                {
                    return false;
                }

                ulong handleCount = (ulong)Marshal.ReadIntPtr(buffer);
                int entrySize = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
                IntPtr entryPtr = IntPtr.Add(buffer, IntPtr.Size * 2); // Skip NumberOfHandles + Reserved

                ulong targetPidUlong = (ulong)targetPid;

                for (ulong i = 0; i < handleCount; i++)
                {
                    var entry = Marshal.PtrToStructure<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(entryPtr);
                    if (entry.UniqueProcessId.ToUInt64() == targetPidUlong)
                    {
                        results.Add(entry.HandleValue);
                    }
                    entryPtr = IntPtr.Add(entryPtr, entrySize);
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static bool TryGetHandlesStandard(int targetPid, List<UIntPtr> results)
        {
            int bufferSize = 0x200000;
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

            try
            {
                int returnLength = 0;
                int status = NtQuerySystemInformation(SystemHandleInformation, buffer, bufferSize, out returnLength);

                while (status == STATUS_INFO_LENGTH_MISMATCH)
                {
                    Marshal.FreeHGlobal(buffer);
                    bufferSize = Math.Max(bufferSize * 2, returnLength + 0x10000);
                    if (bufferSize > 0x8000000)
                    {
                        return false;
                    }
                    buffer = Marshal.AllocHGlobal(bufferSize);
                    status = NtQuerySystemInformation(SystemHandleInformation, buffer, bufferSize, out returnLength);
                }

                if (status != STATUS_SUCCESS)
                {
                    return false;
                }

                int handleCount = Marshal.ReadInt32(buffer);
                int entrySize = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO>();
                IntPtr entryPtr = IntPtr.Add(buffer, 4); // 4 bytes handle count

                ushort targetPidUshort = (ushort)(targetPid & 0xFFFF);

                for (int i = 0; i < handleCount; i++)
                {
                    var entry = Marshal.PtrToStructure<SYSTEM_HANDLE_TABLE_ENTRY_INFO>(entryPtr);
                    if (entry.UniqueProcessId == targetPidUshort)
                    {
                        results.Add(new UIntPtr(entry.HandleValue));
                    }
                    entryPtr = IntPtr.Add(entryPtr, entrySize);
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static string? GetObjectTypeName(IntPtr handle)
        {
            int bufferLength = 0x1000;
            IntPtr buffer = Marshal.AllocHGlobal(bufferLength);

            try
            {
                int status = NtQueryObject(handle, ObjectTypeInformation, buffer, bufferLength, out _);
                if (status == STATUS_SUCCESS)
                {
                    var uniStr = Marshal.PtrToStructure<UNICODE_STRING>(buffer);
                    if (uniStr.Length > 0 && uniStr.Buffer != IntPtr.Zero)
                    {
                        return Marshal.PtrToStringUni(uniStr.Buffer, uniStr.Length / 2);
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static string? GetObjectName(IntPtr handle)
        {
            int bufferLength = 0x2000;
            IntPtr buffer = Marshal.AllocHGlobal(bufferLength);

            try
            {
                int status = NtQueryObject(handle, ObjectNameInformation, buffer, bufferLength, out _);
                if (status == STATUS_SUCCESS)
                {
                    var uniStr = Marshal.PtrToStructure<UNICODE_STRING>(buffer);
                    if (uniStr.Length > 0 && uniStr.Buffer != IntPtr.Zero)
                    {
                        return Marshal.PtrToStringUni(uniStr.Buffer, uniStr.Length / 2);
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        #endregion
    }
}
