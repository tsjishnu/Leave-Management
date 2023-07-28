using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Leave_Management.Models.ViewModels
{
    public class TeachingStaffViewModel
    {
        // Your properties for the ViewModel, such as ApprovedLeaveRequests
        public List<LeaveRequest> ApprovedLeaveRequests { get; set; }
        public Dictionary<string, string> EmployeeFullNames { get; set; }
        public Dictionary<string, int> LeaveTypeCounts { get; set; }
        public Dictionary<string, LeaveTypeDetails> LeaveTypeDetails { get; set; }
    }
    public class LeaveTypeDetails
    {
        public int TotalDaysTaken { get; set; }
        public int RequestCount { get; set; }
    }
}
