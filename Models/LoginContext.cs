using System.Data.Entity;

namespace Leave_Management.Models
{
    public class LoginContext : DbContext
    {
        public DbSet<Admin> AdminTable { get; set; }
        public DbSet<Leave> LeaveType { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<CasualLeave> CasualLeaveTable { get; set; }
        public DbSet<DutyLeave>DutyLeaveTable { get; set; }
        public DbSet<MaternityLeave> MaternityLeaveTable { get; set; }
        public DbSet<StudyLeave>StudyLeaveTable { get; set; }
        public DbSet<VacationLeave>VacationLeaveTable { get; set; }
        public DbSet<MedicalLeave>MedicalLeaveTable { get; set; }
    
        public LoginContext() : base("EmployeeConnection")
        {
        }
    }
}
