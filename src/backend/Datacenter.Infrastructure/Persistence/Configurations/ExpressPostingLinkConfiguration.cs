using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class ExpressPostingLinkConfiguration : IEntityTypeConfiguration<ExpressPostingLink>
{
    public void Configure(EntityTypeBuilder<ExpressPostingLink> builder)
    {
        builder.ToTable("ExpressPostingLinks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceType).HasConversion<int>();
        builder.Property(x => x.ExpressDocNo).HasMaxLength(60);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.PostedAmount).HasColumnType("decimal(18,2)");

        builder.HasOne<ClientCompany>()
            .WithMany().HasForeignKey(x => x.ClientCompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.SourceType, x.Year, x.Month }).IsUnique();
    }
}
