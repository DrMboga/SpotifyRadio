using RadioApp.Common.Contracts;

namespace RadioApp.Persistence.Model;

public class RadioStationEntity: RadioStation, IDbEntity
{
    public int Id { get; set; }
}