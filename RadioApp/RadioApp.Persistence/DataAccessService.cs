using MediatR;
using Microsoft.EntityFrameworkCore;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.RadioStream;
using RadioApp.Persistence.Model;

namespace RadioApp.Persistence;

public class DataAccessService: 
    IRequestHandler<GetButtonsRegionsRequest, RadioRegion[]>,
    INotificationHandler<SetButtonRegionNotification>,
    IRequestHandler<GetRadioStationsByButtonRequest, RadioStation[]>,
    INotificationHandler<SaveRadioStationNotification>
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

    public async Task<RadioStation[]> Handle(GetRadioStationsByButtonRequest request, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync(cancellationToken);
        var stations = await dbContext.RadioStation.AsNoTracking().ToArrayAsync(cancellationToken);
        return stations.ToArray<RadioStation>();
    }

    public async Task Handle(SaveRadioStationNotification notification, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync(cancellationToken);
        var existingStation = await dbContext.RadioStation
            .Where(s => s.Button == notification.Station.Button
                        && s.SabaFrequency == notification.Station.SabaFrequency)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingStation is null)
        {
            await dbContext.RadioStation.AddAsync(ConvertToStationEntity(notification.Station), cancellationToken);
        }
        else
        {
            existingStation.Region = notification.Station.Region;
            existingStation.SabaFrequency = notification.Station.SabaFrequency;
            existingStation.Button = notification.Station.Button;
            existingStation.Name = notification.Station.Name;
            existingStation.RadioLogoBase64 = notification.Station.RadioLogoBase64;
            existingStation.StreamUrl = notification.Station.StreamUrl;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private RadioStationEntity ConvertToStationEntity(RadioStation station)
    {
        // TODO: Use AutoMapper
        return new RadioStationEntity
        {
            Button = station.Button,
            SabaFrequency = station.SabaFrequency,
            Region = station.Region,
            Name = station.Name,
            RadioLogoBase64 = station.RadioLogoBase64,
            StreamUrl = station.StreamUrl
        };
    }
}