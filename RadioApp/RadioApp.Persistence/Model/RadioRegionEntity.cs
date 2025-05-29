using RadioApp.Common.Contracts;

namespace RadioApp.Persistence.Model;

public class RadioRegionEntity: RadioRegion, IDbEntity
{
    public int Id { get; set; }
}