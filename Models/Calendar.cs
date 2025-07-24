using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_Class_Tracker.Models
{
    public class Calendar
    {
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Type { get; set; }
        public string DateDisplay { get; set; }

        public List<Term> Terms { get; set; } = new();
        public List<Course> Courses { get; set; } = new();
        public List<Assessment> Assessments { get; set; } = new();
    }
}
