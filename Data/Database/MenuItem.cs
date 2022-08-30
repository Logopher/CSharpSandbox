using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace Data.Database;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public class MenuItem : IValidatableObject
{
    internal int Id { get; set; }

    public MenuItem? Parent { get; set; }

    public List<MenuItem> Children { get; set; }

    public string Header { get; internal set; }

    public char? AccessCharacter { get; set; }

    public string? CommandName { get; set; }

    public bool IsReadOnly { get; internal set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if((CommandName == null) == (Children.Count == 0))
        {
            yield return new ValidationResult("Must have either a command or another level, not both.");
        }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

internal class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder
            .HasKey(x => x.Id);

        builder
            .Property(i => i.Header)
            .IsRequired();

        builder
            .HasMany(i => i.Children)
            .WithOne(i => i.Parent);
    }
}
