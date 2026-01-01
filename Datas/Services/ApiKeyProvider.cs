using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Datas.Services
{
    public static class ApiKeyProvider
    {
        // Read API key from environment variable and return as SecureString.
        // Caller should avoid converting to managed string when possible.
        public static SecureString GetSecureApiKey(string envVarName)
        {
            var key = Environment.GetEnvironmentVariable(envVarName);
            if (string.IsNullOrEmpty(key)) return null;

            var ss = new SecureString();
            foreach (var c in key)
                ss.AppendChar(c);
            ss.MakeReadOnly();

            return ss;
        }

        // Alternative: return char[] so caller can zero it after use.
        public static char[] GetApiKeyChars(string envVarName)
        {
            var key = Environment.GetEnvironmentVariable(envVarName);
            if (string.IsNullOrEmpty(key)) return null;
            return key.ToCharArray();
        }

        // Convert SecureString to normal string (use only when necessary).
        public static string SecureStringToString(SecureString value)
        {
            if (value == null) return null;
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                if (valuePtr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}
