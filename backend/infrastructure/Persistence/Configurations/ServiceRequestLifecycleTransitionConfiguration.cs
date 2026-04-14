using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Configurations;

public sealed class ServiceRequestLifecycleTransitionConfiguration : IEntityTypeConfiguration<ServiceRequestLifecycleTransition>
{
    public void Configure(EntityTypeBuilder<ServiceRequestLifecycleTransition> builder)
    {
        builder.ToTable("ServiceRequestLifecycleTransitions");

        builder.HasKey(x => x.Id)
            .HasName("PK_ServiceRequestLifecycleTransitions");

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_ServiceRequestLifecycleTransitions_TenantId_Id");

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .HasColumnType("bit")
            .HasDefaultValue(true)
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

        builder.HasIndex(x => new { x.TenantId, x.FromStatus, x.ToStatus })
            .IsUnique()
            .HasDatabaseName("UQ_ServiceRequestLifecycleTransitions_TenantId_From_To");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceRequestLifecycleTransitions_Tenants_TenantId");
    }
}