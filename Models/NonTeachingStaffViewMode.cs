using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Leave_Management.Models
{
    public class NonTeachingStaffViewModel
    {
        public List<LeaveRequest> ApprovedLeaveRequests { get; set; }
        public Dictionary<string, List<LeaveTypeDetail>> LeaveTypeDetails { get; set; }
        public Dictionary<string, string> EmployeeFullNames { get; set; }
    }

    public class LeaveRequests
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Type { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class LeaveTypeDetail
    {
        public string LeaveType { get; set; }
        public int TotalDaysTaken { get; set; }
    }
}