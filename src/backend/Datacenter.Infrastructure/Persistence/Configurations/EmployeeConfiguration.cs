using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EmployeeCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.NationalId).HasMaxLength(20);
        builder.Property(x => x.Prefix).HasMaxLength(30);
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100);
        builder.Property(x => x.MaritalStatus).HasMaxLength(20);
        builder.Property(x => x.Nationality).HasMaxLength(50);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Position).HasMaxLength(100);
        builder.Property(x => x.Department).HasMaxLength(100);
        builder.Property(x => x.SourceSupplierCode).HasMaxLength(30);
        builder.Property(x => x.SsoNumber).HasMaxLength(20);
        builder.Property(x => x.SsoHospital).HasMaxLength(200);
        builder.Property(x => x.TaxId).HasMaxLength(13);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.BaseSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DailyWage).HasColumnType("decimal(18,2)");
        // enum เก็บเป็น int (ค่าเริ่มต้น) — เลี่ยงปัญหา project (int) ใน LINQ

        builder.HasOne<ClientCompany>()
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // รหัสพนักงานไม่ซ้ำต่อบริษัท
        builder.HasIndex(x => new { x.ClientCompanyId, x.EmployeeCode }).IsUnique();
        builder.HasIndex(x => new { x.ClientCompanyId, x.NationalId });

        builder.HasMany(x => x.Documents)
            .WithOne(d => d.Employee!)
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.SsoEnrollments)
            .WithOne(e => e.Employee!)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
