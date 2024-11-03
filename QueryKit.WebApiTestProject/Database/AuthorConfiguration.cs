namespace QueryKit.WebApiTestProject.Database;

using Entities.Authors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    /// <summary>
    /// The database configuration for Authors. 
    /// </summary>
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.Property(o => o.InternalIdentifier)
            .HasDefaultValueSql($"concat('{Consts.DatabaseSequences.AuthorPrefix}', nextval('\"{Consts.DatabaseSequences.AuthorPrefix}\"'))")
            .IsRequired();
    }
}