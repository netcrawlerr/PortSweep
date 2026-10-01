using System.Diagnostics;
using PortSweep.Core.Models;

namespace PortSweep.Core.Services;

public class LinuxProcessTerminator : IProcessTerminator
{
    public async Task<bool> TerminateAsync(int pid, ProcessSignal signal)
    {
        return await Task.Run(() =>
        {
            try
            {
                int sigNum = (int)signal;

                // standard POSIX kill first
                var psi = new ProcessStartInfo
                {
                    FileName = "kill",
                    Arguments = $"-{sigNum} {pid}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit();
                    if (proc.ExitCode == 0) return true;
                }

                // If process is owned by root, try pkexec / sudo fallback
                var sudoPsi = new ProcessStartInfo
                {
                    FileName = "pkexec",
                    Arguments = $"kill -{sigNum} {pid}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var sudoProc = Process.Start(sudoPsi);
                if (sudoProc != null)
                {
                    sudoProc.WaitForExit();
                    return sudoProc.ExitCode == 0;
                }

                return false;
            }
            catch
            {
                return false;
            }
        });
    }
}