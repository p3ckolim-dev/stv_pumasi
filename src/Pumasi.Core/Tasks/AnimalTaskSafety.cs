namespace Pumasi.Core.Tasks;

public static class AnimalTaskSafety
{
    public static bool IsAnimalHouseTypeName(string? typeName)
    {
        return string.Equals(typeName, "AnimalHouse", StringComparison.Ordinal);
    }
}
