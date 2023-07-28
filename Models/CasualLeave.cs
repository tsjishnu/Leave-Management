using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Leave_Management.Models
{
    [Table("tblCasualLeave")]
    public class CasualLeave
    {
        [Key]
        public int Id { get; set; }
        public string Email { get; set; }
        public DateTime Date { get; set; }
        public string Reason { get; set; }
        public string Remarks { get; set; }


    }
}