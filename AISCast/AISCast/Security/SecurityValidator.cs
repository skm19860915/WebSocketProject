using AISCast.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AISCast.Security
{
    public static class SecurityValidator
    {
        private static List<WhitelistEntry> _whitelistedIPs = new List<WhitelistEntry>();
        private static List<WhitelistEntry> _whitelistedUIDs = new List<WhitelistEntry>();

        public static int UidTimeout { get; set; }

        public static bool IsWhitelistedIP(string ipAddress, out WhitelistEntry whitelistEntry)
        {
            whitelistEntry = _whitelistedIPs.FirstOrDefault(wip => wip.Id.Equals(ipAddress));

            return whitelistEntry != null;
        }

        public static bool IsWhitelistedUID(string uid, out WhitelistEntry whitelistEntry)
        {
            whitelistEntry = _whitelistedUIDs.FirstOrDefault(wuid => wuid.Id.Equals(uid));

            return whitelistEntry != null;
        }

        internal static void SetSecurity(List<WhitelistEntry> whitelistEntries, int uidTimeout)
        {
            foreach(var whitelistEntry in whitelistEntries)
            {
                switch(whitelistEntry.EntryType)
                {
                    case WhitelistEntryType.IPAddress:
                        _whitelistedIPs.Add(whitelistEntry);
                        break;
                    case WhitelistEntryType.UID:
                        _whitelistedUIDs.Add(whitelistEntry);
                        break;
                    default:
                        throw new Exception("Invalid whitelist entry in configuration");
                }
            }

            UidTimeout = uidTimeout;
        }
    }
}
