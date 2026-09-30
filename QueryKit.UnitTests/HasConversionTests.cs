namespace QueryKit.UnitTests;

using Configuration;
using Exceptions;
using FluentAssertions;
using WebApiTestProject.Entities;

public class HasConversionTests
{
    private static readonly Guid KnownGuid = Guid.Parse("ab7afb17-abca-4530-9f8f-bd1497e0f6be");

    [Fact]
    public void can_filter_struct_with_has_conversion()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<WrappedIdRow>(x => x.Id).HasConversion<string>();
        });

        // Act
        var result = WrappedIdRows().ApplyQueryKitFilter("""Id == "2" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Name.Should().Be("two");
    }

    [Fact]
    public void struct_with_query_name_and_has_conversion_throws()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<WrappedIdRow>(x => x.Id).HasQueryName("wrappedid").HasConversion<string>();
        });

        // Act
        var act = () => WrappedIdRows().ApplyQueryKitFilter("""wrappedid == "2" """, config).ToList();

        // Assert
        act.Should().ThrowExactly<ParsingException>()
            .WithInnerExceptionExactly<InvalidOperationException>()
            .WithMessage("Unsupported value '2' for type 'WrappedId'");
    }

    [Fact]
    public void struct_with_has_conversion_configured_before_query_name_throws()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<WrappedIdRow>(x => x.Id).HasConversion<string>().HasQueryName("wrappedid");
        });

        // Act
        var act = () => WrappedIdRows().ApplyQueryKitFilter("""wrappedid == "2" """, config).ToList();

        // Assert
        act.Should().ThrowExactly<ParsingException>()
            .WithInnerExceptionExactly<InvalidOperationException>()
            .WithMessage("Unsupported value '2' for type 'WrappedId'");
    }

    [Fact]
    public void can_filter_struct_with_query_name_differing_only_in_case_and_has_conversion()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<WrappedIdRow>(x => x.Id).HasQueryName("ID").HasConversion<string>();
        });

        // Act
        var result = WrappedIdRows().ApplyQueryKitFilter("""ID == "2" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Name.Should().Be("two");
    }

    [Fact]
    public void struct_with_not_equals_query_name_and_has_conversion_throws()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<WrappedIdRow>(x => x.Id).HasQueryName("wrappedid").HasConversion<string>();
        });

        // Act
        var act = () => WrappedIdRows().ApplyQueryKitFilter("""wrappedid != "2" """, config).ToList();

        // Assert
        act.Should().ThrowExactly<ParsingException>()
            .WithInnerExceptionExactly<InvalidOperationException>()
            .WithMessage("Unsupported value '2' for type 'WrappedId'");
    }

    [Fact]
    public void property_path_with_query_name_and_has_conversion_configured_throws()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<WrappedIdRow>(x => x.Id).HasQueryName("wrappedid").HasConversion<string>();
        });

        // Act
        var act = () => WrappedIdRows().ApplyQueryKitFilter("""Id == "2" """, config).ToList();

        // Assert
        act.Should().ThrowExactly<ParsingException>()
            .WithInnerExceptionExactly<InvalidOperationException>()
            .WithMessage("Unsupported value '2' for type 'WrappedId'");
    }

    [Fact]
    public void can_filter_reference_type_with_query_name_differing_only_in_case_and_has_conversion()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<EmailRow>(x => x.Email!).HasQueryName("email").HasConversion<string>();
        });

        // Act
        var result = EmailRows().ApplyQueryKitFilter("""email == "b@x.com" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Email!.Value.Should().Be("b@x.com");
    }

    [Fact]
    public void reference_type_with_query_name_and_has_conversion_throws()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<EmailRow>(x => x.Email!).HasQueryName("mail").HasConversion<string>();
        });

        // Act
        var act = () => EmailRows().ApplyQueryKitFilter("""mail == "b@x.com" """, config).ToList();

        // Assert
        act.Should().ThrowExactly<ParsingException>()
            .WithInnerExceptionExactly<InvalidOperationException>()
            .WithMessage("Unsupported value 'b@x.com' for type 'EmailAddressRecord'");
    }

    [Fact]
    public void nested_property_with_query_name_and_has_conversion_throws()
    {
        // Arrange
        var rows = new List<OwnerRow>
        {
            new() { Owner = new Owner { Contact = new EmailAddressRecord("a@x.com") } },
            new() { Owner = new Owner { Contact = new EmailAddressRecord("b@x.com") } }
        };
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<OwnerRow>(x => x.Owner.Contact!).HasQueryName("contact").HasConversion<string>();
        });

        // Act
        var act = () => rows.ApplyQueryKitFilter("""contact == "b@x.com" """, config).ToList();

        // Assert
        act.Should().ThrowExactly<ParsingException>()
            .WithInnerExceptionExactly<InvalidOperationException>()
            .WithMessage("Unsupported value 'b@x.com' for type 'EmailAddressRecord'");
    }

    [Fact]
    public void child_property_of_converted_parent_with_query_name_compares_the_child()
    {
        // Arrange
        var input = """Email.Value == "a@x.com" """;
        var configWithQueryName = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Email).HasQueryName("mail").HasConversion<string>();
        });
        var configWithoutQueryName = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Email).HasConversion<string>();
        });

        // Act
        var filterWithQueryName = FilterParser.ParseFilter<TestingPerson>(input, configWithQueryName);
        var filterWithoutQueryName = FilterParser.ParseFilter<TestingPerson>(input, configWithoutQueryName);

        // Assert
        filterWithQueryName.ToDisplayString().Should().Be("""x => (x.Email.Value == "a@x.com")""");
        filterWithoutQueryName.ToDisplayString().Should().Be("""x => (x.Email == new EmailAddress("a@x.com"))""");
    }

    [Fact]
    public void can_filter_property_list_with_lowercase_path_and_has_conversion()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<WrappedIdRow>(x => x.Id).HasConversion<string>();
        });

        // Act
        var result = WrappedIdRows().ApplyQueryKitFilter("""(id) == "2" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Name.Should().Be("two");
    }

    [Fact]
    public void nullable_struct_with_has_conversion_throws()
    {
        // Arrange
        var rows = new List<NullableWrappedIdRow>
        {
            new() { Id = new WrappedId(1) },
            new() { Id = new WrappedId(2) },
            new() { Id = null }
        };
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<NullableWrappedIdRow>(x => x.Id!).HasConversion<string>();
        });

        // Act
        var act = () => rows.ApplyQueryKitFilter("""Id == "2" """, config).ToList();

        // Assert
        act.Should().ThrowExactly<ParsingException>()
            .WithInnerExceptionExactly<InvalidOperationException>()
            .WithMessage("The binary operator Equal is not defined for the types*");
    }

    [Fact]
    public void can_filter_null_on_nullable_struct_with_has_conversion()
    {
        // Arrange
        var rows = new List<NullableWrappedIdRow>
        {
            new() { Id = new WrappedId(1) },
            new() { Id = null }
        };
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<NullableWrappedIdRow>(x => x.Id!).HasConversion<string>();
        });

        // Act
        var result = rows.ApplyQueryKitFilter("""Id == null""", config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Id.Should().BeNull();
    }

    [Fact]
    public void null_on_reference_type_with_has_conversion_matches_no_row()
    {
        // Arrange
        var rows = EmailRows();
        rows.Add(new EmailRow { Email = null });
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<EmailRow>(x => x.Email!).HasConversion<string>();
        });

        // Act
        var result = rows.ApplyQueryKitFilter("""Email == null""", config).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void null_on_reference_type_with_query_name_and_has_conversion_throws()
    {
        // Arrange
        var rows = EmailRows();
        rows.Add(new EmailRow { Email = null });
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<EmailRow>(x => x.Email!).HasQueryName("mail").HasConversion<string>();
        });

        // Act
        var act = () => rows.ApplyQueryKitFilter("""mail == null""", config).ToList();

        // Assert
        act.Should().ThrowExactly<ParsingException>()
            .WithInnerExceptionExactly<InvalidOperationException>()
            .WithMessage("Unsupported value 'null' for type 'EmailAddressRecord'");
    }

    [Fact]
    public void guid_with_contains_and_has_conversion_throws()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<GuidRow>(x => x.Id).HasConversion<string>();
        });

        // Act
        var act = () => GuidRows().ApplyQueryKitFilter("""Id @= "ab7afb17" """, config).ToList();

        // Assert
        act.Should().ThrowExactly<ArgumentException>()
            .WithMessage("Expression of type 'System.Guid' cannot be used for parameter of type 'System.String'*");
    }

    [Fact]
    public void can_filter_guid_with_contains_query_name_and_has_conversion()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<GuidRow>(x => x.Id).HasQueryName("identifier").HasConversion<string>();
        });

        // Act
        var result = GuidRows().ApplyQueryKitFilter("""identifier @= "ab7afb17" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Id.Should().Be(KnownGuid);
    }

    private static List<WrappedIdRow> WrappedIdRows() => new()
    {
        new() { Id = new WrappedId(1), Name = "one" },
        new() { Id = new WrappedId(2), Name = "two" }
    };

    private static List<EmailRow> EmailRows() => new()
    {
        new() { Email = new EmailAddressRecord("a@x.com") },
        new() { Email = new EmailAddressRecord("b@x.com") }
    };

    private static List<GuidRow> GuidRows() => new()
    {
        new() { Id = KnownGuid },
        new() { Id = Guid.NewGuid() }
    };

    private readonly record struct WrappedId
    {
        public WrappedId(int value) => Value = value;
        public WrappedId(string value) : this(int.Parse(value)) { }
        public int Value { get; }
    }

    private record EmailAddressRecord(string Value);

    private class WrappedIdRow
    {
        public WrappedId Id { get; set; }
        public string Name { get; set; } = null!;
    }

    private class NullableWrappedIdRow
    {
        public WrappedId? Id { get; set; }
    }

    private class EmailRow
    {
        public EmailAddressRecord? Email { get; set; }
    }

    private class Owner
    {
        public EmailAddressRecord? Contact { get; set; }
    }

    private class OwnerRow
    {
        public Owner Owner { get; set; } = new();
    }

    private class GuidRow
    {
        public Guid Id { get; set; }
    }
}
