using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MAUI_Class_Tracker.Models;
using MAUI_Class_Tracker.Services;
using System.Collections.ObjectModel;

public partial class TermListViewModel : ObservableObject
{
    private readonly DataService _dataService;

    public ObservableCollection<Term> Terms { get; } = new();

    public IAsyncRelayCommand LoadTermsCommand { get; }
    public IAsyncRelayCommand<Term> TermSelectedCommand { get; }

    public event Action<Term>? TermSelected;

    public TermListViewModel(DataService dataService)
    {
        _dataService = dataService;

        LoadTermsCommand = new AsyncRelayCommand(LoadTermsAsync);
        TermSelectedCommand = new AsyncRelayCommand<Term>(OnTermSelectedAsync);
    }

    private async Task LoadTermsAsync()
    {
        await _dataService.InitializeAsync();
        var termList = await _dataService.GetTermsAsync();

        Terms.Clear();
        foreach (var term in termList)
            Terms.Add(term);
    }

    private async Task OnTermSelectedAsync(Term? selectedTerm)
    {
        if (selectedTerm == null) return;
        TermSelected?.Invoke(selectedTerm);
    }
}
