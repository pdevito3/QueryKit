namespace QueryKit.UnitTests;

using System.Linq.Expressions;
using Bogus;
using Configuration;
using Exceptions;
using FluentAssertions;
using WebApiTestProject.Entities;

public class SortParserTests
{
    [Fact]
    public void test_sort_parser_default_sort_direction()
    {
        // Arrange
        var input = "Title";

        // Act
        var sortExpressions = SortParser.ParseSort<TestingPerson>(input);

        // Assert
        sortExpressions.Should().HaveCount(1);

        var firstSortExpression = sortExpressions[0].Expression;
        var directionOne = sortExpressions[0].IsAscending;
        firstSortExpression.Body.Should().BeOfType<UnaryExpression>();
        GetMemberName(firstSortExpression).Should().Be("Title");
        directionOne.Should().BeTrue();
    }
    
    [Fact]
    public void test_sort_parser_case_insensitive_sort_direction()
    {
        // Arrange
        var input = "Title ASC, Age DESC";

        // Act
        var sortExpressions = SortParser.ParseSort<TestingPerson>(input);

        // Assert
        sortExpressions.Should().HaveCount(2);

        var firstSortExpression = sortExpressions[0].Expression;
        var directionOne = sortExpressions[0].IsAscending;
        firstSortExpression.Body.Should().BeOfType<UnaryExpression>();
        GetMemberName(firstSortExpression).Should().Be("Title");
        directionOne.Should().BeTrue();

        var secondSortExpression = sortExpressions[1].Expression;
        var directionTwo = sortExpressions[1].IsAscending;

        secondSortExpression.Body.Should().BeOfType<UnaryExpression>();
        GetMemberName(secondSortExpression).Should().Be("Age");
        directionTwo.Should().BeFalse();
    }
    
    [Fact]
    public void test_sort_parser_empty_input()
    {
        // Arrange
        var input = "";

        // Act
        var sortExpressions = SortParser.ParseSort<TestingPerson>(input);

        // Assert
        sortExpressions.Should().BeEmpty();
    }
    
    [Fact]
    public void test_sort_parser_whitespace_input()
    {
        // Arrange
        var input = " ";

        // Act
        var sortExpressions = SortParser.ParseSort<TestingPerson>(input);

        // Assert
        sortExpressions.Should().BeEmpty();
    }

    [Fact]
    public void test_sort_parser_single_property()
    {
        // Arrange
        var input = "Title asc";

        // Act
        var sortExpressions = SortParser.ParseSort<TestingPerson>(input);

        // Assert
        sortExpressions.Should().HaveCount(1);
        var sortExpression = sortExpressions[0].Expression;
        var directionOne = sortExpressions[0].IsAscending;
        
        sortExpression.Body.Should().BeOfType<UnaryExpression>();
        GetMemberName(sortExpression).Should().Be("Title");
        directionOne.Should().BeTrue();
    }

    [Fact]
    public void test_sort_parser_multiple_properties()
    {
        // Arrange
        var input = "Title, Age desc";

        // Act
        var sortExpressions = SortParser.ParseSort<TestingPerson>(input);

        // Assert
        sortExpressions.Should().HaveCount(2);

        var firstSortExpression = sortExpressions[0].Expression;
        var directionOne = sortExpressions[0].IsAscending;
        firstSortExpression.Body.Should().BeOfType<UnaryExpression>();
        GetMemberName(firstSortExpression).Should().Be("Title");
        directionOne.Should().BeTrue();

        var secondSortExpression = sortExpressions[1].Expression;
        var directionTwo = sortExpressions[1].IsAscending;
        
        secondSortExpression.Body.Should().BeOfType<UnaryExpression>();
        GetMemberName(secondSortExpression).Should().Be("Age");
        directionTwo.Should().BeFalse();
    }

    [Fact]
    public void test_sort_parser_multiple_properties_with_minus()
    {
        // Arrange
        var input = "Title, -Age";

        // Act
        var sortExpressions = SortParser.ParseSort<TestingPerson>(input);

        // Assert
        sortExpressions.Should().HaveCount(2);

        var firstSortExpression = sortExpressions[0].Expression;
        var directionOne = sortExpressions[0].IsAscending;
        firstSortExpression.Body.Should().BeOfType<UnaryExpression>();
        GetMemberName(firstSortExpression).Should().Be("Title");
        directionOne.Should().BeTrue();

        var secondSortExpression = sortExpressions[1].Expression;
        var directionTwo = sortExpressions[1].IsAscending;
        
        secondSortExpression.Body.Should().BeOfType<UnaryExpression>();
        GetMemberName(secondSortExpression).Should().Be("Age");
        directionTwo.Should().BeFalse();
    }

    [Fact]
    public void can_have_no_space_on_comma()
    {
        // Arrange
        var input = "Title,Age desc";

        // Act
        var sortExpressions = SortParser.ParseSort<TestingPerson>(input);

        // Assert
        sortExpressions.Should().HaveCount(2);

        var firstSortExpression = sortExpressions[0].Expression;
        var directionOne = sortExpressions[0].IsAscending;
        firstSortExpression.Body.Should().BeOfType<UnaryExpression>();
        GetMemberName(firstSortExpression).Should().Be("Title");
        directionOne.Should().BeTrue();

        var secondSortExpression = sortExpressions[1].Expression;
        var directionTwo = sortExpressions[1].IsAscending;
        
        secondSortExpression.Body.Should().BeOfType<UnaryExpression>();
        GetMemberName(secondSortExpression).Should().Be("Age");
        directionTwo.Should().BeFalse();
    }
    
    [Fact]
    public void can_have_custom_prop_name()
    {
        var input = "OfficialTitle, Age desc";
    
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).HasQueryName("OfficialTitle");
        });
        var sortExpression = SortParser.ParseSort<TestingPerson>(input, config);
        
        sortExpression.Should().HaveCount(2);
        GetMemberName(sortExpression[0].Expression).Should().Be("Title");
        GetMemberName(sortExpression[1].Expression).Should().Be("Age");
        sortExpression[0].IsAscending.Should().BeTrue();
        sortExpression[1].IsAscending.Should().BeFalse();
    }

    [Fact]
    public void can_have_custom_child_prop_name()
    {
        var input = "PhysicalAddress.State";
    
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.PhysicalAddress.State).HasQueryName("state");
        });
        var sortExpression = SortParser.ParseSort<TestingPerson>(input, config);
        
        sortExpression.Should().HaveCount(1);
        GetMemberName(sortExpression[0].Expression).Should().Be("PhysicalAddress.State");
        sortExpression[0].IsAscending.Should().BeTrue();
    }

    [Fact]
    public void can_have_child_prop_sort()
    {
        var input = "PhysicalAddress.State";
    
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.PhysicalAddress.State);
        });
        var sortExpression = SortParser.ParseSort<TestingPerson>(input, config);
        
        sortExpression.Should().HaveCount(1);
        GetMemberName(sortExpression[0].Expression).Should().Be("PhysicalAddress.State");
        sortExpression[0].IsAscending.Should().BeTrue();
    }
    
    [Fact]
    public void can_prevent_sort_for_custom_prop()
    {
        var input = "OfficialTitle, Age desc";
    
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title)
                .PreventSort()
                .HasQueryName("OfficialTitle");
        });
        var sortExpression = SortParser.ParseSort<TestingPerson>(input, config);
        
        sortExpression.Should().HaveCount(1);
        GetMemberName(sortExpression[0].Expression).Should().Be("Age");
        sortExpression[0].IsAscending.Should().BeFalse();
    }
    
    [Fact]
    public void can_prevent_sort()
    {
        var input = "Title, Age desc";
    
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title)
                .PreventSort();
        });
        var sortExpression = SortParser.ParseSort<TestingPerson>(input, config);
        
        sortExpression.Should().HaveCount(1);
        GetMemberName(sortExpression[0].Expression).Should().Be("Age");
        sortExpression[0].IsAscending.Should().BeFalse();
    }

    [Fact]
    public void can_throw_error_when_property_not_recognized()
    {
        var faker = new Faker();
        var propertyName = faker.Lorem.Word();
        var input = $"""Title, {propertyName}, Age desc""";
        var act = () => SortParser.ParseSort<TestingPerson>(input);
        act.Should().Throw<SortParsingException>()
            .WithMessage($"Parsing failed during sorting. '{propertyName}' was not recognized.");
    }

    [Fact]
    public void can_throw_error_when_operator_not_recognized()
    {
        var input = $"""++Title, Age desc""";
        var act = () => SortParser.ParseSort<TestingPerson>(input);
        act.Should().Throw<SortParsingException>()
            .WithMessage($"Parsing failed during sorting. '++Title' was not recognized.");
    }

    private string GetMemberName<T>(Expression<Func<T, object>>? expr)
    {
        if (expr == null)
        {
            throw new ArgumentNullException(nameof(expr));
        }

        var body = expr.Body;

        if (body is UnaryExpression unary)
        {
            body = unary.Operand;
        }

        return GetMemberNameFromExpression(body);
    }

    private string GetMemberNameFromExpression(Expression expr)
    {
        // Handle conditional expressions (generated by null-safe navigation)
        // For x.A == null ? null : x.A.B, we want to extract "A.B"
        if (expr is ConditionalExpression conditional)
        {
            // The IfFalse branch contains the actual property access chain
            return GetMemberNameFromExpression(conditional.IfFalse);
        }

        // Handle Convert expressions that wrap the actual value
        if (expr is UnaryExpression { NodeType: ExpressionType.Convert } convertExpr)
        {
            return GetMemberNameFromExpression(convertExpr.Operand);
        }

        if (expr is MemberExpression memberExpr)
        {
            if (memberExpr.Expression?.NodeType == ExpressionType.MemberAccess)
            {
                return GetMemberNameFromExpression(memberExpr.Expression) + "." + memberExpr.Member.Name;
            }

            // Check for nested conditional in the expression
            if (memberExpr.Expression is ConditionalExpression nestedConditional)
            {
                var parentName = GetMemberNameFromExpression(nestedConditional);
                return parentName + "." + memberExpr.Member.Name;
            }

            return memberExpr.Member.Name;
        }

        throw new ArgumentException("Invalid expression type", nameof(expr));
    }

    #region Nullable Navigation Property Tests

    // Test entities for nullable navigation property scenarios
    public class Player
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class MatchPlayer
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Player? Player { get; set; }  // Nullable navigation property
    }

    public class PlayerStat
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int Score { get; set; }
        public MatchPlayer MatchPlayer { get; set; } = new();
    }

    [Fact]
    public void in_memory_sort_handles_null_navigation_property_gracefully()
    {
        // Arrange - Create data where MatchPlayer.Player is null for some items
        var stats = new List<PlayerStat>
        {
            new() { Score = 10, MatchPlayer = new MatchPlayer { FirstName = "Alice", Player = new Player { LastName = "Anderson" } } },
            new() { Score = 20, MatchPlayer = new MatchPlayer { FirstName = "Bob", Player = null } }, // null Player!
            new() { Score = 30, MatchPlayer = new MatchPlayer { FirstName = "Charlie", Player = new Player { LastName = "Clark" } } },
        };

        // Act - Sorting by MatchPlayer.Player.LastName should handle null Player gracefully
        var sorted = stats.ApplyQueryKitSort("MatchPlayer.Player.LastName asc").ToList();

        // Assert - Should not throw, nulls should sort first in ASC order
        sorted.Count.Should().Be(3);
        // Null values sort first, then alphabetically: null (Bob), Anderson (Alice), Clark (Charlie)
        sorted[0].MatchPlayer.FirstName.Should().Be("Bob");     // null Player -> null LastName sorts first
        sorted[1].MatchPlayer.FirstName.Should().Be("Alice");   // Anderson
        sorted[2].MatchPlayer.FirstName.Should().Be("Charlie"); // Clark
    }

    [Fact]
    public void in_memory_sort_works_with_derived_property_null_handling()
    {
        // Arrange - The user's workaround uses a derived property with null coalescing
        var stats = new List<PlayerStat>
        {
            new() { Score = 10, MatchPlayer = new MatchPlayer { FirstName = "Alice", LastName = "Direct_A", Player = new Player { LastName = "Anderson" } } },
            new() { Score = 20, MatchPlayer = new MatchPlayer { FirstName = "Bob", LastName = "Direct_B", Player = null } }, // null Player - should use MatchPlayer.LastName
            new() { Score = 30, MatchPlayer = new MatchPlayer { FirstName = "Charlie", LastName = null, Player = new Player { LastName = "Clark" } } }, // null MatchPlayer.LastName - should use Player.LastName
        };

        var config = new QueryKitConfiguration(c =>
        {
            // User's workaround pattern: coalesce MatchPlayer.LastName with Player.LastName
            c.DerivedProperty<PlayerStat>(x =>
                x.MatchPlayer.LastName ??
                (x.MatchPlayer.Player != null ? x.MatchPlayer.Player.LastName : "") ?? "")
                .HasQueryName("playerLastName");
        });

        // Act - Should work without throwing
        var sorted = stats.ApplyQueryKitSort("playerLastName asc", config).ToList();

        // Assert
        sorted.Count.Should().Be(3);
        // Order: "Clark", "Direct_A", "Direct_B" (alphabetical by coalesced last name)
        sorted[0].MatchPlayer.FirstName.Should().Be("Charlie"); // Clark
        sorted[1].MatchPlayer.FirstName.Should().Be("Alice");   // Direct_A
        sorted[2].MatchPlayer.FirstName.Should().Be("Bob");     // Direct_B
    }

    #endregion
}