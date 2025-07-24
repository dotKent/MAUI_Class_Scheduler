using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Plugin.LocalNotification;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using MAUI_Class_Tracker.Models;
using MAUI_Class_Tracker.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MAUI_Class_Tracker.Views;

[QueryProperty(nameof(TermId), "termId")]
[QueryProperty(nameof(CourseId), "courseId")] 
[QueryProperty(nameof(AssessmentId), "_assessmentId")]
public partial class CourseDetailPage : ContentPage, INotifyPropertyChanged
{
    private readonly DataService _dataService;
    private Assessment _objectiveAssessment;
    private Assessment _performanceAssessment;
    public ObservableCollection<Assessment> Assessments { get; set; } = new();
    private ObservableCollection<Course> _allCourses = new();
    public ObservableCollection<Course> FilteredCourses { get; set; } = new();

    public Command OpenFlyoutCommand => new Command(() =>
    {
        Shell.Current.FlyoutIsPresented = true;
    });

    private Course _selectedCourse;
    public Course SelectedCourse
    {
        get => _selectedCourse;
        set
        {
            _selectedCourse = value;
            OnPropertyChanged(); 
        }
    }

    private int _termId;
    public int TermId
    {
        get => _termId;
        set
        {
            _termId = value;
            Debug.WriteLine($"[DEBUG] Set TermId = {value} in CourseDetailPage");
        }
    }

    private int _courseId;
    public int CourseId
    {
        get => _courseId;
        set
        {
            _courseId = value;
            Debug.WriteLine($"[DEBUG] Set CourseId = {value} in CourseDetailPage");
        }
    }

    private int _assessmentId;
    public int AssessmentId
    {
        get => _assessmentId;
        set
        {
                _assessmentId = value;
                LoadAssessment(_assessmentId);
        }
    }

    public Assessment ObjectiveAssessment
    {
        get => _objectiveAssessment;
        set
        {
            _objectiveAssessment = value;
            OnPropertyChanged();
        }
    }

    public Assessment PerformanceAssessment
    {
        get => _performanceAssessment;
        set
        {
            _performanceAssessment = value;
            OnPropertyChanged();
        }
    }
    private bool _hasLoaded = false;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_hasLoaded) return;
        _hasLoaded = true;

        if (CourseId != 0)
        {
            SelectedCourse = await _dataService.GetCourseByIdAsync(CourseId) ?? new Course { TermId = TermId };
        }
        else
        {
            SelectedCourse = new Course { TermId = TermId };
        }

        FilteredCourses.Clear();
        FilteredCourses.Add(SelectedCourse);

        Debug.WriteLine($"[DEBUG] Loaded CourseDetailPage with CourseId={CourseId}, TermId={TermId}");
        await LoadAssessmentsAsync();
    }

    public CourseDetailPage(DataService dataService)
    {
        InitializeComponent();
        _dataService = dataService;
        BindingContext = this;
    }

    private async void OnPageLoaded(object sender, EventArgs e)
    {
        if (CourseId != 0)
        {
            var course = await _dataService.GetCourseByIdAsync(CourseId);
            if (course != null)
            {
                // populate UI
            }
        }
    }

    private async Task LoadCourseAsync(int id)
    {
        try
        {
            var course = await _dataService.GetCourseByIdAsync(id);
            if (course == null)
            {
                await DisplayAlert("Error", "Course not found.", "OK");
                return;
            }

            SelectedCourse = course;
            Debug.WriteLine($"[DEBUG] Loaded course title: {SelectedCourse.Title}");
            await LoadAssessmentsAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Failed to load course: {ex.Message}");
            await DisplayAlert("Error", "Could not load course.", "OK");
        }
    }

    private async void LoadCourse(int id)
    {
        var course = await _dataService.GetCourseByIdAsync(id);
        if (course != null)
        {
            SelectedCourse = course;
            LoadAssessmentsAsync();
        }
        else
        {
            Debug.WriteLine($"[ERROR] Could not load course with Id={id}");
        }
    }

    private async Task LoadAssessmentsAsync()
    {
        if (SelectedCourse == null) return;

        var all = await _dataService.GetAssessmentsForCourseAsync(SelectedCourse.CourseId);

        Assessments.Clear();
        foreach (var a in all)
        {
            Assessments.Add(a);
        }

        ObjectiveAssessment = all.FirstOrDefault(a => a.Type == "Objective Assessment");
        PerformanceAssessment = all.FirstOrDefault(a => a.Type == "Performance Assessment"); 
    }

    private async void LoadAssessment(int id)
    {
        var assessment = await _dataService.GetAssessmentByIdAsync(id);
        if (assessment == null)
        {
            Debug.WriteLine($"[ERROR] Assessment ID={id} not found.");
        }
    }

    private void OnCourseSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.Trim() ?? string.Empty;

        var filtered = _allCourses.Where(t =>
            (!string.IsNullOrEmpty(t.Title) && t.Title.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
            (t.StartDate.ToString("d").Contains(query))
        );

        FilteredCourses.Clear();
        foreach (var item in filtered)
            FilteredCourses.Add(item);
    }

    private async void OnAddAssessmentClicked(object sender, EventArgs e)
    {
        if (_selectedCourse == null || _selectedCourse.CourseId == 0)
        {
            await DisplayAlert("Error", "Please save the course before adding assessments.", "OK");
            return;
        }

        var existing = await _dataService.GetAssessmentsForCourseAsync(_selectedCourse.CourseId);

        if (existing.Count >= 2)
        {
            await DisplayAlert("Limit Reached", "Only one Objective and one Performance assessment are allowed per course.", "OK");
            return;
        }

        string type = await DisplayActionSheet("Select Assessment Type", "Cancel", null, "Objective Assessment", "Performance Assessment");

        if (type == "Cancel" || string.IsNullOrWhiteSpace(type))
            return;

        if (existing.Any(a => a.Type?.Equals(type, StringComparison.OrdinalIgnoreCase) == true))
        {
            await DisplayAlert("Duplicate", $"A {type} already exists.", "OK");
            return;
        }

        string name = await DisplayPromptAsync($"{type} Assessment", "Enter a name:");
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("Invalid", "Name is required.", "OK");
            return;
        }

        var startDate = _selectedCourse.StartDate;
        var dueDate = startDate.AddDays(14);
        var endDate = _selectedCourse.EndDate;

        if (endDate <= startDate)
        {
            await DisplayAlert("Invalid Dates", "Assessment end date must be after start date.", "OK");
            return;
        }

        if (dueDate < startDate)
        {
            await DisplayAlert("Invalid Dates", "Due date cannot be before the start date.", "OK");
            return;
        }

        Assessment assessmentToSave;
        if (AssessmentId != 0)
        {
            assessmentToSave = await _dataService.GetAssessmentByIdAsync(AssessmentId) ?? new Assessment();
            assessmentToSave.Name = name.Trim();
            assessmentToSave.Type = type;
            assessmentToSave.StartDate = startDate;
            assessmentToSave.EndDate = endDate;
            assessmentToSave.DueDate = dueDate;
            assessmentToSave.CourseId = _selectedCourse.CourseId;
        }
        else
        {
            assessmentToSave = new Assessment
            {
                Name = name.Trim(),
                Type = type,
                StartDate = startDate,
                EndDate = endDate,
                DueDate = dueDate,
                CourseId = _selectedCourse.CourseId
            };
        }

        var result = await _dataService.SaveAssessmentAsync(assessmentToSave);
        if (result == null || result.AssessmentId == 0)
        {
            await DisplayAlert("Error", "Could not save the assessment.", "OK");
            return;
        }

        await DisplayAlert("Success", $"{type} assessment saved.", "OK");
        await LoadAssessmentsAsync();
    }

    private async void OnEditAssessmentClicked(object sender, EventArgs e)
    {
        if (sender is not ImageButton button || button.CommandParameter is not Assessment assessment)
        {
            Debug.WriteLine("[ERROR] Invalid sender or missing assessment in CommandParameter.");
            return;
        }

        var freshAssessment = await _dataService.GetAssessmentByIdAsync(assessment.AssessmentId);

        if (freshAssessment == null || freshAssessment.AssessmentId == 0)
        {
            Debug.WriteLine("[ERROR] Assessment not found or has invalid ID.");
            await DisplayAlert("Error", "Assessment not found or not saved. Please refresh and try again.", "OK");
            return;
        }

        if (freshAssessment.CourseId == 0)
        {
            Debug.WriteLine("[ERROR] Assessment has no associated CourseId.");
            await DisplayAlert("Error", "Assessment is not linked to a valid course.", "OK");
            return;
        }

        //await Shell.Current.GoToAsync($"{nameof(AssessmentDetailPage)}?assessmentId={freshAssessment.AssessmentId}&courseId={_selectedCourse.CourseId}");
        await Shell.Current.GoToAsync(
            $"{nameof(AssessmentDetailPage)}?" +
            $"assessmentId={freshAssessment.AssessmentId}&" +
            $"courseId={_selectedCourse.CourseId}&" +
            $"type={Uri.EscapeDataString(freshAssessment.Type)}");
    }

    private async void OnDeleteAssessmentClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.CommandParameter is Assessment assessment)
        {
            if (assessment.AssessmentId == 0)
            {
                Debug.WriteLine("[DEBUG] CRASH - No Assessment Id to delete!");
                await DisplayAlert("Error", "Assessment is not yet saved and cannot be deleted.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Delete Assessment",
                $"Are you sure you want to delete '{assessment.Name}'?", "Yes", "No");

            if (!confirm)
                return;

            await _dataService.DeleteAssessmentAsync(assessment);

            LoadAssessmentsAsync();

            Debug.WriteLine($"[DEBUG] Deleted Assessment Id={assessment.AssessmentId}, Name={assessment.Name}");
        }
    }
    private async void OnNotifyAssessmentClicked(object sender, EventArgs e)
    {
        if (sender is not ImageButton button || button.CommandParameter is not Assessment assessment)
        {
            Debug.WriteLine("[ERROR] Invalid sender or missing assessment in CommandParameter.");
            return;
        }

        // Validate: Start Date cannot be after End Date
        if (assessment.StartDate > assessment.EndDate)
        {
            await DisplayAlert("Invalid Dates", "Start Date cannot be after End Date.", "OK");
            return;
        }

        string choice = await DisplayActionSheet(
            "Set Notification", "Cancel", null,
            "Start Date", "End Date");

        if (choice == "Cancel" || string.IsNullOrEmpty(choice))
            return;

        DateTime notifyDate;
        string label;

        if (choice == "Start Date")
        {
            notifyDate = assessment.StartDate.Date;
            label = $"Reminder: {assessment.Type} Assessment Starts";
        }
        else if (choice == "End Date")
        {
            notifyDate = assessment.EndDate.Date;
            label = $"Reminder: {assessment.Type} Assessment Ends";
        }
        else
        {
            Debug.WriteLine("[DEBUG] Invalid choice.");
            return;
        }

        // Validate: Don't allow notifications for past dates
        if (notifyDate <= DateTime.Now)
        {
            await DisplayAlert("Too Late", $"The {choice.ToLower()} has already passed. Notification not scheduled.", "OK");
            Debug.WriteLine($"[DEBUG] Attempted to schedule {choice} notification for a past date.");
            return;
        }

        var notification = new NotificationRequest
        {
            Title = label,
            Description = $"{assessment.Name} {choice.ToLower()}s on {notifyDate:MMM dd, yyyy}.",
            NotificationId = assessment.AssessmentId + (choice == "Start Date" ? 3000 : 4000),
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = notifyDate
            }
        };

        LocalNotificationCenter.Current.Show(notification);

        await DisplayAlert("Notification Scheduled",
            $"{choice} notification set for {assessment.Name} on {notifyDate:MMM dd, yyyy}.",
            "OK");

        Debug.WriteLine($"[DEBUG] {choice} notification scheduled for '{assessment.Name}' on {notifyDate:MM/dd/yyyy}");
    }


    private async void OnShareNotesClicked(object sender, EventArgs e)
    {
        if (_selectedCourse == null)
        {
            await DisplayAlert("Error", "No course is currently loaded.", "OK");
            return;
        }

        var notes = _selectedCourse.Notes?.Trim();

        if (string.IsNullOrEmpty(notes))
        {
            await DisplayAlert("No Notes", "There are no notes to share for this course.", "OK");
            return;
        }

        try
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Text = notes,
                Title = $"Share Notes - {_selectedCourse.Title}"
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Failed to share notes: {ex.Message}");
            await DisplayAlert("Share Failed", "Unable to share notes at this time.", "OK");
        }
    }

    public List<string> StatusOptions { get; } = new()
    {
        "In Progress",
        "Completed",
        "Dropped",
        "Plan to Take"
    };

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (SelectedCourse == null)
        {
            await DisplayAlert("Error", "No course loaded.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedCourse.Title))
        {
            await DisplayAlert("Validation", "A course title is required.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedCourse.Status))
        {
            await DisplayAlert("Validation", "A course status is required.", "OK");
            return;
        }

        bool hasInstructorInfo =
        !string.IsNullOrWhiteSpace(SelectedCourse.InstructorName) ||
        !string.IsNullOrWhiteSpace(SelectedCourse.InstructorPhone) ||
        !string.IsNullOrWhiteSpace(SelectedCourse.InstructorEmail);

        if (hasInstructorInfo)
        {
            // Name
            if (string.IsNullOrWhiteSpace(SelectedCourse.InstructorName))
            {
                await DisplayAlert("Validation", "Instructor name is required if you're adding instructor info.", "OK");
                return;
            }

            // Phone (format and validate only if provided)
            string rawPhone = SelectedCourse.InstructorPhone?.Trim();
            if (string.IsNullOrWhiteSpace(rawPhone))
            {
                await DisplayAlert("Validation", "Instructor phone number is required if you're adding instructor info.", "OK");
                return;
            }

            string digitsOnly = Regex.Replace(rawPhone, @"\D", "");
            if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
                digitsOnly = digitsOnly.Substring(1);

            if (digitsOnly.Length != 10)
            {
                await DisplayAlert("Validation", "Phone number must be a valid 10-digit US number.", "OK");
                return;
            }

            SelectedCourse.InstructorPhone = $"({digitsOnly.Substring(0, 3)}) {digitsOnly.Substring(3, 3)}-{digitsOnly.Substring(6, 4)}";

            // Email
            string email = SelectedCourse.InstructorEmail?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                await DisplayAlert("Validation", "Instructor email is required if you're adding instructor info.", "OK");
                return;
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                await DisplayAlert("Validation", "Invalid email address format.", "OK");
                return;
            }
        }

        if (SelectedCourse.EndDate < SelectedCourse.StartDate)
        {
            await DisplayAlert("Validation", "End date must be after the start date. You may need to reset the notifications.", "OK");
            return;
        }

        SelectedCourse.TermId = TermId;

        await _dataService.SaveCourseAsync(SelectedCourse);
        await DisplayAlert("Saved", "Course saved successfully.", "OK");
        await Shell.Current.GoToAsync($"{nameof(TermDetailPage)}?termId={SelectedCourse.TermId}");
    }

    public new event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));

}


