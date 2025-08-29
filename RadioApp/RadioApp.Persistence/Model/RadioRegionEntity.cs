using RadioApp.Common.Contracts;

namespace RadioApp.Persistence.Model;

[Obsolete("Delete this entity when restructure database")]
public class RadioRegionEntity: RadioRegion, IDbEntity
{
    public int Id { get; set; }
}