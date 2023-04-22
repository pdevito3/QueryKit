namespace QueryKit.WebApiTestProject.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryKit.WebApiTestProject.Entities;

public sealed class PersonConfiguration : IEntityTypeConfiguration<TestingPerson>
{
    public void Configure(EntityTypeBuilder<TestingPerson> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Email)
            .HasConversion(x => x.Value, x => new EmailAddress(x))
            .HasColumnName("email")
            .IsRequired(false);
    }
}