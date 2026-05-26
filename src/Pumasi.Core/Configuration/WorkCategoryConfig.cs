namespace Pumasi.Core.Configuration;

public sealed class WorkCategoryConfig
{
    public bool Crops { get; set; } = true;
    public bool Machines { get; set; } = true;
    public bool Animals { get; set; }
    public bool Chests { get; set; }
    public bool Planting { get; set; }
}
