using AlertHawk.Monitoring.Infrastructure.Utils;

namespace AlertHawk.Monitoring.Tests.UtilsTests;

public class VariableUtilsTests
{
    [Fact]
    public void Should_Return_Env_Variable()
    {
        // Arrange
        Environment.SetEnvironmentVariable("variableBool", "true");
        Environment.SetEnvironmentVariable("variableInt", "1");

        // Act
        var varbool = VariableUtils.GetBoolEnvVariable("variableBool");
        var varInt = VariableUtils.GetIntEnvVariable("variableInt");

        var invalidBool = VariableUtils.GetBoolEnvVariable("non_existent_variable");
        var invalidInt = VariableUtils.GetIntEnvVariable("non_existent_variable");

        // Assert
        Assert.True(varbool);
        Assert.Equal(1, varInt);
        Assert.False(invalidBool);
        Assert.Null(invalidInt);
    }
}