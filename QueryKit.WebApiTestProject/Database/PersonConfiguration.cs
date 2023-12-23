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

        // builder.ComplexProperty(
        //     x => x.PhysicalAddress,
        //     y =>
        //     {
        //         y.Property(e => e.Line1).HasColumnName("physical_address_line1");
        //         y.Property(e => e.Line2).HasColumnName("physical_address_line2");
        //         y.Property(e => e.City).HasColumnName("physical_address_city");
        //         y.Property(e => e.State).HasColumnName("physical_address_state");
        //         y.ComplexProperty(e => e.PostalCode, z
        //             => z.Property(s => s.Value).HasColumnName("physical_address_postal_code"));
        //         y.Property(e => e.Country).HasColumnName("physical_address_country");
        //     });
    }
}