namespace QueryKit.UnitTests;

using FluentAssertions;
using QueryKit.WebApiTestProject.Entities.Recipes;
using SharedTestingHelper.Fakes.Recipes;

public class EnumerableFilteringTests()
{
    [Fact]
    public async Task can_filter_enumerable()
    {
        // Arrange
        var recipeOne = new FakeRecipeBuilder().Build();
        var recipeTwo = new FakeRecipeBuilder().Build();
        var listOfRecipes = new List<Recipe> { recipeOne, recipeTwo };

        var input = $"""{nameof(Recipe.Title)} == "{recipeOne.Title}" """;

        // Act
        var filteredRecipes = listOfRecipes.ApplyQueryKitFilter(input).ToList();

        // Assert
        filteredRecipes.Count.Should().Be(1);
        filteredRecipes[0].Id.Should().Be(recipeOne.Id);
    }

    [Fact]
    public void case_insensitive_contains_handles_null_string_values()
    {
        // Arrange - This test reproduces the bug reported:
        // @=* operator on nullable string throws when value is null
        var items = new List<PersonWithNullableEmail>
        {
            new() { Name = "Alice", Email = null },
            new() { Name = "Bob", Email = "bob@gmail.com" },
            new() { Name = "Carol", Email = "carol@yahoo.com" }
        };

        var filter = """Email @=* "gmail" """;

        // Act & Assert - should not throw and should return only Bob
        var result = items.ApplyQueryKitFilter(filter).ToList();

        result.Count.Should().Be(1);
        result[0].Name.Should().Be("Bob");
    }

    [Fact]
    public void case_insensitive_equals_handles_null_string_values()
    {
        // Arrange
        var items = new List<PersonWithNullableEmail>
        {
            new() { Name = "Alice", Email = null },
            new() { Name = "Bob", Email = "bob@gmail.com" },
            new() { Name = "Carol", Email = "BOB@GMAIL.COM" }
        };

        var filter = """Email ==* "bob@gmail.com" """;

        // Act & Assert - should not throw and should return Bob and Carol
        var result = items.ApplyQueryKitFilter(filter).ToList();

        result.Count.Should().Be(2);
        result.Should().Contain(p => p.Name == "Bob");
        result.Should().Contain(p => p.Name == "Carol");
    }

    [Fact]
    public void case_insensitive_starts_with_handles_null_string_values()
    {
        // Arrange
        var items = new List<PersonWithNullableEmail>
        {
            new() { Name = "Alice", Email = null },
            new() { Name = "Bob", Email = "bob@gmail.com" },
            new() { Name = "Carol", Email = "carol@yahoo.com" }
        };

        var filter = """Email _=* "bob" """;

        // Act & Assert
        var result = items.ApplyQueryKitFilter(filter).ToList();

        result.Count.Should().Be(1);
        result[0].Name.Should().Be("Bob");
    }

    [Fact]
    public void case_insensitive_ends_with_handles_null_string_values()
    {
        // Arrange
        var items = new List<PersonWithNullableEmail>
        {
            new() { Name = "Alice", Email = null },
            new() { Name = "Bob", Email = "bob@gmail.com" },
            new() { Name = "Carol", Email = "carol@yahoo.com" }
        };

        var filter = """Email _-=* "gmail.com" """;

        // Act & Assert
        var result = items.ApplyQueryKitFilter(filter).ToList();

        result.Count.Should().Be(1);
        result[0].Name.Should().Be("Bob");
    }

    [Fact]
    public void case_insensitive_not_contains_handles_null_string_values()
    {
        // Arrange
        var items = new List<PersonWithNullableEmail>
        {
            new() { Name = "Alice", Email = null },
            new() { Name = "Bob", Email = "bob@gmail.com" },
            new() { Name = "Carol", Email = "carol@yahoo.com" }
        };

        var filter = """Email !@=* "gmail" """;

        // Act & Assert - should return Alice (null) and Carol (yahoo)
        var result = items.ApplyQueryKitFilter(filter).ToList();

        result.Count.Should().Be(2);
        result.Should().Contain(p => p.Name == "Alice");
        result.Should().Contain(p => p.Name == "Carol");
    }

    [Fact]
    public void case_insensitive_not_equals_handles_null_string_values()
    {
        // Arrange
        var items = new List<PersonWithNullableEmail>
        {
            new() { Name = "Alice", Email = null },
            new() { Name = "Bob", Email = "bob@gmail.com" },
            new() { Name = "Carol", Email = "carol@yahoo.com" }
        };

        var filter = """Email !=* "bob@gmail.com" """;

        // Act & Assert - should return Alice (null != value) and Carol
        var result = items.ApplyQueryKitFilter(filter).ToList();

        result.Count.Should().Be(2);
        result.Should().Contain(p => p.Name == "Alice");
        result.Should().Contain(p => p.Name == "Carol");
    }

    [Fact]
    public void case_insensitive_in_handles_null_string_values()
    {
        // Arrange
        var items = new List<PersonWithNullableEmail>
        {
            new() { Name = "Alice", Email = null },
            new() { Name = "Bob", Email = "bob@gmail.com" },
            new() { Name = "Carol", Email = "carol@yahoo.com" }
        };

        var filter = """Email ^^* ["bob@gmail.com", "alice@test.com"] """;

        // Act & Assert
        var result = items.ApplyQueryKitFilter(filter).ToList();

        result.Count.Should().Be(1);
        result[0].Name.Should().Be("Bob");
    }

    [Fact]
    public void case_insensitive_not_in_handles_null_string_values()
    {
        // Arrange
        var items = new List<PersonWithNullableEmail>
        {
            new() { Name = "Alice", Email = null },
            new() { Name = "Bob", Email = "bob@gmail.com" },
            new() { Name = "Carol", Email = "carol@yahoo.com" }
        };

        var filter = """Email !^^* ["bob@gmail.com"] """;

        // Act & Assert - should return Alice (null) and Carol
        var result = items.ApplyQueryKitFilter(filter).ToList();

        result.Count.Should().Be(2);
        result.Should().Contain(p => p.Name == "Alice");
        result.Should().Contain(p => p.Name == "Carol");
    }

    [Fact]
    public void quoted_string_literal_matching_a_property_name_is_treated_as_a_literal()
    {
        // Arrange - reproduces the reported bug: filtering for the literal string "id"
        // in the Name property. "id" coincides with the Id property name, so the parser
        // wrongly resolved it to a property-to-property comparison and threw.
        var items = new List<EntityWithIdAndName>
        {
            new() { Id = 1, Name = "the id column" },
            new() { Id = 2, Name = "something else" }
        };

        var filter = """Name @=* "id" """;

        // Act
        var result = items.ApplyQueryKitFilter(filter).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Id.Should().Be(1);
    }

    [Fact]
    public void bare_property_name_on_the_right_is_still_a_property_to_property_comparison()
    {
        // Arrange - guards the property-to-property feature: an unquoted right-hand side
        // that names a property must still compare the two properties.
        var items = new List<EntityWithIdAndName>
        {
            new() { Id = 1, Name = "match", Code = "match" },
            new() { Id = 2, Name = "left", Code = "right" }
        };

        var filter = "Name == Code";

        // Act
        var result = items.ApplyQueryKitFilter(filter).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Id.Should().Be(1);
    }

    private class PersonWithNullableEmail
    {
        public string Name { get; set; } = null!;
        public string? Email { get; set; }
    }

    private class EntityWithIdAndName
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
    }
}
