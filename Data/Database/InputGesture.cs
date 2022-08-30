using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Database
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class InputGesture
    {
        internal int Id { get; set; }

        public string Stimulus { get; set; }

        public string CommandName { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    internal class InputGestureConfiguration : IEntityTypeConfiguration<InputGesture>
    {
        public void Configure(EntityTypeBuilder<InputGesture> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .Property(i => i.Stimulus)
                .IsRequired();

            builder
                .Property(i => i.CommandName)
                .IsRequired();
        }
    }
}
