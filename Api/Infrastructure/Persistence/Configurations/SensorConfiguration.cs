using Api.Domain.Entities;
using Api.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations;

internal sealed class SensorConfiguration : IEntityTypeConfiguration<Sensor>
{
    public void Configure(EntityTypeBuilder<Sensor> builder)
    {
        builder
            .ToTable("Sensors");

        builder
            .HasKey(u => u.Id)
            .HasName("PK_Sensors");

        builder
            .Property(s => s.PlotId)
            .IsRequired();

        builder
            .Property(s => s.Timestamp)
            .IsRequired();

        builder
            .HasIndex(u => u.PlotId)
            .HasDatabaseName("IX_Sensors_PlotId");

        builder
            .HasIndex(u => u.Timestamp)
            .HasDatabaseName("IX_Sensors_Timestamp");
    }
}
