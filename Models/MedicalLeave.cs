using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Leave_Management.Models
{
    [Table("tblMedicalLeave")]
    public class MedicalLeave
    {
        [Key]
        public int Id { get; set; }
        public string Email { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public string Reason { get; set; }
        public string MedicalCertificate { get; set; }
        public string Remarks { get; set; }
        public string Status { get; set; }

    }
}