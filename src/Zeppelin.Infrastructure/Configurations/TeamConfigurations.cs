using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zeppelin.Domain.Entities.Team;

namespace Zeppelin.Infrastructure.Configurations;

public class TeamTaskConfiguration : IEntityTypeConfiguration<TeamTask>
{
    public void Configure(EntityTypeBuilder<TeamTask> builder)
    {
        builder.HasIndex(t => new { t.AssignedToUserId, t.Status });

        builder.HasOne(t => t.AssignedToUser).WithMany()
            .HasForeignKey(t => t.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.AssignedByUser).WithMany()
            .HasForeignKey(t => t.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
