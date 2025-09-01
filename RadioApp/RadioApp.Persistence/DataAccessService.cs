using MediatR;
using Microsoft.EntityFrameworkCore;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.RadioStream;
using RadioApp.Common.Messages.Spotify;
using RadioApp.Persistence.Model;

namespace RadioApp.Persistence;

public class DataAccessService :
    IRequestHandler<GetRadioStationsByButtonRequest, RadioStation[]>,
    INotificationHandler<SaveRadioStationNotification>,
    IRequestHandler<GetSpotifySettingsRequest, SpotifySettings>,
    INotificationHandler<SetSpotifySettingsNotification>,
    INotificationHandler<DeleteRadioStationNotification>,
    IRequestHandler<GetCountriesListRequest, MyTunerCountryInfo[]>,
    INotificationHandler<SaveCountriesNotification>,
    IRequestHandler<GetStationsInfosRequest, RadioStationInfo[]>,
    INotificationHandler<SaveStationsInfosNotification>,
    INotificationHandler<CleanUpMuTunerCacheNotification>
{
    private readonly IDbContextFactory<Persistence> _dbContextFactory;

    public DataAccessService(IDbContextFactory<Persistence> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<RadioStation[]> Handle(GetRadioStationsByButtonRequest request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync(cancellationToken);
        var stations = await dbContext.RadioStation.AsNoTracking().Where(s => s.Button == request.Button)
            .ToArrayAsync(cancellationToken);
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
            existingStation.StationDetailsUrl = notification.Station.StationDetailsUrl;
            existingStation.Country = notification.Station.Country;
            existingStation.CountryFlagBase64 = notification.Station.CountryFlagBase64;
            existingStation.SabaFrequency = notification.Station.SabaFrequency;
            existingStation.Button = notification.Station.Button;
            existingStation.Name = notification.Station.Name;
            existingStation.RadioLogoBase64 = notification.Station.RadioLogoBase64;
            existingStation.StreamUrl = notification.Station.StreamUrl;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SpotifySettings> Handle(GetSpotifySettingsRequest request, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync(cancellationToken);
        var settings = await dbContext.SpotifySettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return settings ?? new SpotifySettings();
    }

    public async Task Handle(SetSpotifySettingsNotification notification, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync(cancellationToken);
        var settings = await dbContext.SpotifySettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            await dbContext.AddAsync(notification.SpotifySettings, cancellationToken);
        }
        else
        {
            settings.ClientId = notification.SpotifySettings.ClientId;
            settings.ClientSecret = notification.SpotifySettings.ClientSecret;
            settings.RedirectUrl = notification.SpotifySettings.RedirectUrl;
            settings.AuthToken = notification.SpotifySettings.AuthToken;
            settings.AuthTokenExpiration = notification.SpotifySettings.AuthTokenExpiration;
            settings.RefreshToken = notification.SpotifySettings.RefreshToken;
            settings.DeviceName = notification.SpotifySettings.DeviceName;
            settings.PlaylistName = notification.SpotifySettings.PlaylistName;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(DeleteRadioStationNotification notification, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync(cancellationToken);
        var station = await dbContext.RadioStation.Where(s =>
                s.Button == notification.Button && s.SabaFrequency == notification.SabaFrequency)
            .FirstOrDefaultAsync(cancellationToken);
        if (station is not null)
        {
            dbContext.RadioStation.Remove(station);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<MyTunerCountryInfo[]> Handle(GetCountriesListRequest request, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync(cancellationToken);

        var countries = dbContext.Countries.AsNoTracking().ToArray();
        return countries ?? [];
    }

    public async Task Handle(SaveCountriesNotification notification, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync(cancellationToken);

        dbContext.Countries.RemoveRange(dbContext.Countries);

        await dbContext.Countries.AddRangeAsync(notification.Countries);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RadioStationInfo[]> Handle(GetStationsInfosRequest request, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync(cancellationToken);

        var stations = await dbContext.RadioStationInfos.AsNoTracking()
            .Where(s => s.Country == request.Country && s.StationProcessed).ToArrayAsync(cancellationToken);
        return stations ?? [];
    }

    public async Task Handle(SaveStationsInfosNotification notification, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync(cancellationToken);
        var country = notification.Stations.FirstOrDefault()?.Country;
        if (country == null)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM RadioStationInfos WHERE Country = {country}", cancellationToken: cancellationToken);

        await dbContext.RadioStationInfos.AddRangeAsync(notification.Stations, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }


    public async Task Handle(CleanUpMuTunerCacheNotification notification, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync(cancellationToken);

        dbContext.Countries.RemoveRange(dbContext.Countries);
        dbContext.RadioStationInfos.RemoveRange(dbContext.RadioStationInfos);

        await dbContext.SaveChangesAsync(cancellationToken);
    }


    private RadioStationEntity ConvertToStationEntity(RadioStation station)
    {
        // TODO: Use AutoMapper
        return new RadioStationEntity
        {
            Button = station.Button,
            SabaFrequency = station.SabaFrequency,
            StationDetailsUrl = station.StationDetailsUrl,
            Name = station.Name,
            RadioLogoBase64 = station.RadioLogoBase64,
            StreamUrl = station.StreamUrl,
            Country = station.Country,
            CountryFlagBase64 = station.CountryFlagBase64,
        };
    }
}