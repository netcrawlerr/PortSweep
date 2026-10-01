using PortSweep.Core.Models;

namespace PortSweep.Core.Services;

public interface ISocketScanner
{
    Task<List<ListeningProcess>> GetListeningProcessesAsync();
}