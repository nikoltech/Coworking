using Coworking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coworking.Infrastructure.Persistence.Configurations.Bookings;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StartTime)
            .IsRequired();

        builder.Property(x => x.EndTime)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.UserTimeZoneId)
            .HasMaxLength(100);

        // indexes
        builder.HasIndex(x => new { x.DeskId, x.StartTime, x.EndTime, x.Status })
               .HasDatabaseName("ix_bookings_overlap_check");

        builder.HasIndex(x => x.CreatedAt);
    }
}