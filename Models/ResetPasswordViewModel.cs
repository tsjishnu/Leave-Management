using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Leave_Management.Models
{
    public class ResetPasswordViewModel
    {
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set;}
    }
}