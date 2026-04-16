using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Configurations;

public sealed class ServiceRequestLifecycleStageConfiguration : IEntityTypeConfiguration<ServiceRequestLifecycleStage>
{
    public void Configure(EntityTypeBuilder<ServiceRequestLifecycleStage> builder)
    {
        builder.ToTable("ServiceRequestLifecycleStages");

        builder.HasKey(x => x.Id)
            .HasName("PK_ServiceRequestLifecycleStages");

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_ServiceRequestLifecycleStages_TenantId_Id");

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.StatusCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.DisplayOrder)
            .HasColumnType("int")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(x => x.IsDeleted)
            .HasColumnType("bit")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.DisplayOrder })
            .HasDatabaseName("IX_ServiceRequestLifecycleStages_TenantId_DisplayOrder");

        builder.HasIndex(x => new { x.TenantId, x.DisplayName })
            .IsUnique()
            .HasDatabaseName("UQ_ServiceRequestLifecycleStages_TenantId_DisplayName");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceRequestLifecycleStages_Tenants_TenantId");
    }
}
