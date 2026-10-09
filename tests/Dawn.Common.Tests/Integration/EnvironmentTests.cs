using Dawn.Common.Extensions;
using FluentAssertions;

namespace Dawn.Common.Tests.Integration;

[TestFixture(TestOf = typeof(EnvironmentEx))]
public class EnvironmentTests
{
    [Test]
    public void Environment_HasAdditionalVariables()
    {
        //Arrange
        
        // Act
        Environment.LoadAdditionalVariables();
        
        // Assert
        Environment.GetEnvironmentVariable("HAS_ENV_VARIABLE")
            .Should()
            .Be("true");
    }

    [Test]
    public void Environment_ShouldTrack_VariablesLoadedFromEnv()
    {
        //Arrange
        
        // Act
        var additionalVariables = Environment.LoadAdditionalVariables();
        
        // Assert
        Environment.AdditionalVariables
            .Should().BeEquivalentTo(additionalVariables);

        additionalVariables
            .Should().HaveCount(1)
            .And
            .Subject.First()
            .Should().Be(new KeyValuePair<string, string>("HAS_ENV_VARIABLE", "true"));
    }
}