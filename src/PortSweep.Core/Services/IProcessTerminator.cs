using PortSweep.Core.Models;

namespace PortSweep.Core.Services;

public interface IProcessTerminator
{
    Task<bool> TerminateAsync(int pid, ProcessSignal signal);
}