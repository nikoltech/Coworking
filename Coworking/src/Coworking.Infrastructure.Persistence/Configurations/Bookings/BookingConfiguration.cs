using Coworking.Domain.Constants;
using Coworking.Domain.Entities;
using Coworking.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Coworking.Infrastructure.Persistence.Configurations.Bookings;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    // the check constraint spells the column out, so both must come from here
    private const string AccessCodeColumn = "access_code";

    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(x => x.Id);

        // the UUID version sits in the high nibble of byte 6
        builder.ToTable("bookings", t => t.HasCheckConstraint(
            "ck_bookings_access_code_v7",
            $"get_byte(uuid_send({AccessCodeColumn}), 6) >> 4 = 7"));

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

        builder.Property(x => x.AccessCode)
            .HasColumnName(AccessCodeColumn)
            .IsRequired()
            .ValueGeneratedOnAdd()
            .HasValueGenerator<UUIDv7ValueGenerator>();

        builder.HasIndex(x => x.AccessCode)
               .IsUnique();

        builder.HasIndex(x => new { x.DeskId, x.StartTime })
               .IncludeProperties(x => new { x.EndTime, x.Status })
               .HasDatabaseName("ix_bookings_overlap_check");

        builder.HasIndex(x => x.CreatedAt);
    }
}