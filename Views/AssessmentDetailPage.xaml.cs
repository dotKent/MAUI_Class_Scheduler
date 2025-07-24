using System.Diagnostics;
using System.Linq;
using SQLite;
using MAUI_Class_Tracker.Models;
using MAUI_Class_Tracker.Services;
using System.ComponentModel;

namespace MAUI_Class_Tracker.Views;

[QueryProperty(nameof(CourseId), "courseId")]
[QueryProperty(nameof(AssessmentId), "assessmentId")]
[QueryProperty(nameof(Type), "type")]
public partial class AssessmentDetailPage : ContentPage, INotifyPropertyChanged
{
    private readonly DataService _dataService;
    public event PropertyChangedEventHandler? PropertyChanged;
    public int CourseId { get; set; }
    public string Type { get; set; }
    private Assessment _assessment;

    public int AssessmentId { get; set; }

    public Command OpenFlyoutCommand => new Command(() =>
    {
        Shell.Current.FlyoutIsPresented = true;
    });

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public List<string> AssessmentTypes { get; } = new()
{
    "Objective Assessment",
    "Performance Assessment"
};

    private string _selectedType;
    public string SelectedType
    {
        get => _selectedType;
        set
        {
            if (_selectedType != value)
            {
                _selectedType = value;
                OnPropertyChanged();
            }
        }
    }

    public AssessmentDetailPage(DataService dataService)
    {
        InitializeComponent();
        _dataService = dataService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (CourseId == 0)
        {
            await DisplayAlert("Error", "Missing CourseId. Cannot load assessment.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        if (AssessmentId > 0)
        {
            _assessment = await _dataService.GetAssessmentByIdAsync(AssessmentId);
        }
        else
        {
            _assessment = new Assessment
            {
                CourseId = CourseId,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7),
                DueDate = DateTime.Today.AddDays(10),
                Type = Type,
                UserId = Preferences.Get("UserId", 0)
            };
        }

        if (_assessment != null)
        {
            NameEntry.Text = _assessment.Name;
            StartDatePicker.Date = _assessment.StartDate;
            EndDatePicker.Date = _assessment.EndDate;
            DueDatePicker.Date = _assessment.DueDate;
            TypePicker.SelectedItem = _assessment.Type;
        }
       
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_assessment == null)
        {
            await DisplayAlert("Error", "Assessment not found.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(NameEntry.Text) || TypePicker.SelectedItem == null)
        {
            await DisplayAlert("Validation", "Name and Type are required.", "OK");
            return;
        }

        DateTime start = StartDatePicker.Date;
        DateTime end = EndDatePicker.Date;
        DateTime due = DueDatePicker.Date;

        if (end < start)
        {
            await DisplayAlert("Invalid Dates", "End date cannot be before the start date.", "OK");
            return;
        }

        if (due < start || due < end)
        {
            await DisplayAlert("Invalid Dates", "Due date cannot be before start or end date.", "OK");
            return;
        }

        string selectedType = TypePicker.SelectedItem.ToString();

        //Check if another assessment of this type already exists for the course
        var existing = await _dataService.GetAssessmentByTypeAsync(CourseId, selectedType);

        if (existing != null && existing.AssessmentId != _assessment.AssessmentId)
        {
            await DisplayAlert("Validation Error",
                $"A {selectedType} already exists for this course. Only one of each type is allowed.", "OK");
            return;
        }

        _assessment.CourseId = CourseId;
        _assessment.Name = NameEntry.Text.Trim();
        _assessment.Type = TypePicker.SelectedItem.ToString();
        _assessment.StartDate = StartDatePicker.Date;
        _assessment.EndDate = EndDatePicker.Date;
        _assessment.DueDate = DueDatePicker.Date;

        await _dataService.SaveAssessmentAsync(_assessment);

        await DisplayAlert("Success", "Assessment saved.", "OK");
        await Shell.Current.GoToAsync($"{nameof(CourseDetailPage)}?courseId={_assessment.CourseId}");
    }
}
