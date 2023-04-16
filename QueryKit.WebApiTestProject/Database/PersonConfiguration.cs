namespace QueryKit.WebApiTestProject.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryKit.WebApiTestProject.Entities;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(x => x.Id);

        // example for a simple 1:1 value object
        // builder.Property(x => x.Percent)
        //     .HasConversion(x => x.Value, x => new Percent(x))
        //     .HasColumnName("percent");
    }
}