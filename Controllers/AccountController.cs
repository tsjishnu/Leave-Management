using System;
using System.Linq;
using System.Net.Mail;
using System.Web.Mvc;
using Leave_Management.Models;

namespace Leave_Management.Controllers
{
    public class AccountController : Controller
    {
        private LoginContext db;

        public AccountController()
        {
            db = new LoginContext();
        }

        // GET: /Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var admin = db.AdminTable.FirstOrDefault(a => a.Email == model.Email && a.Password == model.Password);
                var employee = db.Employees.FirstOrDefault(e => e.Email == model.Email && e.Password == model.Password && e.Status == "Approve");

                if (admin != null)
                {
                    Session["Email"] = model.Email;
                    return RedirectToAction("Index", "Admin");
                }
                else if (employee != null)
                {
                    Session["Email"] = model.Email;
                    if (employee.Category == "TeachingStaff")
                    {
                        return RedirectToAction("Index", "TeachingStaff");
                    }
                    else
                    {
                        return RedirectToAction("Index", "NonTeachingStaff");
                    }

                }
                else
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }
            }

            // If the model is not valid, return the view with the model to display validation errors
            return View(model);
        }


        public ActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Signup(Employee employee)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    int age = CalculateAge(employee.DateofBirth);

                    // Check if the employee is below 23 years old
                    if (age < 23)
                    {
                        ModelState.AddModelError("", "Employee must be at least 23 years old to sign up.");
                        return View(employee);
                    }
                    // Save the employee to the database
                    db.Employees.Add(employee);
                    db.SaveChanges();

                    // Retrieve the admin email from the database
                    var admin = db.AdminTable.FirstOrDefault();
                    if (admin == null)
                    {
                        // Admin record not found, handle the situation accordingly
                        ModelState.AddModelError("", "Admin record not found.");
                        return View(employee);
                    }

                    SendEmailNotification(admin.Email, employee);

                    return RedirectToAction("Login", "Account");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while signing up: " + ex.Message);
                }
            }

            return View(employee);
        }
        private int CalculateAge(DateTime dateOfBirth)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - dateOfBirth.Year;
            if (dateOfBirth > today.AddYears(-age))
                age--;

            return age;
        }


        private void SendEmailNotification(string recipientEmail, Employee model)
        {
            string senderEmail = "tsjishnu200@gmail.com";
            string senderPassword = "gbzedyajitbosnqu"; // Replace with the actual sender email password

            MailMessage mail = new MailMessage(senderEmail, recipientEmail);
            mail.Subject = "Employee Signup Request Verification";

            string body = $"An employee has requested signup:\n\nName: {model.FullName}\nEmail: {model.Email}\nCategory: {model.Category}\n\nPlease verify their request and take appropriate action.\n\nThank you.";

            mail.Body = body;

            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
            smtpClient.Credentials = new System.Net.NetworkCredential(senderEmail, senderPassword);
            smtpClient.EnableSsl = true;
            smtpClient.Send(mail);
        }
        public ActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if the provided email exists in the database
                var user = db.Employees.FirstOrDefault(u => u.Email == model.Email);
                if (user != null)
                {
                    // Generate a random OTP (One-Time Password)
                    Random random = new Random();
                    int otp = random.Next(100000, 999999);

                    // Save the OTP in the database (you can create a new table to store OTPs with a reference to the user)
                    user.Otp = otp;
                    db.SaveChanges();

                    // Send the OTP to the user's email
                    SendOTPEmail(model.Email, otp);

                    // Redirect to the OTP verification page with the user's email
                    return RedirectToAction("VerifyOTP", new { email = model.Email });
                }
                else
                {
                    // If the email doesn't exist in the database, show an error message
                    ViewBag.ErrorMessage = "Email not found.";
                }
            }
            return View(model);
        }

        // Method to send the OTP to the user's email
        private void SendOTPEmail(string email, int otp)
        {
            string senderEmail = "tsjishnu200@gmail.com";
            string senderPassword = "gbzedyajitbosnqu"; // Replace with the actual sender email password

            MailMessage mail = new MailMessage(senderEmail, email);
            string subject = "OTP for Password Reset";
            string body = $"Your OTP for password reset is: {otp}";
            mail.Subject = subject;
            mail.Body = body;

            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
            smtpClient.Credentials = new System.Net.NetworkCredential(senderEmail, senderPassword);
            smtpClient.EnableSsl = true;
            smtpClient.Send(mail);
        }
        public ActionResult VerifyOTP(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public ActionResult VerifyOTP(string email, int otp)
        {
            if (ModelState.IsValid)
            {
                var user = db.Employees.FirstOrDefault(u => u.Email == email && u.Otp == otp);
                if (user != null)
                {
                    return RedirectToAction("ResetPassword", new { email = email });
                }
                else
                {
                    ViewBag.ErrorMessage = "Invalid OTP. Please try again.";
                }
            }
            return View();
        }
        // GET: Account/ResetPassword
        public ActionResult ResetPassword(string email)
        {
            // Pass the user's email to the view for password reset
            ViewBag.Email = email;
            return View();
        }

        // POST: Account/ResetPassword
        [HttpPost]
        public ActionResult ResetPassword(string email, string newPassword,string confirmPassword)
        {
            if (ModelState.IsValid)
            {
                // Find the user by email
                var user = db.Employees.FirstOrDefault(u => u.Email == email);
                if (user != null)
                {
                    if (newPassword != confirmPassword)
                    {
                        ViewBag.ErrorMessage = "Passwords not matching.";
                    }
                    else
                    {
                        user.Password = newPassword;
                        db.SaveChanges();
                        return RedirectToAction("SuccessPasswordReset");
                    }
                }
                else
                {
                    // If the user is not found, show an error message
                    ViewBag.ErrorMessage = "User not found. Please try again.";
                }
            }
            return View();
        }
        public ActionResult SuccessPasswordReset()
        {
            return View();
        }
    }
}
