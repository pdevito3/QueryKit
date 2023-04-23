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
        
        builder.OwnsOne(x => x.PhysicalAddress, opts =>
        {
            opts.Property(x => x.Line1).HasColumnName("physical_address_line1");
            opts.Property(x => x.Line2).HasColumnName("physical_address_line2");
            opts.Property(x => x.City).HasColumnName("physical_address_city");
            opts.Property(x => x.State).HasColumnName("physical_address_state");
            opts.Property(x => x.PostalCode).HasColumnName("physical_address_postal_code")
                .HasConversion(x => x.Value, x => new PostalCode(x));
            opts.Property(x => x.Country).HasColumnName("physical_address_country");
        }).Navigation(x => x.PhysicalAddress)
            .IsRequired();
    }
}