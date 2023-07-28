using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Leave_Management.Models
{
    [Table("tblEmployee")]
    public class Employee
    {
        [Key]
        [Required]
        public string Email { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Category { get; set; }

        [Required]
        public DateTime JoiningDate { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public DateTime DateofBirth { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public int? Otp { get; set; } = 00000;
    }
}
