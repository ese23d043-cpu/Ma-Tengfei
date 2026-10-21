using MaiYuHCM.Models;
using Microsoft.EntityFrameworkCore;

namespace MaiYuHCM.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<ScheduleAssignment> ScheduleAssignments { get; set; }
    }
}