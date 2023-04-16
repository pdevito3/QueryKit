namespace QueryKit.IntegrationTests.Tests;

using Fakes;
using FluentAssertions;
using WebApiTestProject.Features;

public class HelloIntegrationTests : TestBase
{
    [Fact]
    public async Task hello_integration_tests()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakePersonBuilder().Build();
        var fakePersonTwo = new FakePersonBuilder().Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        // Act
        var query = new GetPersonList.Query();
        var people = await testingServiceScope.SendAsync(query);

        // Assert
        people.Count.Should().BeGreaterThanOrEqualTo(2);
    }
}