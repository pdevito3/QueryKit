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
    public void can_filter_struct_with_query_name_and_has_conversion()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<WrappedIdRow>(x => x.Id).HasQueryName("wrappedid").HasConversion<string>();
        });

        // Act
        var result = WrappedIdRows().ApplyQueryKitFilter("""wrappedid == "2" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Name.Should().Be("two");
    }

    [Fact]
    public void can_filter_struct_with_has_conversion_configured_before_query_name()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<WrappedIdRow>(x => x.Id).HasConversion<string>().HasQueryName("wrappedid");
        });

        // Act
        var result = WrappedIdRows().ApplyQueryKitFilter("""wrappedid == "2" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Name.Should().Be("two");
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
    public void can_filter_struct_with_not_equals_query_name_and_has_conversion()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<WrappedIdRow>(x => x.Id).HasQueryName("wrappedid").HasConversion<string>();
        });

        // Act
        var result = WrappedIdRows().ApplyQueryKitFilter("""wrappedid != "2" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Name.Should().Be("one");
    }

    [Fact]
    public void can_filter_by_property_path_when_query_name_and_has_conversion_are_configured()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<WrappedIdRow>(x => x.Id).HasQueryName("wrappedid").HasConversion<string>();
        });

        // Act
        var result = WrappedIdRows().ApplyQueryKitFilter("""Id == "2" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Name.Should().Be("two");
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
    public void can_filter_reference_type_with_query_name_and_has_conversion()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<EmailRow>(x => x.Email!).HasQueryName("mail").HasConversion<string>();
        });

        // Act
        var result = EmailRows().ApplyQueryKitFilter("""mail == "b@x.com" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Email!.Value.Should().Be("b@x.com");
    }

    [Fact]
    public void can_filter_nested_property_with_query_name_and_has_conversion()
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
        var result = rows.ApplyQueryKitFilter("""contact == "b@x.com" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Owner.Contact!.Value.Should().Be("b@x.com");
    }

    [Fact]
    public void child_property_of_converted_parent_with_query_name_compares_parent()
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
        filterWithQueryName.ToDisplayString().Should().Be("""x => (x.Email == new EmailAddress("a@x.com"))""");
        filterWithQueryName.ToDisplayString().Should().Be(filterWithoutQueryName.ToDisplayString());
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
    public void can_filter_nullable_struct_with_has_conversion()
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
        var result = rows.ApplyQueryKitFilter("""Id == "2" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Id.Should().Be(new WrappedId(2));
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
    public void can_filter_null_on_reference_type_with_has_conversion()
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
        result.Count.Should().Be(1);
        result[0].Email.Should().BeNull();
    }

    [Fact]
    public void can_filter_null_on_reference_type_with_query_name_and_has_conversion()
    {
        // Arrange
        var rows = EmailRows();
        rows.Add(new EmailRow { Email = null });
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<EmailRow>(x => x.Email!).HasQueryName("mail").HasConversion<string>();
        });

        // Act
        var result = rows.ApplyQueryKitFilter("""mail == null""", config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Email.Should().BeNull();
    }

    [Fact]
    public void can_filter_guid_with_contains_and_has_conversion()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<GuidRow>(x => x.Id).HasConversion<string>();
        });

        // Act
        var result = GuidRows().ApplyQueryKitFilter("""Id @= "ab7afb17" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Id.Should().Be(KnownGuid);
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

    [Fact]
    public void can_filter_nullable_struct_with_query_name_and_has_conversion()
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
            config.Property<NullableWrappedIdRow>(x => x.Id!).HasQueryName("wrappedid").HasConversion<string>();
        });

        // Act
        var result = rows.ApplyQueryKitFilter("""wrappedid == "2" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Id.Should().Be(new WrappedId(2));
    }

    [Fact]
    public void can_filter_property_list_with_lowercase_path_query_name_and_has_conversion()
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<WrappedIdRow>(x => x.Id).HasQueryName("wrappedid").HasConversion<string>();
        });

        // Act
        var result = WrappedIdRows().ApplyQueryKitFilter("""(id) == "2" """, config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Name.Should().Be("two");
    }

    [Theory]
    [InlineData("count")]
    [InlineData("Number")]
    public void int_with_has_conversion_compares_the_int(string queryName)
    {
        // Arrange
        var rows = new List<NumberRow>
        {
            new() { Number = 1 },
            new() { Number = 2 }
        };
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<NumberRow>(x => x.Number).HasQueryName(queryName).HasConversion<string>();
        });

        // Act
        var result = rows.ApplyQueryKitFilter($"{queryName} == 2", config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Number.Should().Be(2);
    }

    [Fact]
    public void nullable_int_with_query_name_and_has_conversion_compares_the_int()
    {
        // Arrange
        var rows = new List<NullableNumberRow>
        {
            new() { Number = 1 },
            new() { Number = 2 },
            new() { Number = null }
        };
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<NullableNumberRow>(x => x.Number).HasQueryName("count").HasConversion<string>();
        });

        // Act
        var result = rows.ApplyQueryKitFilter("count == 2", config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Number.Should().Be(2);
    }

    [Theory]
    [InlineData("level")]
    [InlineData("Level")]
    public void enum_with_has_conversion_compares_the_enum(string queryName)
    {
        // Arrange
        var rows = new List<LevelRow>
        {
            new() { Level = LevelKind.Low },
            new() { Level = LevelKind.High }
        };
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<LevelRow>(x => x.Level).HasQueryName(queryName).HasConversion<string>();
        });

        // Act
        var result = rows.ApplyQueryKitFilter($"{queryName} == High", config).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Level.Should().Be(LevelKind.High);
    }

    [Theory]
    [InlineData("identifier")]
    [InlineData("Id")]
    public void guid_with_has_conversion_compares_a_guid_constant(string queryName)
    {
        // Arrange
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<GuidRow>(x => x.Id).HasQueryName(queryName).HasConversion<string>();
        });

        // Act
        var filter = FilterParser.ParseFilter<GuidRow>($"{queryName} == \"{KnownGuid}\"", config);

        // Assert
        filter.ToDisplayString().Should().Be($"x => (x.Id == {KnownGuid})");
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

    private class NumberRow
    {
        public int Number { get; set; }
    }

    private class NullableNumberRow
    {
        public int? Number { get; set; }
    }

    private enum LevelKind
    {
        Low,
        High
    }

    private class LevelRow
    {
        public LevelKind Level { get; set; }
    }
}
