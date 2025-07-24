using MAUI_Class_Tracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_Class_Tracker.ViewModels
{
    public class CalendarTermViewModel
    {
        public string Title => Term.Title;
        public DateTime StartDate => Term.StartDate;
        public DateTime EndDate => Term.EndDate;

        public Term Term { get; set; }
        public List<CalendarCourseViewModel> Courses { get; set; } = new();
    }
}
