using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.RadioStream;

public record GetOneCountryInfoRequest(string Country): IRequest<MyTunerCountryInfo?>;