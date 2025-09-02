using MediatR;
using Microsoft.AspNetCore.Mvc;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.RadioStream;

namespace RadioApp.RadioStreamSettings;

public static class RadioStreamSettingsEndpoints
{
    public static void MapRadioStreamSettingsEndpoints(this WebApplication app)
    {
        app.MapGet("radio-countries-list", async (IMediator mediator) =>
        {
            var cachedCountries = await mediator.Send(new GetCountriesListRequest());

            if (cachedCountries.Length > 0)
            {
                return cachedCountries;
            }

            var countries = await mediator.Send(new GetMyTunerCountriesListRequest());

            await mediator.Publish(new SaveCountriesNotification(countries));
            return countries;
        }).WithName("List of countries from MyTuner");

        app.MapGet("radio-stations-by-country",
                async (IMediator mediator, [FromQuery] string country) =>
                {
                    var cachedStations = await mediator.Send(new GetStationsInfosRequest(country));
                    return cachedStations;
                })
            .WithName("List of stations by country sorted by rating");

        app.MapPost("radio-stations-cache-by-country",
                async (IMediator mediator, [FromBody] MyTunerStationsRequest country) =>
                {
                    await mediator.Publish(new StartCacheMyTunerStationsByCountryNotification(country));
                })
            .WithName("Starts caching all stations by country from MyTuner to the local DB");

        app.MapGet("radio-stations-cache-by-country-status",
                async (IMediator mediator, [FromQuery] string country) =>
                {
                    var status = await mediator.Send(new GetRadioStationsCachingStatusRequest(country));
                    return status;
                })
            .WithName("Returns status of cashing");

        app.MapDelete("radio-stations-cache",
                async (IMediator mediator) => { await mediator.Publish(new CleanUpMuTunerCacheNotification()); })
            .WithName("Delete MyTuner Cache");

        app.MapGet("radio-stations-by-button", async (SabaRadioButtons button, IMediator mediator) =>
        {
            var stationsList = await mediator.Send(new GetRadioStationsByButtonRequest(button));
            return stationsList;
        }).WithName("RadioStationsListConnectedWithButton");

        app.MapPost("radio-stations-by-button", async (RadioStation station, IMediator mediator) =>
        {
            if (station.RadioLogoBase64 == null)
            {
                // TODO: Find another way of saving image of the station
                // if (radioInfo?.StationImageUrl != null)
                // {
                //     var extension = Path.GetExtension(radioInfo.StationImageUrl).Replace(".", "").ToLower();
                //     using var webClient = new HttpClient();
                //     var imageData = await webClient.GetByteArrayAsync(radioInfo.StationImageUrl);
                //     var imageBase64 = Convert.ToBase64String(imageData);
                //     station.RadioLogoBase64 = $"data:image/{extension};base64,{imageBase64}";
                // }
            }

            await mediator.Publish(new SaveRadioStationNotification(station));
            return station;
        }).WithName("SavesStationInfo");

        app.MapDelete("radio-stations-by-button",
            async (SabaRadioButtons button, int sabaFrequency, IMediator mediator) =>
            {
                await mediator.Publish(new DeleteRadioStationNotification(button, sabaFrequency));
            }).WithName("DeleteStationInfo");

        app.MapGet("radio-buttons", () =>
        {
            SabaRadioButtonInfo[] buttonsToMap =
            [
                new()
                {
                    Button = SabaRadioButtons.L,
                    ButtonLabel = "L",
                    ButtonDescription = "Long waves button which has a L sign on a SABA panel"
                },
                new()
                {
                    Button = SabaRadioButtons.M,
                    ButtonLabel = "M",
                    ButtonDescription = "Middle waves button which has a M sign on a SABA panel"
                },
                new()
                {
                    Button = SabaRadioButtons.K,
                    ButtonLabel = "K",
                    ButtonDescription = "Short waves button which has a K sign on a SABA panel"
                },
                new()
                {
                    Button = SabaRadioButtons.U,
                    ButtonLabel = "U",
                    ButtonDescription = "FM waves button which has an U sign on a SABA panel"
                },
            ];
            return buttonsToMap;
        }).WithName("RadioPanelButtons");
    }
}