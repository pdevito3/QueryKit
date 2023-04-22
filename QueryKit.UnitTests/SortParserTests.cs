namespace QueryKit.UnitTests;

using System.Linq.Expressions;
using FluentAssertions;
using WebApiTestProject.Entities;

public class SortParserTests
{
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
    public void can_have_custom_child_prop_name()
    {
        var input = "OfficialTitle, Age desc";
    
        var config = new QueryKitProcessorConfiguration(config =>
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
    public void can_prevent_sort_for_custom_prop()
    {
        var input = "OfficialTitle, Age desc";
    
        var config = new QueryKitProcessorConfiguration(config =>
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
    
        var config = new QueryKitProcessorConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title)
                .PreventSort();
        });
        var sortExpression = SortParser.ParseSort<TestingPerson>(input, config);
        
        sortExpression.Should().HaveCount(1);
        GetMemberName(sortExpression[0].Expression).Should().Be("Age");
        sortExpression[0].IsAscending.Should().BeFalse();
    }

    private string GetMemberName<T>(Expression<Func<T, object>>? expr)
    {
        var body = expr.Body;

        if (body is UnaryExpression unary)
        {
            body = unary.Operand;
        }

        return (body as MemberExpression)?.Member.Name;
    }
}