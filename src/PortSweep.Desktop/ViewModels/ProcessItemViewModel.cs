using CommunityToolkit.Mvvm.ComponentModel;
using PortSweep.Core.Models;

namespace PortSweep.Desktop.ViewModels;

public partial class ProcessItemViewModel : ObservableObject
{
    public ListeningProcess Model { get; }

    public int Port => Model.Port;
    public string Protocol => Model.Protocol;
    public int Pid => Model.Pid;
    public string ProcessName => Model.ProcessName;
    public string User => Model.User;
    public string DisplayMemory => Model.DisplayMemory;
    public string WorkingDirectory => Model.WorkingDirectory;
    public string CommandLine => Model.CommandLine;

    [ObservableProperty]
    private bool _isSelected;

    public ProcessItemViewModel(ListeningProcess model)
    {
        Model = model;
    }
}