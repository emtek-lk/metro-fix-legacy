using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Configurations;

public sealed class ServiceRequestLifecycleStageTransitionConfiguration : IEntityTypeConfiguration<ServiceRequestLifecycleStageTransition>
{
    public void Configure(EntityTypeBuilder<ServiceRequestLifecycleStageTransition> builder)
    {
        builder.ToTable("ServiceRequestLifecycleStageTransitions");

        builder.HasKey(x => x.Id)
            .HasName("PK_ServiceRequestLifecycleStageTransitions");

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_ServiceRequestLifecycleStageTransitions_TenantId_Id");

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.FromStageId)
            .IsRequired();

        builder.Property(x => x.ToStageId)
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

        builder.HasIndex(x => new { x.TenantId, x.FromStageId, x.ToStageId })
            .IsUnique()
            .HasDatabaseName("UQ_ServiceRequestLifecycleStageTransitions_TenantId_From_To");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceRequestLifecycleStageTransitions_Tenants_TenantId");

        builder.HasOne<ServiceRequestLifecycleStage>()
            .WithMany()
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .HasForeignKey(x => new { x.TenantId, x.FromStageId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceRequestLifecycleStageTransitions_Stages_From");

        builder.HasOne<ServiceRequestLifecycleStage>()
            .WithMany()
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .HasForeignKey(x => new { x.TenantId, x.ToStageId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceRequestLifecycleStageTransitions_Stages_To");
    }
}
