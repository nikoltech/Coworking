using Coworking.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coworking.Infrastructure.Persistence.Configurations.Coworkings;

public class CoworkingConfiguration : IEntityTypeConfiguration<Domain.Entities.Coworking>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Coworking> builder)
    {
        builder.ToTable("coworkings", t => t.HasCheckConstraint(
            "ck_coworkings_working_hours",
            "is_non_stop OR (open_time IS NOT NULL AND close_time IS NOT NULL AND open_time <> close_time)"));

        builder.HasKey(c => c.Id);

        builder.HasStoreConcurrencyToken();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.OwnsOne(x => x.SlotSize, SlotSizeEfMapping.Map);

        builder.Property(c => c.TimeZoneId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasMany(x => x.Desks)
            .WithOne(c => c.Coworking)
            .HasForeignKey(x => x.CoworkingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}