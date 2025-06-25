using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Manager.Party.Party;
using MelonLoader;

public static class PrefixRet
{
    public const bool BLOCK_ORIGINAL = false;
    public const bool RUN_ORIGINAL = true;

    public static bool Run(Action action) => RUN_ORIGINAL.Also(_ => action());
    public static bool Block(Action action) => BLOCK_ORIGINAL.Also(_ => action());
}

public enum FutariCmd
{
    // Control plane
    CTL_START = 1,
    CTL_HEARTBEAT = 3,
    CTL_TCP_CONNECT = 4,  // Accept a new multiplexed TCP stream
    CTL_TCP_ACCEPT = 5,
    CTL_TCP_CLOSE = 7,

    // Data plane
    DATA_SEND = 21,
    DATA_BROADCAST = 22,
}

public class RecruitRecord
{
    public RecruitInfo RecruitInfo;
    public string Keychip;
}

public class ServerInfo
{
    public string relayHost;
    public int relayPort;
}

public class OnlineUserInfo
{
    public int totalUsers;
    public int activeRecruits;
}

public struct FutariMsg
{
    public FutariCmd FutariCmd;
    public ProtocolType? proto;
    public int? sid;
    public uint? src;
    public int? sPort;
    public uint? dst;
    public int? dPort;
    public string? data;

    public override string ToString()
    {
        int? proto_ = proto == null ? null : (int) proto;
        var arr = new object[] {
            1, (int) FutariCmd, proto_, sid, src, sPort, dst, dPort, 
            null, null, null, null, null, null, null, null,  // reserved for future use
            data
        };
        
        // Map nulls to empty strings
        return string.Join(",", arr.Select(x => x ?? "")).TrimEnd(',');
    }
    
    private static T? Parse<T>(string[] fields, int i) where T : struct 
        => fields.Length <= i || fields[i] == "" ? null
            : (T) Convert.ChangeType(fields[i], typeof(T));
    

    public static FutariMsg FromString(string str)
    {
        var fields = str.Split(',');
        return new FutariMsg
        {
            FutariCmd = (FutariCmd) (Parse<int>(fields, 1) ?? throw new InvalidOperationException("cmd is required")),
            proto = Parse<int>(fields, 2)?.Let(it => (ProtocolType) it),
            sid = Parse<int>(fields, 3),
            src = Parse<uint>(fields, 4),
            sPort = Parse<int>(fields, 5),
            dst = Parse<uint>(fields, 6),
            dPort = Parse<int>(fields, 7),
            data = string.Join(",", fields.Skip(16))
        };
    }

    public string ToReadableString()
    {
        var parts = new List<string> { FutariCmd.ToString() };

        if (proto.HasValue) parts.Add(proto.ToString());
        if (sid.HasValue) parts.Add($"Stream: {sid}");
        if (src.HasValue) parts.Add($"Src: {src?.ToIP()}:{sPort}");
        if (dst.HasValue) parts.Add($"Dst: {dst?.ToIP()}:{dPort}");
        if (!string.IsNullOrEmpty(data))
        {
            try { parts.Add(Encoding.UTF8.GetString(data.B64())); }
            catch { parts.Add(data); }
        }
    
        return string.Join(" | ", parts);
    }
}

namespace HarmonyLib
{
    public static class Log
    {
        private static readonly object _lock = new object();

#if UNITY_2018_1_OR_NEWER
        // Unity cannot display ANSI colors, use empty strings
        public const string BLACK = "";
        public const string RED = "";
        public const string GREEN = "";
        public const string YELLOW = "";
        public const string BLUE = "";
        public const string MAGENTA = "";
        public const string CYAN = "";
        public const string WHITE = "";
        public const string BRIGHT_BLACK = "";
        public const string BRIGHT_RED = "";
        public const string BRIGHT_GREEN = "";
        public const string BRIGHT_YELLOW = "";
        public const string BRIGHT_BLUE = "";
        public const string BRIGHT_MAGENTA = "";
        public const string BRIGHT_CYAN = "";
        public const string BRIGHT_WHITE = "";
        public const string RESET = "";
        public const string PREFIX = "[FUTARI] ";
#else
        // Text colors
        public const string BLACK = "\u001b[30m";
        public const string RED = "\u001b[31m";
        public const string GREEN = "\u001b[32m";
        public const string YELLOW = "\u001b[33m";
        public const string BLUE = "\u001b[34m";
        public const string MAGENTA = "\u001b[35m";
        public const string CYAN = "\u001b[36m";
        public const string WHITE = "\u001b[37m";

        // Bright text colors
        public const string BRIGHT_BLACK = "\u001b[90m";
        public const string BRIGHT_RED = "\u001b[91m";
        public const string BRIGHT_GREEN = "\u001b[92m";
        public const string BRIGHT_YELLOW = "\u001b[93m";
        public const string BRIGHT_BLUE = "\u001b[94m";
        public const string BRIGHT_MAGENTA = "\u001b[95m";
        public const string BRIGHT_CYAN = "\u001b[96m";
        public const string BRIGHT_WHITE = "\u001b[97m";

        // Reset
        public const string RESET = "\u001b[0m";
        public const string PREFIX = "";
#endif

        // Remove all non-printable characters
        private static string Norm(this string msg) =>
            string.IsNullOrEmpty(msg) ? msg : new string(msg.Where(ch => !char.IsControl(ch)).ToArray());

        public static void Error(string msg)
        {
            lock (_lock)
            {
                MelonLogger.Error($"{PREFIX}{RED}ERROR {RESET}{msg.Norm()}{RESET}");
            }
        }

        public static void Warn(string msg)
        {
            lock (_lock)
            {
                MelonLogger.Warning($"{PREFIX}{YELLOW}WARN  {RESET}{msg.Norm()}{RESET}");
            }
        }

        public static void Debug(string msg)
        {
            if (!Futari.Debug) return;
            lock (_lock)
            {
                MelonLogger.Msg($"{PREFIX}{CYAN}DEBUG {RESET}{msg.Norm()}{RESET}");
            }
        }

        public static void Info(string msg)
        {
            if (!Futari.Debug) return;
            lock (_lock)
            {
                if (msg.StartsWith("A001")) msg = MAGENTA + msg;
                if (msg.StartsWith("A002")) msg = CYAN + msg;
                MelonLogger.Msg($"{PREFIX}{GREEN}INFO  {RESET}{msg.Norm()}{RESET}");
            }
        }
    }
}
