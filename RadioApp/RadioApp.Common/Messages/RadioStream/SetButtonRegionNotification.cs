using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.RadioStream;

public record SetButtonRegionNotification(RadioRegion RadioButtonRegion): INotification;