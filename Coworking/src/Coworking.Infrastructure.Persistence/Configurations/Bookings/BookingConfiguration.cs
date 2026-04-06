using Coworking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coworking.Infrastructure.Persistence.Configurations.Bookings;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();
        builder.Property(x => x.TimeZoneId).IsRequired();

        /* indexes */

        // overlaps checking
        builder.HasIndex(x => new { x.DeskId, x.StartTime, x.EndTime })
               .HasDatabaseName("ix_bookings_overlap_check");

        builder.HasIndex(nameof(Booking.CreatedAt));

        /* relationships */
        // TODO: add properties and relationships when needed

        // builder.HasOne<Desk>().WithMany().HasForeignKey(x => x.DeskId);
    }
}
