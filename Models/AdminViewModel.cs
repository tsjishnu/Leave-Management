using System.Collections.Generic;

namespace Leave_Management.Models.ViewModels
{
    public class AdminViewModel
    {
        public List<LeaveRequest> ApprovedLeaveRequests { get; set; }
        public Dictionary<string, string> EmployeeFullNames { get; set; }
        // Add any other properties that are needed for the Admin view here
    }
}
