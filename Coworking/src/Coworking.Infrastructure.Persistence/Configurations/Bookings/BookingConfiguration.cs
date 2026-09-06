using Coworking.Domain.Constants;
using Coworking.Domain.Entities;
using Coworking.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coworking.Infrastructure.Persistence.Configurations.Bookings;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasStoreConcurrencyToken();

        builder.Property(x => x.StartTime)
            .IsRequired();

        builder.Property(x => x.EndTime)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.UserEmail)
            .IsRequired()
            .HasMaxLength(BookingLimits.UserEmailMaxLength);

        builder.Property(x => x.UserName)
            .IsRequired()
            .HasMaxLength(BookingLimits.UserNameMaxLength);

        builder.Property(x => x.UserTimeZoneId)
            .HasMaxLength(BookingLimits.UserTimeZoneMaxLength);

        builder.HasIndex(x => x.AccessCode)
               .IsUnique();

        builder.HasIndex(x => new { x.DeskId, x.StartTime })
               .IncludeProperties(x => new { x.EndTime, x.Status })
               .HasDatabaseName("ix_bookings_overlap_check");

        builder.HasIndex(x => x.CreatedAt);
    }
}