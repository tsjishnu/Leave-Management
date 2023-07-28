using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Leave_Management.Models;

namespace Leave_Management.Controllers
{
    public class LeaveCalendarController : Controller
    {
        private readonly LoginContext db = new LoginContext();

        public ActionResult Index()
        {
            // Get all approved leave requests
            var approvedLeaveRequests = db.LeaveRequests.Where(lr => lr.Status == "Approved").ToList();

            // Group the leave requests by the FromDate to create a calendar view
            var groupedLeaveRequests = approvedLeaveRequests.GroupBy(lr => GetStartOfWeek(lr.FromDate)).ToList();

            return View(groupedLeaveRequests);
        }

        private DateTime GetStartOfWeek(DateTime date)
        {
            // Calculate the start of the week (Sunday) for a given date
            return date.AddDays(-(int)date.DayOfWeek);
        }
    }
}
