using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_Class_Tracker.Models
{
    public class ReportItem
    {
        public string Title { get; set; }        
        public string Subtitle { get; set; }  
        public string Detail { get; set; } 
        public DateTime? Date { get; set; }

        public int Id { get; set; }  //UserID
        public int AssessmentId { get; set; }
        public int CourseId { get; set; }
        public int TermId { get; set; }

        public bool HasDate => Date.HasValue && Date.Value > DateTime.MinValue;
    }
}
