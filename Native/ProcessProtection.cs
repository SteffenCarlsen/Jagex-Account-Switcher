#region

using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using JagexAccountSwitcher.Helpers;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

#endregion

/// <summary>
///     Provides functionality to protect the current process from external manipulation.
/// </summary>
public static class ProcessProtection
{
    #region Public Methods

    /// <summary>
    ///     Restricts access to the current process by applying security settings.
    ///     Requires administrator privileges on Windows.
    /// </summary>
    public static void RestrictProcessAccess()
    {
#if WINDOWS
        if (!ProcessHelper.IsRunningAsAdmin())
        {
            MessageBoxManager.GetMessageBoxStandard(
                "Administrator Privileges Required",
                "Process blocking requires administrator privileges. Please restart the application as administrator.",
                ButtonEnum.Ok, Icon.Warning).ShowAsync();
            return;
        }

        const uint TOKEN_ADJUST_DEFAULT = 0x0080;
        const uint TOKEN_QUERY = 0x0008;

        if (OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_DEFAULT | TOKEN_QUERY, out var token))
        {
            try
            {
                ApplyHighIntegrityLevel(token);
                ApplyRestrictiveSecurityDescriptor();
                ApplyJobObjectRestrictions();
            }
            finally
            {
                CloseHandle(token);
            }
        }
#endif
    }

    #endregion

    #region Private Methods

#if WINDOWS
    private static void ApplyHighIntegrityLevel(IntPtr token)
    {
        // Set high integrity level
        var highIntegritySid = new SecurityIdentifier(WellKnownSidType.WinHighLabelSid, null);
        var sidBytes = new byte[highIntegritySid.BinaryLength];
        highIntegritySid.GetBinaryForm(sidBytes, 0);

        var sidPtr = Marshal.AllocHGlobal(sidBytes.Length);
        try
        {
            Marshal.Copy(sidBytes, 0, sidPtr, sidBytes.Length);

            var tml = new TOKEN_MANDATORY_LABEL
            {
                Label = new SID_AND_ATTRIBUTES
                {
                    Sid = sidPtr,
                    Attributes = 0x20 // SE_GROUP_INTEGRITY
                }
            };

            SetTokenInformation(
                token,
                TOKEN_INFORMATION_CLASS.TokenIntegrityLevel,
                ref tml,
                (uint)Marshal.SizeOf(tml) + (uint)sidBytes.Length
            );
        }
        finally
        {
            Marshal.FreeHGlobal(sidPtr);
        }
    }

    private static void ApplyRestrictiveSecurityDescriptor()
    {
        var pSD = SetupRestrictiveSecurityDescriptor();
        if (pSD != IntPtr.Zero)
        {
            try
            {
                SetProcessSecurityDescriptor(GetCurrentProcess(), pSD);
            }
            finally
            {
                Marshal.FreeHGlobal(pSD);
            }
        }
    }

    private static void ApplyJobObjectRestrictions()
    {
        const uint JOB_OBJECT_LIMIT_QUERY = 0x00001000;

        // Create a job object
        var hJob = CreateJobObject(IntPtr.Zero, null);
        if (hJob != IntPtr.Zero)
        {
            // Set job object limits that prevent process handle queries
            var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JOB_OBJECT_LIMIT_QUERY
            };

            var infoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(info));
            try
            {
                Marshal.StructureToPtr(info, infoPtr, false);

                SetInformationJobObject(
                    hJob,
                    JOBOBJECTINFOCLASS.JobObjectBasicLimitInformation,
                    infoPtr,
                    (uint)Marshal.SizeOf(info)
                );

                AssignProcessToJobObject(hJob, GetCurrentProcess());
            }
            finally
            {
                Marshal.FreeHGlobal(infoPtr);
            }
        }
    }

    private static IntPtr SetupRestrictiveSecurityDescriptor()
    {
        const int SECURITY_DESCRIPTOR_REVISION = 1;

        // Allocate and initialize security descriptor
        var pSD = Marshal.AllocHGlobal(0x100);
        if (InitializeSecurityDescriptor(pSD, SECURITY_DESCRIPTOR_REVISION))
        {
            // Setting a NULL DACL essentially denies all access
            if (SetSecurityDescriptorDacl(pSD, true, IntPtr.Zero, false))
            {
                return pSD;
            }
        }

        Marshal.FreeHGlobal(pSD);
        return IntPtr.Zero;
    }

    private static bool SetProcessSecurityDescriptor(IntPtr processHandle, IntPtr pSecurityDescriptor)
    {
        // Extract DACL from security descriptor
        GetSecurityDescriptorDacl(pSecurityDescriptor, out var daclPresent, out var pDacl, out var daclDefaulted);

        // Set security info on process
        var result = SetSecurityInfo(
            processHandle,
            SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
            SECURITY_INFORMATION.DACL_SECURITY_INFORMATION,
            IntPtr.Zero, // No owner change
            IntPtr.Zero, // No group change
            pDacl, // DACL
            IntPtr.Zero // No SACL
        );

        return result == 0; // 0 indicates success
    }
#endif

    #endregion

    #region Native Imports and Structs

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(
        IntPtr ProcessHandle,
        uint DesiredAccess,
        out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool SetTokenInformation(
        IntPtr TokenHandle,
        TOKEN_INFORMATION_CLASS TokenInformationClass,
        ref TOKEN_MANDATORY_LABEL TokenInformation,
        uint TokenInformationLength);

    private enum TOKEN_INFORMATION_CLASS
    {
        TokenIntegrityLevel = 25
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_MANDATORY_LABEL
    {
        public SID_AND_ATTRIBUTES Label;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SID_AND_ATTRIBUTES
    {
        public IntPtr Sid;
        public uint Attributes;
    }

#if WINDOWS
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool InitializeSecurityDescriptor(IntPtr pSecurityDescriptor, uint dwRevision);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool SetSecurityDescriptorDacl(IntPtr pSecurityDescriptor, bool bDaclPresent,
        IntPtr pDacl, bool bDaclDefaulted);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetSecurityDescriptorDacl(
        IntPtr pSecurityDescriptor,
        out bool bDaclPresent,
        out IntPtr pDacl,
        out bool bDaclDefaulted);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern uint SetSecurityInfo(
        IntPtr handle,
        SE_OBJECT_TYPE ObjectType,
        SECURITY_INFORMATION SecurityInfo,
        IntPtr psidOwner,
        IntPtr psidGroup,
        IntPtr pDacl,
        IntPtr pSacl);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(IntPtr hJob, JOBOBJECTINFOCLASS JobObjectInfoClass,
        IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    private enum JOBOBJECTINFOCLASS
    {
        JobObjectBasicLimitInformation = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public IntPtr MinimumWorkingSetSize;
        public IntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public IntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [Flags]
    private enum SECURITY_INFORMATION : uint
    {
        OWNER_SECURITY_INFORMATION = 0x00000001,
        GROUP_SECURITY_INFORMATION = 0x00000002,
        DACL_SECURITY_INFORMATION = 0x00000004,
        SACL_SECURITY_INFORMATION = 0x00000008
    }

    private enum SE_OBJECT_TYPE
    {
        SE_UNKNOWN_OBJECT_TYPE = 0,
        SE_FILE_OBJECT,
        SE_SERVICE,
        SE_PRINTER,
        SE_REGISTRY_KEY,
        SE_LMSHARE,
        SE_KERNEL_OBJECT,
        SE_WINDOW_OBJECT,
        SE_DS_OBJECT,
        SE_DS_OBJECT_ALL,
        SE_PROVIDER_DEFINED_OBJECT,
        SE_WMIGUID_OBJECT,
        SE_REGISTRY_WOW64_32KEY
    }
#endif

    #endregion
}