namespace QueryKit.Net6Tests;

using FluentAssertions;

public class TimeOnlyFilterTests
{
    private class Shift
    {
        public string Name { get; set; } = "";
        public TimeOnly? Start { get; set; }
    }

    [Fact]
    public void runs_on_the_net6_runtime()
    {
        Environment.Version.Major.Should().Be(6);
    }

    // The TimeOnly constructor with microseconds needs .NET 7. On net6.0 the value is a constant.
    [Fact]
    public void time_only_value_filters_on_net6()
    {
        var shifts = new[]
        {
            new Shift { Name = "early", Start = new TimeOnly(8, 30) },
            new Shift { Name = "late", Start = new TimeOnly(9, 0) },
        };

        var result = shifts.AsQueryable().ApplyQueryKitFilter("Start == \"08:30:00\"").ToList();

        result.Select(x => x.Name).Should().Equal("early");
    }

    [Fact]
    public void time_only_value_keeps_its_fraction_on_net6()
    {
        var shifts = new[]
        {
            new Shift { Name = "whole", Start = new TimeOnly(8, 30) },
            new Shift { Name = "fraction", Start = new TimeOnly(8, 30, 0, 500) },
        };

        var result = shifts.AsQueryable().ApplyQueryKitFilter("Start == \"08:30:00.5\"").ToList();

        result.Select(x => x.Name).Should().Equal("fraction");
    }
}
