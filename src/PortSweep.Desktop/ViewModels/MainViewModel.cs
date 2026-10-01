using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PortSweep.Core.Models;
using PortSweep.Core.Services;

namespace PortSweep.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ISocketScanner _scanner;
    private readonly IProcessTerminator _terminator;

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private ProcessItemViewModel? _selectedItem;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private int _totalListenersCount;

    public ObservableCollection<ProcessItemViewModel> AllItems { get; } = new();
    public ObservableCollection<ProcessItemViewModel> FilteredItems { get; } = new();

    public MainViewModel()
    {
        _scanner = new LinuxSocketScanner();
        _terminator = new LinuxProcessTerminator();
        _ = RefreshAsync();
    }

    partial void OnSearchFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsRefreshing = true;
        StatusMessage = "Scanning active listening ports...";

        try
        {
            var processes = await _scanner.GetListeningProcessesAsync();
            
            AllItems.Clear();
            foreach (var proc in processes)
            {
                AllItems.Add(new ProcessItemViewModel(proc));
            }

            TotalListenersCount = AllItems.Count;
            ApplyFilter();
            StatusMessage = $"Updated at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task TerminateSelectedAsync()
    {
        if (SelectedItem == null) return;
        await TerminateInternal(SelectedItem, ProcessSignal.Graceful);
    }

    [RelayCommand]
    public async Task ForceKillSelectedAsync()
    {
        if (SelectedItem == null) return;
        await TerminateInternal(SelectedItem, ProcessSignal.Force);
    }

    private async Task TerminateInternal(ProcessItemViewModel item, ProcessSignal signal)
    {
        StatusMessage = $"Sending {signal} to {item.ProcessName} (PID: {item.Pid})...";
        bool success = await _terminator.TerminateAsync(item.Pid, signal);

        if (success)
        {
            StatusMessage = $"Successfully terminated {item.ProcessName} (PID: {item.Pid})";
            await RefreshAsync();
        }
        else
        {
            StatusMessage = $"Failed to terminate PID {item.Pid}. May require elevated privileges.";
        }
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();
        var query = AllItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            string term = SearchFilter.Trim().ToLowerInvariant();
            query = query.Where(item =>
                item.Port.ToString().Contains(term) ||
                item.ProcessName.ToLowerInvariant().Contains(term) ||
                item.Pid.ToString().Contains(term) ||
                item.Protocol.ToLowerInvariant().Contains(term) ||
                item.WorkingDirectory.ToLowerInvariant().Contains(term));
        }

        foreach (var item in query)
        {
            FilteredItems.Add(item);
        }
    }
}