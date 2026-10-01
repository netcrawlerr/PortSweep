using System.Diagnostics;

namespace PortSweep.Core.Helpers;

public static class ProcParser
{
    public static string ReadCmdLine(int pid)
    {
        try
        {
            string path = $"/proc/{pid}/cmdline";
            if (!File.Exists(path)) return string.Empty;

            string content = File.ReadAllText(path);
            return content.Replace('\0', ' ').Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string ReadCwd(int pid)
    {
        try
        {
            string path = $"/proc/{pid}/cwd";
            if (!Directory.Exists(path)) return "Unknown";

            var target = new DirectoryInfo(path).LinkTarget;
            return !string.IsNullOrEmpty(target) ? target : path;
        }
        catch
        {
            return "Access Denied";
        }
    }

    public static long ReadMemoryBytes(int pid)
    {
        try
        {
            using var proc = Process.GetProcessById(pid);
            return proc.WorkingSet64;
        }
        catch
        {
            return 0;
        }
    }

    public static string ReadUser(int pid)
    {
        try
        {
            string path = $"/proc/{pid}/status";
            if (!File.Exists(path)) return Environment.UserName;

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith("Uid:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && parts[1].Trim().StartsWith("0"))
                        return "root";
                }
            }
        }
        catch { }

        return Environment.UserName;
    }
}