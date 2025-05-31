using MediatR;
using Microsoft.EntityFrameworkCore;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.RadioStream;

namespace RadioApp.Persistence;

public class DataAccessService: 
    IRequestHandler<GetButtonsRegionsRequest, RadioRegion[]>,
    INotificationHandler<SetButtonRegionNotification>
{
    private readonly IDbContextFactory<Persistence> _dbContextFactory;

    public DataAccessService(IDbContextFactory<Persistence> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<RadioRegion[]> Handle(GetButtonsRegionsRequest request, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync(cancellationToken);
        var stations = await dbContext.RadioRegion.AsNoTracking().ToArrayAsync(cancellationToken);
        return stations.ToArray<RadioRegion>();
    }

    public async Task Handle(SetButtonRegionNotification notification, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync(cancellationToken);
        var existingRegion = await dbContext.RadioRegion
            .Where(r => r.SabaRadioButton == notification.RadioButtonRegion.SabaRadioButton)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        if (existingRegion is null)
        {
            await dbContext.RadioRegion.AddAsync(new()
            {
                SabaRadioButton = notification.RadioButtonRegion.SabaRadioButton,
                Region = notification.RadioButtonRegion.Region,
            } ,cancellationToken);
        }
        else
        {
            existingRegion.Region = notification.RadioButtonRegion.Region;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}