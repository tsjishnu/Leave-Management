using Leave_Management.Models;
using System.Collections.Generic;

namespace Leave_Management.Models.ViewModels
{
    public class EmployeeDetailsViewModel
    {
        public Employee Employee { get; set; }
        public List<LeaveRequest> ApprovedLeaves { get; set; }
        public List<TypeCount> LeaveTypesCount { get; set; }
    }

    public class TypeCount
    {
        public string Type { get; set; }
        public int Count { get; set; }
        public int TotalDays { get; set; }
    }

}
