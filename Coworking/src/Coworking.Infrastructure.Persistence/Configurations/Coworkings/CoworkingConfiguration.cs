using Coworking.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coworking.Infrastructure.Persistence.Configurations.Coworkings;

public class CoworkingConfiguration : IEntityTypeConfiguration<Domain.Entities.Coworking>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Coworking> builder)
    {
        builder.ToTable("Coworkings");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Version)
            .IsRowVersion();
        //.IsConcurrencyToken();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.OwnsOne(x => x.SlotSize, SlotSizeEfMapping.Map);

        builder.Property(c => c.TimeZoneId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.OpenTime)
            .IsRequired();

        builder.Property(c => c.CloseTime)
            .IsRequired();

        builder.HasMany(x => x.Desks)
            .WithOne(c => c.Coworking)
            .HasForeignKey(x => x.CoworkingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}