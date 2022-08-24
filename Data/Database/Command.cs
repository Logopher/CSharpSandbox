using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Database;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
internal class Command
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string FilePath { get; set; }

    public string Language { get; set; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

internal class CommandConfiguration : IEntityTypeConfiguration<Command>
{
    public void Configure(EntityTypeBuilder<Command> builder)
    {
        builder
            .HasKey(x => x.Id);

        builder
            .Property(i => i.Language)
            .IsRequired();

        builder
            .Property(i => i.FilePath)
            .IsRequired();
    }
}
