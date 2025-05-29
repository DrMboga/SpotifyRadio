using MediatR;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.RadioStream;
using RadioApp.Persistence.Model;

namespace RadioApp.Persistence;

public class DataAccessService: IRequestHandler<GetButtonsRegionsRequest, RadioRegion[]>
{
    public Task<RadioRegion[]> Handle(GetButtonsRegionsRequest request, CancellationToken cancellationToken)
    {
        RadioRegionEntity[] stations =
        [
            new()
            {
                Id = 1,
                Region = "Hey ho",
                SabaRadioButton = SabaRadioButtons.K
            }
        ];
        return Task.FromResult(stations.ToArray<RadioRegion>());
    }
}