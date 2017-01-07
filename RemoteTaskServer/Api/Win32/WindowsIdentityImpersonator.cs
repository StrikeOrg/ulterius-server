﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace UlteriusServer.Api.Win32
{
    /// Impersonates a windows identity.
    /// Based on: http://msdn.microsoft.com/en-us/library/w070t6ka.aspx
    public class WindowsIdentityImpersonator : IDisposable
    {
        readonly SafeTokenHandle _safeTokenHandle;
        private WindowsImpersonationContext _impersonatedUser;

        public WindowsIdentity Identity { get; private set; }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public WindowsIdentityImpersonator(string domain, string username, string password)
        {
            bool returnValue = LogonUser(username, domain, password, 2, 0, out _safeTokenHandle);

            if (returnValue == false)
            {
                throw new UnauthorizedAccessException("Could not login as " + domain + "\\" + username + ".",
                    new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()));
            }
        }

        public void BeginImpersonate()
        {
            Identity = new WindowsIdentity(_safeTokenHandle.DangerousGetHandle());
            _impersonatedUser = Identity.Impersonate();
        }

        public void EndImpersonate()
        {
            Identity?.Dispose();

            _impersonatedUser?.Dispose();
        }

        public void Dispose()
        {
            EndImpersonate();

            _safeTokenHandle?.Dispose();
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
            int dwLogonType, int dwLogonProvider, out SafeTokenHandle phToken);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);
    }

    public sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeTokenHandle() : base(true)
        {
        }

        [DllImport("kernel32.dll")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);

        protected override bool ReleaseHandle()
        {
            return CloseHandle(handle);
        }
    }
}
