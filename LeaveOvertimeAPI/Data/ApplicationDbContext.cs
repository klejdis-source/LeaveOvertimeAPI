using LeaveOvertimeAPI.Models;
using LeaveOvertimeAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveOvertimeAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        
        public DbSet<Employee> Employees { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Overtime> Overtimes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.Employee) // Një leje ka një punonjës
                .WithMany()              // Një punonjës mund të ketë shumë leje
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade); // Nëse fshihet punonjësi, fshihen edhe lejet e tij

            // Konfigurimi  për Overtime
            modelBuilder.Entity<Overtime>()
                .HasOne(o => o.Employee)
                .WithMany()
                .HasForeignKey(o => o.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Konfigurimi për pagën 
            modelBuilder.Entity<Employee>()
                .Property(e => e.Salary)
                .HasColumnType("decimal(18,2)");

            // Konfigurimi për orët (HoursWorked)
            modelBuilder.Entity<Overtime>()
                .Property(o => o.HoursWorked)
                .HasColumnType("decimal(18,2)");
        }
    }
}