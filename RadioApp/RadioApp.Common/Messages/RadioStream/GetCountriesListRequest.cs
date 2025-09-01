using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.RadioStream;

/// <summary>
/// Gets countries from DB
/// </summary>
public record GetCountriesListRequest() : IRequest<MyTunerCountryInfo[]>;