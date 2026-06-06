using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class PrepaidExpenseConfiguration : IEntityTypeConfiguration<PrepaidExpense>
{
    public void Configure(EntityTypeBuilder<PrepaidExpense> builder)
    {
        builder.ToTable("PrepaidExpenses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.AttachmentPath).HasMaxLength(500);
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.ClientCompany)
            .WithMany().HasForeignKey(x => x.ClientCompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.IsActive });
    }
}
