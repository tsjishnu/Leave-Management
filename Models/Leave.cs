using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web;

namespace Leave_Management.Models
{
    [Table("tblLeaveTypes")]
    public class Leave
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Type { get; set; }
        [Required]
        public int MaxDayTS { get; set; }
        [Required]
        public int MaxDayNTS { get; set; }
        public string Description { get; set; }
    }
}