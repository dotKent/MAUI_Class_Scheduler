using MAUI_Class_Tracker.Models;
using MAUI_Class_Tracker.Services;
using Microsoft.Maui.Controls;
using Plugin.LocalNotification;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MAUI_Class_Tracker.Views;

[QueryProperty(nameof(TermId), "termId")]
[QueryProperty(nameof(CourseId), "courseId")]
public partial class TermDetailPage : ContentPage
{
    private readonly DataService _dataService;
    public ObservableCollection<Course> Courses { get; set; } = new();

    public Command OpenFlyoutCommand => new Command(() =>
    {
        Shell.Current.FlyoutIsPresented = true;
    });

    private string _title;
    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    private DateTime _startDate;
    public DateTime StartDate
    {
        get => _startDate;
        set { _startDate = value; OnPropertyChanged(); }
    }

    private DateTime _endDate;
    public DateTime EndDate
    {
        get => _endDate;
        set { _endDate = value; OnPropertyChanged(); }
    }
    private Term _selectedTerm;

    private int _termId;
    public int TermId
    {
        get => _termId;
        set
        {
            _termId = value;
            _ = LoadTermAsync(_termId); 
        }
    }
    private int _courseId;
    public int CourseId
    {
        get => _courseId;
        set
        {
            _courseId = value;
            Debug.WriteLine($"[DEBUG] Set CourseId = {_courseId} in TermDetailPage");
        }
    }

    public TermDetailPage(DataService dataService)
    {
        InitializeComponent();
        _dataService = dataService;
        CoursesCollectionView.ItemsSource = Courses;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_termId > 0)
            await LoadTermAsync(_termId);
    }

    private async Task LoadTermAsync(int termId)
    {
        _selectedTerm = await _dataService.GetTermByIdAsync(termId);
        if (_selectedTerm != null)
        {
            Title = _selectedTerm.Title;
            StartDate = _selectedTerm.StartDate;
            EndDate = _selectedTerm.EndDate;

            LoadCourses();
            Debug.WriteLine($"[DEBUG] Loaded Term: Id={_selectedTerm.TermId}, Title={_selectedTerm.Title}");
        }
        else
        {
            Debug.WriteLine($"[ERROR] Failed to load term from DB with Id={termId}");
            await DisplayAlert("Error", "Could not load the selected term from the database.", "OK");
        }
    }

    private async void LoadCourses()
    {
        if (_selectedTerm == null || _selectedTerm.TermId == 0)
            return;

        var courses = await _dataService.GetCoursesByTermIdAsync(_selectedTerm.TermId);

        Courses.Clear();
        foreach (var course in courses)
        {
            Courses.Add(course);
        }

        Debug.WriteLine($"[DEBUG] Loaded {courses.Count} courses for Term Id={_selectedTerm.TermId}");
    }

    public Command<Course> SelectCourseCommand => new Command<Course>(async (course) =>
    {
        if (course != null)
        {
            await Shell.Current.GoToAsync($"{nameof(CourseDetailPage)}?courseId={course.CourseId}&termId={course.TermId}");
        }
    });

    private async void OnCourseTapped(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is Course course)
        {
            await Shell.Current.GoToAsync($"{nameof(CourseDetailPage)}?courseId={course.CourseId}");
        }
    }

    private async void OnAddCourseClicked(object sender, EventArgs e)
    {
        if (_selectedTerm == null || _selectedTerm.TermId == 0)
        {
            await DisplayAlert("Error", "Please save the term first.", "OK");
            return;
        }

        var existingCourses = await _dataService.GetCoursesByTermIdAsync(_selectedTerm.TermId);

        if (CourseId == 0 && existingCourses.Count >= 6)
        {
            await DisplayAlert("Limit Reached", "You cannot add more than 6 courses to a single term.", "OK");
            return;
        }

        string courseTitle = await DisplayPromptAsync(
            CourseId == 0 ? "New Course" : "Edit Course",
            CourseId == 0 ? "Enter a title for the course:" : "Edit the course title:");

        if (string.IsNullOrWhiteSpace(courseTitle))
        {
            await DisplayAlert("Error", "A course title is required.", "OK");
            return;
        }

        if (CourseId == 0 && existingCourses.Any(c =>
                !string.IsNullOrWhiteSpace(c.Title) &&
                c.Title.Trim().Equals(courseTitle.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            await DisplayAlert("Duplicate Course", "A course with that title already exists in this term.", "OK");
            return;
        }

        var startDate = _selectedTerm.StartDate;
        var endDate = _selectedTerm.EndDate.AddMonths(3);

        if (endDate <= startDate)
        {
            await DisplayAlert("Invalid Dates", "Course end date must be after the start date.", "OK");
            return;
        }

        Course courseToSave;
        if (CourseId != 0)
        {
            courseToSave = await _dataService.GetCourseByIdAsync(CourseId) ?? new Course();
            courseToSave.Title = courseTitle.Trim();
            courseToSave.StartDate = startDate;
            courseToSave.EndDate = endDate;
            courseToSave.TermId = _selectedTerm.TermId;
        }
        else
        {
            courseToSave = new Course
            {
                Title = courseTitle.Trim(),
                StartDate = startDate,
                EndDate = endDate,
                TermId = _selectedTerm.TermId
            };
        }

        var success = await _dataService.SaveCourseAsync(courseToSave);
        if (!success)
        {
            await DisplayAlert("Limit Reached", "You cannot add more than 6 courses to a term.", "OK");
            return;
        }

        await DisplayAlert("Success", CourseId == 0 ? "Course saved." : "Course updated.", "OK");

        CourseId = 0; // Reset after save
        LoadCourses();
    }

    private async void OnNotifyCourseClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.CommandParameter is Course course)
        {
            var notification = new NotificationRequest
            {
                Title = "Course Reminder",
                Description = $"{course.Title} has upcoming dates!",
                NotificationId = course.CourseId + 1000,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = DateTime.Today.AddSeconds(1) // test with near future
                }
            };

            if(DateTime.Now > _selectedTerm.StartDate)
            {
            await LocalNotificationCenter.Current.Show(notification);
                await DisplayAlert("Notification Set",
                $"You will be reminded when {_selectedTerm.Title} ends on {_selectedTerm.EndDate:MMM dd, yyyy}.",
                "OK");
            }
            else
            {
                await LocalNotificationCenter.Current.Show(notification);
                await DisplayAlert("Notification Set",
                $"You will be reminded when {_selectedTerm.Title} starts on {_selectedTerm.StartDate:MMM dd, yyyy} and ends on {_selectedTerm.EndDate:MMM dd, yyyy}.",
                "OK");
            }
        }

    }

    private async void OnShareCourseClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.CommandParameter is Course course)
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Text = $"Course: {course.Title}\nDates: {course.StartDate:d} to {course.EndDate:d}",
                Title = $"Share Course Info - {course.Title}"
            });
        }
    }

    private async void OnAddNoteToCourseClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.CommandParameter is Course course)
        {
            string note = await DisplayPromptAsync("Add Note", $"Enter a note for course '{course.Title}':");

            if (!string.IsNullOrWhiteSpace(note))
            {
                course.Notes = note.Trim(); // assumes Course model has a Notes property
                await _dataService.SaveCourseAsync(course);
                LoadCourses(); // refresh the list
                Debug.WriteLine($"[DEBUG] Note added to Course Id={course.CourseId}");
            }
        }
    }

    private async void OnEditCourseClicked(object sender, EventArgs e)
    {
        if (_selectedTerm == null || _selectedTerm.TermId == 0)
        {
            await DisplayAlert("Error", "Please save the term before editing courses.", "OK");
            return;
        }

        if (sender is ImageButton button && button.CommandParameter is Course course)
        {
            if (course.CourseId == 0)
            {
                await DisplayAlert("Error", "Invalid course. Please save the course first.", "OK");
                return;
            }

            var freshCourse = await _dataService.GetCourseByIdAsync(course.CourseId);
            if (freshCourse == null || freshCourse.CourseId == 0)
            {
                await DisplayAlert("Error", "Could not load the course. Please try again.", "OK");
                return;
            }
            await Shell.Current.GoToAsync($"{nameof(CourseDetailPage)}?courseId={course.CourseId}&termId={course.TermId}");

        }
    }

    private async void OnDeleteCourseClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.CommandParameter is Course courseToDelete)
        {
            bool confirm = await DisplayAlert("Delete Course", $"Delete '{courseToDelete.Title}'?", "Yes", "No");
            if (!confirm) return;

            await _dataService.DeleteCourseAsync(courseToDelete);
            Courses.Remove(courseToDelete);
            Debug.WriteLine($"[DEBUG] Deleted Course: {courseToDelete.Title}");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_selectedTerm == null)
        {
            await DisplayAlert("Error", "No Term loaded.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedTerm.Title))
        {
            await DisplayAlert("Validation", "A Term title is required.", "OK");
            return;
        }

        _selectedTerm.StartDate = StartDatePicker.Date;
        _selectedTerm.EndDate = EndDatePicker.Date;
        if (_selectedTerm.EndDate <= _selectedTerm.StartDate)
        {
            await DisplayAlert("Date Error", "The end date must be after the start date.", "OK");
            return;
        }

        await _dataService.SaveTermAsync(_selectedTerm);
        await DisplayAlert("Saved", "Term saved successfully.", "OK");
        await Shell.Current.GoToAsync("..");  // Navigate back
    }
}

