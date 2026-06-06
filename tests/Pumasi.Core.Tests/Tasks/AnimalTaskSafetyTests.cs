using Pumasi.Core.Tasks;
using Xunit;

namespace Pumasi.Core.Tests.Tasks;

public sealed class AnimalTaskSafetyTests
{
    [Theory]
    [InlineData("AnimalHouse", true)]
    [InlineData("Farm", false)]
    [InlineData("Greenhouse", false)]
    [InlineData("", false)]
    public void IsAnimalHouseTypeName_AllowsOnlyAnimalHouse(string typeName, bool expected)
    {
        Assert.Equal(expected, AnimalTaskSafety.IsAnimalHouseTypeName(typeName));
    }
}
