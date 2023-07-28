using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using Leave_Management.Models;
using Leave_Management.Models.ViewModels;

namespace Leave_Management.Controllers
{
    public class NonTeachingStaffController : Controller
    {
        private LoginContext db;
        public NonTeachingStaffController()
        {
            db = new LoginContext(); // Initialize the LoginContext here
        }
        // GET: NonTeachinStaff
        public ActionResult Index()
        {

            // Get the current date
            DateTime currentDate = DateTime.Now.Date;
            string currentEmployeeEmail = Session["Email"] as string;

            // Fetch approved leave requests with FromDate greater than or equal to the current date
            List<LeaveRequest> approvedLeaveRequests = db.LeaveRequests
                .Where(lr => lr.Status == "Approved" && lr.FromDate >= currentDate && lr.Email == currentEmployeeEmail)
                .ToList();


            // Fetch all employees
            var employees = db.Employees.ToDictionary(e => e.Email, e => e.FullName);

            // Calculate leave type details for each employee
            var leaveTypeDetails = approvedLeaveRequests
                .GroupBy(lr => lr.Type)
                .ToDictionary(
                    group => group.Key,
                    group => new LeaveTypeDetails
                    {
                        TotalDaysTaken = group.Sum(lr => (lr.ToDate - lr.FromDate).Days + 1),
                        RequestCount = group.Count()
                    });

            // Create the TeachingStaffViewModel and assign the approved leave requests, employee full names, and leave type details
            var viewModel = new TeachingStaffViewModel
            {
                ApprovedLeaveRequests = approvedLeaveRequests,
                EmployeeFullNames = employees,
                LeaveTypeDetails = leaveTypeDetails
            };

            return View(viewModel);
        }

        public ActionResult RequestLeave()
        {
            ViewBag.LeaveTypes = GetLeaveTypes();
            return View();
        }

        [HttpPost]
        public ActionResult RequestLeave(Leave model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.LeaveTypes = GetLeaveTypes();
                return View(model);
            }
            string selectedLeaveType = model.Type;
            if (model.Type == "Casual Leave")
            {
                return RedirectToAction("CasualLeave", new { selectedLeaveType });
            }
            else if (model.Type == "Maternity Leave")
            {
                return RedirectToAction("MaternityLeave", new { selectedLeaveType });
            }
            else if (model.Type == "Vacation Leave")
            {
                return RedirectToAction("VacationLeave", new { selectedLeaveType });
            }
            else if (model.Type == "Medical Leave")
            {
                return RedirectToAction("MedicalLeave", new { selectedLeaveType });
            }
            else
            {
                ViewBag.LeaveTypes = GetLeaveTypes();
                return View(model);
            }
        }
        private SelectList GetLeaveTypes()
        {
            var leaveTypes = db.LeaveType.Select(l => l.Type);
            return new SelectList(leaveTypes);
        }
        public ActionResult CasualLeave(String selectedLeaveType)
        {
            ViewBag.SelectedLeaveType = selectedLeaveType;
            return View();
        }

        [HttpPost]
        public ActionResult CasualLeave(LeaveRequest model)
        {
            if (ModelState.IsValid)
            {
                string email = (string)Session["Email"];
                var leaveType = db.LeaveType.FirstOrDefault(l => l.Type == "CasualLeave");
                if (leaveType != null)
                {
                    if (model.FromDate < DateTime.Now)
                    {
                        ViewBag.ErrorMessage = "You cannot apply for leave for the this day.";
                        return View(model);
                    }
                    else if (model.FromDate == DateTime.Now)
                    {
                        if (DateTime.Now.Hour < 9 && DateTime.Now.Minute < 20)
                        {
                            ViewBag.ErrorMessage = "You cannot apply for leave for the current day after 9:20 AM.";
                            return View(model);
                        }
                    }
                    var casualLeave = db.CasualLeaveTable.Where(r => r.Email == email && r.Date.Year == DateTime.Now.Year);
                    if (casualLeave.Count() >= leaveType.MaxDayNTS)
                    {
                        ViewBag.ErrorMessage = "You have already applied for casual leave more than the maximum allowed limit.";
                        return View(model);
                    }
                }
                try
                {
                    model.Email = email;
                    string type = "Casual Leave";
                    model.Type = type;
                    model.ToDate = model.FromDate;
                    db.LeaveRequests.Add(model);
                    db.SaveChanges();

                    string body = $"A Casual leave request has been submitted. Details:\n\n" +
                         $"Email: {model.Email}\n" +
                         $"From: {model.FromDate}\n" +
                         $"Reason: {model.Reason}\n";

                    SendLeaveRequestEmail(model, body);

                    return RedirectToAction("ConfirmLeaveRequest", "NonTeachingStaff");
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "An error occurred while saving the leave request.";
                }
            }
            return View(model);
        }
        private void SendLeaveRequestEmail(LeaveRequest model, string body)
        {
            using (LoginContext db = new LoginContext())
            {
                string adminEmail = db.AdminTable.FirstOrDefault()?.Email;
                if (adminEmail != null)
                {
                    string senderEmail = "tsjishnu200@gmail.com";
                    string senderPassword = "gbzedyajitbosnqu"; // Replace with the actual sender email password

                    MailMessage mail = new MailMessage(senderEmail, adminEmail);
                    mail.Subject = "Leave Request";
                    mail.Body = body;

                    SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new System.Net.NetworkCredential(senderEmail, senderPassword);
                    smtpClient.EnableSsl = true;
                    smtpClient.Send(mail);
                }
                else
                {
                    // Handle the case where admin email is not found
                    ViewBag.ErrorMessage = "Admin email not found. Leave request notification could not be sent.";
                }
            }
        }
        public ActionResult MaternityLeave(string selectedLeaveType)
        {
            ViewBag.SelectedLeaveType = selectedLeaveType;
            return View();
        }
        [HttpPost]
        public ActionResult MaternityLeave(LeaveRequest model)
        {
            if (ModelState.IsValid)
            {
                string email = (string)Session["Email"];
                var leaveType = db.LeaveType.FirstOrDefault(l => l.Type == "Maternity Leave");
                if (leaveType != null)
                {
                    // Check if the user has already applied for maternity leave within the past 180 dayS
                    var maternityLeaves = db.LeaveRequests.Where(l => l.Email == email && l.Type == "Maternity Leave" && l.FromDate.Year == DateTime.Now.Year);
                    TimeSpan totalMaternityLeaveDuration = TimeSpan.Zero;
                    foreach (var leave in maternityLeaves)
                    {
                        TimeSpan leaveDuration = leave.ToDate - leave.FromDate;
                        totalMaternityLeaveDuration += leaveDuration;
                    }
                    if (totalMaternityLeaveDuration.TotalDays >= leaveType.MaxDayNTS)
                    {
                        ViewBag.ErrorMessage = "You have already applied for maternity leave for the maximum allowed days within the past 180 days.";
                        return View(model);
                    }
                }

                try
                { 
                    model.Email = email;
                    string type = "Maternity Leave";
                    model.Type = type;
                    model.Reason = "Maternity Leave";
                    // Fetch MaxDayTS for Maternity Leave from Leave table
                    var maternityLeaveType = db.LeaveType.FirstOrDefault(l => l.Type == "Maternity Leave");
                    int maxDayNTS = maternityLeaveType?.MaxDayNTS ?? 0;
                    // Calculate the ToDate by adding MaxDayTS days to the FromDate
                    model.ToDate = model.FromDate.AddDays(maxDayNTS);

                    db.LeaveRequests.Add(model);
                    db.SaveChanges();

                    string body = $"A Maternity leave request has been submitted. Details:\n\n" +
                                  $"Email: {email}\n" +
                                  $"From: {model.FromDate}\n" +
                                  $"To: {model.ToDate}\n";

                    SendLeaveRequestEmail(model, body); // Send email to admin

                    return RedirectToAction("ConfirmLeaveRequest", "NonTeachingStaff");
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "An error occurred while saving the leave request.";
                }
            }
            return View(model);
        }
        public ActionResult ConfirmLeaveRequest()
        {
            return View();
        }
        public ActionResult VacationLeave(String selectedLeaveType)
        {
            ViewBag.SelectedLeaveType = selectedLeaveType;
            return View();
        }

        [HttpPost]
        public ActionResult VacationLeave(LeaveRequest model)
        {
            if (ModelState.IsValid)
            {
                var leaveType = db.LeaveType.FirstOrDefault(l => l.Type == "VacationLeave");
                if (leaveType != null)
                {
                    if (model.FromDate < DateTime.Now)
                    {
                        ViewBag.ErrorMessage = "You cannot apply for leave for this day";
                        return View(model);
                    }
                    else if (model.FromDate == DateTime.Now)
                    {
                        if (DateTime.Now.Hour < 9 && DateTime.Now.Minute < 20)
                        {
                            ViewBag.ErrorMessage = "You cannot apply for leave for the current day after 9:20 AM.";
                            return View(model);
                        }
                    }
                    if(model.FromDate>model.ToDate)
                    {
                        ViewBag.ErrorMessage = "Check From date and To date";
                        return View(model);

                    }

                    string email = (string)Session["Email"];
                    // Check if the user has already applied for maternity leave within the past 180 dayS
                    var vacationLeaves = db.LeaveRequests.Where(l => l.Email == email && l.Type == "Vacation Leave" && l.FromDate.Year == DateTime.Now.Year);
                    TimeSpan totalvacationLeaveDuration = TimeSpan.Zero;
                    foreach (var leave in vacationLeaves)
                    {
                        TimeSpan leaveDuration = leave.ToDate - leave.FromDate;
                        totalvacationLeaveDuration += leaveDuration;
                    }
                    if (totalvacationLeaveDuration.TotalDays >= leaveType.MaxDayTS)
                    {
                        ViewBag.ErrorMessage = "You have already applied for maternity leave for the maximum allowed days.";
                        return View(model);
                    }
                    try
                    {
                        model.Email = email;
                        string type = "Vacation Leave";
                        model.Type = type;
                        model.Reason = "VacationLeave";
                        db.LeaveRequests.Add(model);
                        db.SaveChanges();
                        string body = $"A Vacation leave request has been submitted. Details:\n\n" +
                          $"Email: {model.Email}\n" +
                          $"From: {model.FromDate}\n" +
                          $"To: {model.ToDate}\n";

                        SendLeaveRequestEmail(model, body);

                        return RedirectToAction("ConfirmLeaveRequest", "NonTeachingStaff");
                    }
                    catch (Exception ex)
                    {
                        ViewBag.ErrorMessage = "An error occurred while saving the leave request.";
                    }
                }
            }
            return View(model);
        }
        public ActionResult MedicalLeave(string selectedLeaveType)
        {
            ViewBag.SelectedLeaveType = selectedLeaveType;
            return View();
        }

        [HttpPost]
        public ActionResult MedicalLeave(LeaveRequest model)
        {
            if (ModelState.IsValid)
            {
                if (model.FromDate < DateTime.Now)
                {
                    ViewBag.ErrorMessage = "You cannot apply for leave for this day";
                    return View(model);
                }
                else if (model.FromDate == DateTime.Now)
                {
                    if (DateTime.Now.Hour < 9 && DateTime.Now.Minute < 20)
                    {
                        ViewBag.ErrorMessage = "You cannot apply for leave for the current day after 9:20 AM.";
                        return View(model);
                    }
                }
                if (model.FromDate > model.ToDate)
                {
                    ViewBag.ErrorMessage = "Check From date and To date";
                    return View(model);

                }

                string email = (string)Session["Email"];

                // Check if the user has already applied for duty leave on the same day
                var existingLeave = db.LeaveRequests.FirstOrDefault(l => l.Email == email && l.Type == "Medical Leave" && DbFunctions.TruncateTime(l.FromDate) == DbFunctions.TruncateTime(model.FromDate));

                if (existingLeave != null)
                {
                    ViewBag.ErrorMessage = "You have already applied for duty leave on the same day.";
                    return View(model);
                }

                try
                {
                    model.Email = email;
                    string type = "Medical Leave";
                    model.Type = type;

                    db.LeaveRequests.Add(model);
                    db.SaveChanges();

                    string body = $"A Medical leave request has been submitted. Details:\n\n" +
                                  $"Email: {email}\n" +
                                  $"From: {model.FromDate}\n" +
                                  $"To: {model.ToDate}\n";

                    SendLeaveRequestEmail(model, body); // Send email to admin

                    return RedirectToAction("ConfirmLeaveRequest", "NonTeachingStaff");
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "An error occurred while saving the leave request.";
                }
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult UploadMedicalCertificate(int leaveRequestId, HttpPostedFileBase certificateFile)
        {
            // Find the medical leave record based on the provided leaveRequestId
            var medicalLeave = db.MedicalLeaveTable.FirstOrDefault(m => m.Id == leaveRequestId);

            if (medicalLeave != null && certificateFile != null && certificateFile.ContentLength > 0)
            {
                // Save the uploaded certificate to a directory or perform any necessary processing
                // For example, you can save the file to the server and store the file path in the database

                var fileName = Path.GetFileName(certificateFile.FileName);
                var directoryPath = Server.MapPath("~/MedicalCertificates");

                // Create the directory if it doesn't exist
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var filePath = Path.Combine(directoryPath, fileName);
                certificateFile.SaveAs(filePath);

                // Update the medical leave record with the certificate path and status
                medicalLeave.MedicalCertificate = filePath;
                medicalLeave.Status = "Uploaded";

                // Save the updated medical leave record to the database
                db.SaveChanges();
            }

            // Redirect back to the UploadMedicalCertificate action to display the updated medical leave requests
            return RedirectToAction("UploadMedicalCertificate");
        }

        public ActionResult UploadMedicalCertificate()
        {
            string email = (string)Session["Email"];

            // Retrieve all medical leave requests by the user
            var medicalLeaves = db.MedicalLeaveTable.Where(m => m.Email == email && m.Status == "Pending").ToList();

            return View(medicalLeaves);
        }
        public ActionResult ModifyRequestedLeave()
        {
            // Get the current employee's email from the session
            string email = (string)Session["Email"];

            // Retrieve leave requests for the current employee with FromDate >= Current Date
            var leaveRequests = db.LeaveRequests
                .Where(lr => lr.Email == email && DbFunctions.TruncateTime(lr.FromDate) >= DbFunctions.TruncateTime(DateTime.Now))
                .ToList();

            return View("ModifyRequestedLeave", leaveRequests);
        }

        public ActionResult ModifyCasualLeave(int leaveRequestId)
        {
            // Fetch the requested leave from the database based on the provided leaveRequestId
            var leaveRequest = db.LeaveRequests.FirstOrDefault(lr => lr.Id == leaveRequestId);

            return View(leaveRequest);
        }
        private bool IsPastDate(DateTime date)
        {
            return date.Date < DateTime.Now.Date || (date.Date == DateTime.Now.Date && DateTime.Now.TimeOfDay >= TimeSpan.Parse("09:20:00"));
        }

        private bool ExceedsLeaveLimit(string email, int maxDayTS)
        {
            var casualLeave = db.CasualLeaveTable.Where(r => r.Email == email && r.Date.Year == DateTime.Now.Year);
            return casualLeave.Count() >= maxDayTS;
        }


        [HttpPost]
        public ActionResult ModifyCasualLeave(LeaveRequest model)
        {
            if (ModelState.IsValid)
            {
                if (model.FromDate < DateTime.Now)
                {
                    ViewBag.ErrorMessage = "You cannot apply for leave for this day";
                    return View(model);
                }
                else if (model.FromDate == DateTime.Now)
                {
                    if (DateTime.Now.Hour < 9 && DateTime.Now.Minute < 20)
                    {
                        ViewBag.ErrorMessage = "You cannot apply for leave for the current day after 9:20 AM.";
                        return View(model);
                    }
                }
                var leaveType = db.LeaveType.FirstOrDefault(l => l.Type == "CasualLeave");
                if (ExceedsLeaveLimit(model.Email, leaveType.MaxDayNTS))
                {
                    ViewBag.ErrorMessage = "You have already applied for casual leave more than the maximum allowed limit.";
                    return View(model);
                }
                try
                {
                    // Fetch the existing leave request from the database
                    var existingLeaveRequest = db.LeaveRequests.FirstOrDefault(lr => lr.Id == model.Id);

                    if (existingLeaveRequest != null)
                    {
                        // Update the properties of the existing leave request with the modified values
                        existingLeaveRequest.FromDate = model.FromDate;
                        existingLeaveRequest.Reason = model.Reason;
                        existingLeaveRequest.Remarks = model.Remarks;
                        existingLeaveRequest.Status = "Modified";

                        // Save the changes to the database
                        db.SaveChanges();
                        string body = $"A Casual leave request Modification has been submitted. Details:\n\n" +
                           $"Email: {model.Email}\n" +
                           $"From: {model.FromDate}\n" +
                           $"Reason: {model.Reason}\n";

                        SendLeaveRequestEmail(model, body);

                        // Redirect to the "ConfirmModifiedLeave" action to show success message
                        return RedirectToAction("ConfirmModifiedLeave");
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Leave request not found.";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "An error occurred while saving the leave request.";
                }
            }

            // If there are validation errors or the leave request was not found, return the view with the model
            return View(model);
        }

        public ActionResult ModifyMaternityLeave(int leaveRequestId)
        {
            var leaveRequest = db.LeaveRequests.FirstOrDefault(lr => lr.Id == leaveRequestId);
            return View(leaveRequest);
        }
        [HttpPost]
        public ActionResult ModifyMaternityLeave(LeaveRequest model)
        {
            if (ModelState.IsValid)
            {

                if (model.FromDate < DateTime.Now)
                {
                    ViewBag.ErrorMessage = "You cannot apply for leave for this day";
                    return View(model);
                }
                else if (model.FromDate == DateTime.Now)
                {
                    if (DateTime.Now.Hour < 9 && DateTime.Now.Minute < 20)
                    {
                        ViewBag.ErrorMessage = "You cannot apply for leave for the current day after 9:20 AM.";
                        return View(model);
                    }
                }
                if (model.FromDate > model.ToDate)
                {
                    ViewBag.ErrorMessage = "Check From date and To date";
                    return View(model);

                }
                var leaveType = db.LeaveType.FirstOrDefault(l => l.Type == "Maternity Leave");
                if (leaveType != null)
                {
                    // Check if the user has already applied for maternity leave within the past 180 days
                    var maternityLeaves = db.LeaveRequests.Where(l => l.Email == model.Email && l.Type == "Maternity Leave" && l.FromDate >= DateTime.Now.AddDays(-180));
                    if (maternityLeaves.Count() >= leaveType.MaxDayTS)
                    {
                        ViewBag.ErrorMessage = "You have already applied for maternity leave for the maximum allowed days within the past 180 days.";
                        return View(model);
                    }
                }

                try
                {
                    var existingLeaveRequest = db.LeaveRequests.FirstOrDefault(lr => lr.Id == model.Id);
                    if (existingLeaveRequest != null)
                    {
                        existingLeaveRequest.FromDate = model.FromDate;
                        existingLeaveRequest.ToDate = model.ToDate;
                        var maternityLeaveType = db.LeaveType.FirstOrDefault(l => l.Type == "Maternity Leave");
                        int maxDayTS = maternityLeaveType?.MaxDayTS ?? 0;
                        // Calculate the new ToDate by adding the maximum allowed maternity leave days
                        existingLeaveRequest.ToDate = model.FromDate.AddDays(maxDayTS);

                        existingLeaveRequest.Remarks = model.Remarks;
                        existingLeaveRequest.Status = "Modified";

                        db.SaveChanges();
                        string body = $"A Maternity leave request Modification has been submitted. Details:\n\n" +
                                       $"Email: {model.Email}\n" +
                                       $"From: {model.FromDate}\n" +
                                       $"To: {model.ToDate}\n" +
                                       $"Reason: {model.Reason}\n";

                        SendLeaveRequestEmail(model, body);

                        // Redirect to the "ConfirmModifiedLeave" action to show the success message
                        return RedirectToAction("ConfirmModifiedLeave");
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Leave request not found.";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "An error occurred while saving the leave request.";
                }
            }
            return View(model);
        }
        public ActionResult ModifyVacationLeave(int leaveRequestId)
        {
            var leaveRequest = db.LeaveRequests.FirstOrDefault(lr => lr.Id == leaveRequestId);
            return View(leaveRequest);
        }
        [HttpPost]
        public ActionResult ModifyVacationLeave(LeaveRequest model)
        {
            if (ModelState.IsValid)
            {
                if (model.FromDate < DateTime.Now)
                {
                    ViewBag.ErrorMessage = "You cannot apply for leave for this day";
                    return View(model);
                }
                else if (model.FromDate == DateTime.Now)
                {
                    if (DateTime.Now.Hour < 9 && DateTime.Now.Minute < 20)
                    {
                        ViewBag.ErrorMessage = "You cannot apply for leave for the current day after 9:20 AM.";
                        return View(model);
                    }
                }
                if (model.FromDate >= model.ToDate)
                {
                    ModelState.AddModelError("ToDate", "To Date must be greater than From Date.");
                    return View(model);
                }
                var leaveType = db.LeaveType.FirstOrDefault(l => l.Type == "Study Leave");
                try
                {
                    var existingLeaveRequest = db.LeaveRequests.FirstOrDefault(lr => lr.Id == model.Id);
                    if (existingLeaveRequest != null)
                    {
                        existingLeaveRequest.FromDate = model.FromDate;
                        existingLeaveRequest.ToDate = model.ToDate;
                        existingLeaveRequest.ToDate = model.ToDate;
                        existingLeaveRequest.Remarks = model.Remarks;
                        existingLeaveRequest.Status = "Modified";

                        db.SaveChanges();
                        string body = $"A Vacation leave request Modification has been submitted. Details:\n\n" +
                                       $"Email: {model.Email}\n" +
                                       $"From: {model.FromDate}\n" +
                                       $"To: {model.ToDate}\n" +
                                       $"Reason: {model.Reason}\n";

                        SendLeaveRequestEmail(model, body);
                        return RedirectToAction("ConfirmModifiedLeave");
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Leave request not found.";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "An error occurred while saving the leave request.";
                }
            }
            return View(model);
        }
        public ActionResult ConfirmModifiedLeave()
        {
            // Retrieve the success message from TempData and pass it to the view
            ViewBag.SuccessMessage = TempData["LeaveRequestModified"];
            return View();
        }
        public ActionResult CancelLeaveRequest()
        {
            string employeeEmail = (string)Session["Email"];

            // Fetch all leave requests by the logged-in employee from the database
            var leaveRequests = db.LeaveRequests.Where(lr => lr.Email == employeeEmail && lr.Status != "Cancelled").ToList();

            return View(leaveRequests);
        }
        [HttpPost]
        public ActionResult CancelLeaveRequest(int leaveRequestId)
        {
            // Fetch the requested leave from the database based on the provided leaveRequestId
            var leaveRequest = db.LeaveRequests.FirstOrDefault(lr => lr.Id == leaveRequestId);

            if (leaveRequest != null)
            {
                // Update the status of the leave request to "Cancelled"
                leaveRequest.Status = "Cancelled";
                db.SaveChanges();

                // Redirect to the "ConfirmCancelledLeave" action to show success message
                return RedirectToAction("ConfirmCancelledLeave");
            }
            else
            {
                // If leave request not found, handle accordingly
                ViewBag.ErrorMessage = "Leave request not found.";
                return View("Error"); // You can create an Error view to display the error message
            }
        }
        public ActionResult ConfirmCancelledLeave()
        {
            // Optionally, you can provide a view to display a confirmation message after cancelling leave.
            return View();
        }
        // GET: TeachingStaff/LeaveHistory
        public ActionResult LeaveHistory()
        {
            string employeeEmail = (string)Session["Email"];
            DateTime currentDate = DateTime.Now.Date;

            // Fetch approved leave requests by the logged-in employee with FromDate greater than or equal to the current date
            var leaveHistory = db.LeaveRequests
                .Where(lr => lr.Email == employeeEmail && lr.Status == "Approved" && lr.FromDate >= currentDate)
                .OrderByDescending(lr => lr.FromDate)
                .ToList();

            return View(leaveHistory);
        }
        public ActionResult ChangePassword()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ChangePassword(changePassword model)
        {
            if (ModelState.IsValid)
            {
                string email = (string)Session["Email"];

                // Retrieve the employee record from the database based on the email and old password
                var employee = db.Employees.FirstOrDefault(cp => cp.Email == email && cp.Password == model.OldPassword);

                if (employee != null)
                {
                    if (model.NewPassword == model.ConfirmPassword)
                    {
                        // Update the employee's password with the new password
                        employee.Password = model.NewPassword;

                        // Save changes to the database
                        db.SaveChanges();

                        // Redirect to a success view indicating that the password was changed successfully
                        return RedirectToAction("PasswordChangedSuccess");
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Password is not matching";
                    }
                }
                else
                {
                    ViewBag.ErrorMessage = "Check your old password again";
                }
            }

            // If the model state is not valid or there was an error, return the view with the appropriate error message
            return View(model);
        }
        public ActionResult PasswordChangedSuccess()
        {
            return View();
        }
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}