namespace PortSweep.Core.Models;

public record ListeningProcess(
    int Pid,
    string ProcessName,
    int Port,
    string Protocol,
    string CommandLine,
    string WorkingDirectory,
    string User,
    long MemoryBytes
)
{
    public string DisplayMemory
    {
        get
        {
            if (MemoryBytes <= 0) return "N/A";
            double mb = MemoryBytes / (1024.0 * 1024.0);
            return $"{mb:N1} MB";
        }
    }

    public string ShortCommandLine => 
        string.IsNullOrWhiteSpace(CommandLine) ? ProcessName : CommandLine;
}