using System.Text.Json;
using MediatR;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.RadioStream;

namespace RadioApp.Persistence;

public class MyTunerDbService: 
    IRequestHandler<GetRadioRegionsListRequest, string[]>,
    IRequestHandler<GetRadioStationsListByRegionRequest, RadioStationInfo[]>
{
    public Task<string[]> Handle(GetRadioRegionsListRequest request, CancellationToken cancellationToken)
    {
        var myTunerDb = ReadMyTunerData();
        return myTunerDb == null 
            ? Task.FromResult<string[]>([]) 
            : Task.FromResult(myTunerDb.Select(x => x.Region).Distinct().ToArray());
    }

    public Task<RadioStationInfo[]> Handle(GetRadioStationsListByRegionRequest request, CancellationToken cancellationToken)
    {
        var myTunerDb = ReadMyTunerData();
        return myTunerDb == null 
            ? Task.FromResult<RadioStationInfo[]>([]) 
            : Task.FromResult(myTunerDb.Where(r => r.Region == request.Region).ToArray());
    }

    private static RadioStationInfo[]? ReadMyTunerData()
    {
        var myTunerDbFileName = Path.Combine(Directory.GetCurrentDirectory(), "data", "RadioStationsMyTuner.json");
        if (!File.Exists(myTunerDbFileName))
        {
            return [];
        }
        var myTunerStationsJson = File.ReadAllText(myTunerDbFileName);
        return JsonSerializer.Deserialize<RadioStationInfo[]>(myTunerStationsJson);
    }
}