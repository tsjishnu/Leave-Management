using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using Leave_Management.Models;
using Leave_Management.Models.ViewModels;

namespace Leave_Management.Controllers
{
    public class AdminController : Controller
    {
        private LoginContext db;

        public AdminController()
        {
            db = new LoginContext();
        }

        public ActionResult Index()
        {
            DateTime currentDate = DateTime.Now.Date;
            // Fetch approved leave requests from the database
            List<LeaveRequest> approvedLeaveRequests = db.LeaveRequests
                .Where(lr => lr.Status == "Approved" && lr.FromDate >= currentDate)
                .ToList();

            // Get the employee full names for approved leave requests
            var viewModel = new AdminViewModel
            {
                ApprovedLeaveRequests = approvedLeaveRequests,
                EmployeeFullNames = GetEmployeeFullNames(approvedLeaveRequests)
            };

            // Pass the viewModel to the view
            return View(viewModel);
        }

        // Helper method to get employee full names for approved leave requests
        private Dictionary<string, string> GetEmployeeFullNames(List<LeaveRequest> leaveRequests)
        {
            // Assuming you have a database or source to get the employee full names based on emails
            // For demonstration purposes, let's assume the Employee model has the FullName property

            // Fetch distinct employee emails from the leave requests
            var employeeEmails = leaveRequests.Select(lr => lr.Email).Distinct();

            // Create a dictionary to store employee emails and full names
            var employeeFullNames = new Dictionary<string, string>();

            foreach (var email in employeeEmails)
            {
                // Fetch the employee based on the email from the database or source
                var employee = db.Employees.FirstOrDefault(emp => emp.Email == email);
                if (employee != null)
                {
                    // Add the employee email and full name to the dictionary
                    employeeFullNames.Add(employee.Email, employee.FullName);
                }
            }

            return employeeFullNames;
        }

        // GET: Admin/AddLeave
        public ActionResult AddLeave()
        {
            return View();
        }

        // POST: Admin/AddLeave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddLeave(Leave model)
        {
            if (ModelState.IsValid)
            {
                db.LeaveType.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }
        public ActionResult ManageSignupRequest()
        {
            var pendingEmployees = db.Employees.Where(e => e.Status == "Pending").ToList();

            if (pendingEmployees.Count == 0)
            {
                ViewBag.Message = "No pending signup requests.";
            }

            return View(pendingEmployees);
        }

        [HttpPost]
        public ActionResult UpdateStatus(string email, string status)
        {
            var employee = db.Employees.FirstOrDefault(e => e.Email == email);
            if (employee != null)
            {
                employee.Status = status;
                db.SaveChanges();
            }

            return RedirectToAction("ManageSignupRequest");
        }
        public ActionResult ReviewLeaveRequest()
        {
            var leaveRequests = db.LeaveRequests.Where(r => r.Status == "Pending" || r.Status == "Modified").ToList();

            return View(leaveRequests);
        }

        public ActionResult RejectLeaveRequest(int leaveRequestId)
        {
            var leaveRequest = db.LeaveRequests.Find(leaveRequestId);
            leaveRequest.Status = "Rejected";
            db.SaveChanges();
            return RedirectToAction("ReviewLeaveRequest");
        }
        public ActionResult ApproveLeaveRequest(int leaveRequestId)
        {
            var leaveRequest = db.LeaveRequests.Find(leaveRequestId);
            leaveRequest.Status = "Approved";
            db.SaveChanges();

            if (leaveRequest.Type == "Casual Leave")
            {
                var existingCasualLeave = db.CasualLeaveTable.FirstOrDefault(c => c.Email == leaveRequest.Email && c.Date == leaveRequest.FromDate);

                if (existingCasualLeave != null)
                {
                    // Update the existing Casual Leave entry
                    existingCasualLeave.Date = leaveRequest.FromDate;
                    existingCasualLeave.Reason = leaveRequest.Reason;
                    existingCasualLeave.Remarks = leaveRequest.Remarks;
                }
                else
                {
                    // Create a new Casual Leave entry
                    var casualLeave = new CasualLeave
                    {
                        Email = leaveRequest.Email,
                        Date = leaveRequest.FromDate,
                        Reason = leaveRequest.Reason,
                        Remarks = leaveRequest.Remarks
                    };

                    db.CasualLeaveTable.Add(casualLeave);
                    db.SaveChanges();
                }
            }
            else if (leaveRequest.Type == "Duty Leave")
            {
                var existingDutyLeave = db.DutyLeaveTable.FirstOrDefault(c => c.Email == leaveRequest.Email && c.FromDate == leaveRequest.FromDate);

                if (existingDutyLeave != null)
                {
                    // Update the existing Casual Leave entry
                    existingDutyLeave.FromDate = leaveRequest.FromDate;
                    existingDutyLeave.ToDate = leaveRequest.FromDate;
                    existingDutyLeave.Reason = leaveRequest.Reason;
                    existingDutyLeave.Remarks = leaveRequest.Remarks;
                }
                else
                {
                    // Create a new Casual Leave entry
                    var dutyLeave = new DutyLeave
                    {
                        Email = leaveRequest.Email,
                        FromDate = leaveRequest.FromDate,
                        ToDate = leaveRequest.ToDate,
                        Reason = leaveRequest.Reason,
                        Remarks = leaveRequest.Remarks,
                        DutyCertificate = "To be Uploaded",
                        Status = "Pending",
                    };
                    db.DutyLeaveTable.Add(dutyLeave);
                    db.SaveChanges();
                }
            }
            else if (leaveRequest.Type == "Maternity Leave")
            {
                var existingMaternityLeave = db.MaternityLeaveTable.FirstOrDefault(m => m.Email == leaveRequest.Email && m.FromDate == leaveRequest.FromDate);

                if (existingMaternityLeave != null)
                {
                    // Update the existing Maternity Leave entry
                    existingMaternityLeave.Email = leaveRequest.Email;
                    existingMaternityLeave.FromDate = leaveRequest.FromDate;
                    existingMaternityLeave.ToDate = leaveRequest.ToDate;
                    existingMaternityLeave.Remarks = leaveRequest.Remarks;
                    existingMaternityLeave.Status = "Approved";
                }
                else
                {
                    // Create a new Maternity Leave entry
                    var maternityLeave = new MaternityLeave
                    {
                        Email = leaveRequest.Email,
                        FromDate = leaveRequest.FromDate,
                        ToDate = leaveRequest.ToDate,
                        Remarks = leaveRequest.Remarks,
                        Status = "Approved",
                    };

                    db.MaternityLeaveTable.Add(maternityLeave);
                    db.SaveChanges();
                }
            }
            else if (leaveRequest.Type == "Study Leave")
            {
                var existingStudyLeave = db.StudyLeaveTable.FirstOrDefault(s => s.Email == leaveRequest.Email && s.FromDate == leaveRequest.FromDate);

                if (existingStudyLeave != null)
                {
                    // Update the existing Study Leave entry
                    existingStudyLeave.Email = leaveRequest.Email;
                    existingStudyLeave.FromDate = leaveRequest.FromDate;
                    existingStudyLeave.ToDate = leaveRequest.ToDate;
                    existingStudyLeave.Remarks = leaveRequest.Remarks;
                    existingStudyLeave.Status = "Pending";
                }
                else
                {
                    // Create a new Study Leave entry
                    var studyLeave = new StudyLeave
                    {
                        Email = leaveRequest.Email,
                        FromDate = leaveRequest.FromDate,
                        ToDate = leaveRequest.ToDate,
                        Remarks = leaveRequest.Remarks,
                        Status = "Pending",
                    };

                    db.StudyLeaveTable.Add(studyLeave);
                    db.SaveChanges();
                }
            }
            else if (leaveRequest.Type == "Vacation Leave")
            {
                var existingVacationLeave = db.VacationLeaveTable.FirstOrDefault(v => v.Email == leaveRequest.Email && v.FromDate == leaveRequest.FromDate);

                if (existingVacationLeave != null)
                {
                    // Update the existing Vacation Leave entry
                    existingVacationLeave.Email = leaveRequest.Email;
                    existingVacationLeave.FromDate = leaveRequest.FromDate;
                    existingVacationLeave.ToDate = leaveRequest.ToDate;
                    existingVacationLeave.Remarks = leaveRequest.Remarks;
                    existingVacationLeave.Status = "Pending";
                }
                else
                {
                    // Create a new Vacation Leave entry
                    var vacationLeave = new VacationLeave
                    {
                        Email = leaveRequest.Email,
                        FromDate = leaveRequest.FromDate,
                        ToDate = leaveRequest.ToDate,
                        Remarks = leaveRequest.Remarks,
                        Status = "Approved",
                    };

                    db.VacationLeaveTable.Add(vacationLeave);
                    db.SaveChanges();
                }
            }
            else if (leaveRequest.Type == "Medical Leave")
            {
                var existingMedicalLeave = db.MedicalLeaveTable.FirstOrDefault(m => m.Email == leaveRequest.Email && m.FromDate == leaveRequest.FromDate);

                if (existingMedicalLeave != null)
                {
                    // Update the existing Medical Leave entry
                    existingMedicalLeave.Email = leaveRequest.Email;
                    existingMedicalLeave.FromDate = leaveRequest.FromDate;
                    existingMedicalLeave.ToDate = leaveRequest.ToDate;
                    existingMedicalLeave.Reason = leaveRequest.Reason;
                    existingMedicalLeave.Remarks = leaveRequest.Remarks;
                    existingMedicalLeave.MedicalCertificate = "NotUploaded";
                    existingMedicalLeave.Status = "Pending";
                }
                else
                {
                    // Create a new Medical Leave entry
                    var medicalLeave = new MedicalLeave
                    {
                        Email = leaveRequest.Email,
                        FromDate = leaveRequest.FromDate,
                        ToDate = leaveRequest.ToDate,
                        Reason = leaveRequest.Reason,
                        Remarks = leaveRequest.Remarks,
                        MedicalCertificate = "NotUploaded",
                        Status = "Pending",
                    };

                    db.MedicalLeaveTable.Add(medicalLeave);
                    db.SaveChanges();
                }
            }
            return RedirectToAction("ReviewLeaveRequest");
        }
        public ActionResult ReviewDutyCertificate()
        {
        
            var dutyLeaves = db.DutyLeaveTable.Where(d => d.DutyCertificate != "To be uploaded" && d.Status == "Pending").ToList();

            return View(dutyLeaves);
        }
        public ActionResult DownloadCertificate(string certificatePath)
        {
            if (!string.IsNullOrEmpty(certificatePath) && System.IO.File.Exists(certificatePath))
            {
                // Set the appropriate content type for the file
                string contentType = MimeMapping.GetMimeMapping(certificatePath);

                // Return the file for download
                return File(certificatePath, contentType, Path.GetFileName(certificatePath));
            }

            // If the certificate path is invalid or the file doesn't exist, handle the error
            return HttpNotFound("Certificate not found.");
        }
        public ActionResult ApproveDutyLeave(int leaveRequestId)
        {
            // Find the duty leave record based on the provided leaveRequestId
            var dutyLeave = db.DutyLeaveTable.FirstOrDefault(d => d.Id == leaveRequestId);

            if (dutyLeave != null)
            {
                // Perform any necessary logic for approving the duty leave, such as updating the status

                dutyLeave.Status = "Approved";
                db.SaveChanges();
            }

            // Redirect back to the ReviewDutyCertificate action to display the updated records
            return RedirectToAction("ReviewDutyCertificate");
        }

        public ActionResult RejectDutyLeave(int leaveRequestId)
        {
            // Find the duty leave record based on the provided leaveRequestId
            var dutyLeave = db.DutyLeaveTable.FirstOrDefault(d => d.Id == leaveRequestId);

            if (dutyLeave != null)
            {
                dutyLeave.Status = "Rejected";
                db.SaveChanges();
            }
            return RedirectToAction("ReviewDutyCertificate");
        }
        public ActionResult StudyLeaveDocument()
        {
            var studyLeaves = db.StudyLeaveTable.Where(sl => sl.Status == "Uploaded").ToList();
            return View(studyLeaves);
        }

        public ActionResult ApproveStudyLeaveRequest(int studyLeaveId)
        {
            var studyLeave = db.StudyLeaveTable.Find(studyLeaveId);
            if (studyLeave != null)
            {
                studyLeave.Status = "Approved";
                db.SaveChanges();
            }

            return RedirectToAction("StudyLeaveDocument");
        }

        public ActionResult RejectStudyLeaveRequest(int studyLeaveId)
        {
            var studyLeave = db.StudyLeaveTable.Find(studyLeaveId);
            if (studyLeave != null)
            {
                studyLeave.Status = "Rejected";
                db.SaveChanges();
            }

            return RedirectToAction("StudyLeaveDocument");
        }

        public FileResult ViewDocument(int studyLeaveId)
        {
            var studyLeave = db.StudyLeaveTable.FirstOrDefault(s => s.Id == studyLeaveId);
            if (studyLeave != null && !string.IsNullOrEmpty(studyLeave.DocumentPath))
            {
                string filePath = studyLeave.DocumentPath;
                string fileName = Path.GetFileName(filePath);
                return File(filePath, MimeMapping.GetMimeMapping(fileName), fileName);
            }

            // If the study leave or document is not found, you can return an appropriate error view or handle it accordingly
            return null;
        }
        public ActionResult ReviewMedicalCertificate()
        {
            var medicalLeaves = db.MedicalLeaveTable.Where(d => d.Status == "Uploaded").ToList();
            return View(medicalLeaves);
        }

        // GET: Admin/ApproveMedicalLeave
        public ActionResult ApproveMedicalLeave(int leaveRequestId)
        {
            // Find the medical leave record based on the provided leaveRequestId
            var medicalLeave = db.MedicalLeaveTable.FirstOrDefault(d => d.Id == leaveRequestId);

            if (medicalLeave != null)
            {
                // Perform any necessary logic for approving the medical leave, such as updating the status
                medicalLeave.Status = "Approved";
                db.SaveChanges();
            }

            // Redirect back to the ReviewMedicalCertificate action to display the updated records
            return RedirectToAction("ReviewMedicalCertificate");
        }

        // GET: Admin/RejectMedicalLeave
        public ActionResult RejectMedicalLeave(int leaveRequestId)
        {
            // Find the medical leave record based on the provided leaveRequestId
            var medicalLeave = db.MedicalLeaveTable.FirstOrDefault(d => d.Id == leaveRequestId);

            if (medicalLeave != null)
            {
                // Perform any necessary logic for rejecting the medical leave, such as updating the status
                medicalLeave.Status = "Rejected";
                db.SaveChanges();
            }

            // Redirect back to the ReviewMedicalCertificate action to display the updated records
            return RedirectToAction("ReviewMedicalCertificate");
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
                var admin = db.AdminTable.FirstOrDefault(cp => cp.Email == email && cp.Password == model.OldPassword);

                if (admin != null)
                {
                    if (model.NewPassword == model.ConfirmPassword)
                    {
                        // Update the employee's password with the new password
                        admin.Password = model.NewPassword;

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
        public ActionResult ManageEmployee()
        {
            var employee = db.Employees.Where(d => d.Status == "Approve").ToList();
            return View(employee);
        }
        public ActionResult ViewEmployeeDetails(string email)
        {
            var employee = db.Employees.SingleOrDefault(e => e.Email == email);

            if (employee == null)
            {
                return View("EmployeeNotFound");
            }

            var approvedLeaves = db.LeaveRequests
                .Where(lr => lr.Email == email && lr.Status == "Approved")
                .ToList();

            var leaveTypesCount = approvedLeaves
                .GroupBy(lr => lr.Type)
                .Select(group => new TypeCount
                {
                    Type = group.Key,
                    Count = group.Count(),
                    TotalDays = group.Sum(lr => (lr.ToDate - lr.FromDate).Days + 1)
                })
                .ToList();

            var viewModel = new EmployeeDetailsViewModel
            {
                Employee = employee,
                ApprovedLeaves = approvedLeaves,
                LeaveTypesCount = leaveTypesCount
            };

            return View(viewModel);
        }

        // GET: /Admin/EditEmployee?email=email@example.com
        public ActionResult EditEmployee(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Employee employee = db.Employees.FirstOrDefault(e => e.Email == email);
            if (employee == null)
            {
                return HttpNotFound();
            }

            return View(employee);
        }

        // POST: /Admin/EditEmployee?email=email@example.com
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditEmployee(Employee employee)
        {
            if (ModelState.IsValid)
            {
                db.Entry(employee).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("ManageEmployee");
            }

            return View(employee);
        }

        // GET: /Admin/RemoveEmployee?email=email@example.com
        [HttpGet]
        public ActionResult RemoveEmployee(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Employee employee = db.Employees.FirstOrDefault(e => e.Email == email);

            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("RemoveEmployee")]
        public ActionResult RemoveEmployeePost(string email)
        {
            Employee employee = db.Employees.FirstOrDefault(e => e.Email == email);
            if (employee == null)
            {
                return HttpNotFound();
            }
            employee.Status = "Removed";
            db.SaveChanges();
            return RedirectToAction("ManageEmployee");
        }
        public ActionResult Generate()
        {
            var model = new DateSelectionModel();
            return View(model);
        }

        [HttpPost]
        public ActionResult Generate(DateSelectionModel model)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("ApprovedLeaveReport", new { startDate = model.StartDate, endDate = model.EndDate });
            }
            return View(model);
        }
        public ActionResult ApprovedLeaveReport(DateTime? startDate, DateTime? endDate)
        {
            DateTime currentDate = DateTime.Now.Date;

            // Fetch approved leave requests from the database
            var query = db.LeaveRequests.Where(lr => lr.Status == "Approved" && lr.FromDate >= currentDate);

            // Apply date range filtering if startDate and endDate parameters are provided
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(lr => lr.FromDate >= startDate && lr.ToDate <= endDate);
            }

            List<LeaveRequest> approvedLeaveRequests = query.ToList();

            // Get the employee full names for approved leave requests
            var viewModel = new TeachingStaffViewModel
            {
                ApprovedLeaveRequests = approvedLeaveRequests,
                EmployeeFullNames = GetEmployeeFullNames(approvedLeaveRequests)
            };

            // Pass the viewModel to the view
            return View(viewModel);
        }
    }
}
