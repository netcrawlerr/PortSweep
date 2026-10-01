using System.Diagnostics;
using System.Text.RegularExpressions;
using PortSweep.Core.Helpers;
using PortSweep.Core.Models;

namespace PortSweep.Core.Services;

public class LinuxSocketScanner : ISocketScanner
{
    // Regex matches users:(("process_name",pid=1234,fd=4))
    private static readonly Regex UsersRegex = new(
        @"users:\(\(\""(?<name>[^\""]+)\"",pid=(?<pid>\d+)",
        RegexOptions.Compiled
    );

    public async Task<List<ListeningProcess>> GetListeningProcessesAsync()
    {
        return await Task.Run(() =>
        {
            var results = new List<ListeningProcess>();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ss",
                    Arguments = "-H -tlpn -ulpn",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return results;

                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var line in lines)
                {
                    var parsed = ParseSsLine(line);
                    if (parsed != null)
                    {
                        results.Add(parsed);
                    }
                }
            }
            catch
            {
                // Fallback for non-ss Linux environments if needed
            }

            return results
                .DistinctBy(p => (p.Pid, p.Port, p.Protocol))
                .OrderBy(p => p.Port)
                .ToList();
        });
    }

    private static ListeningProcess? ParseSsLine(string line)
    {
        try
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5) return null;

            string proto = parts[0].ToUpperInvariant();
            string localAddr = parts[4];

            int lastColon = localAddr.LastIndexOf(':');
            if (lastColon == -1 || !int.TryParse(localAddr[(lastColon + 1)..], out int port))
                return null;

            var match = UsersRegex.Match(line);
            if (!match.Success) return null;

            string procName = match.Groups["name"].Value;
            if (!int.TryParse(match.Groups["pid"].Value, out int pid))
                return null;

            string cmdline = ProcParser.ReadCmdLine(pid);
            string cwd = ProcParser.ReadCwd(pid);
            long memory = ProcParser.ReadMemoryBytes(pid);
            string user = ProcParser.ReadUser(pid);

            return new ListeningProcess(
                Pid: pid,
                ProcessName: procName,
                Port: port,
                Protocol: proto,
                CommandLine: string.IsNullOrWhiteSpace(cmdline) ? procName : cmdline,
                WorkingDirectory: cwd,
                User: user,
                MemoryBytes: memory
            );
        }
        catch
        {
            return null;
        }
    }
}