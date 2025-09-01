using MediatR;

namespace RadioApp.Common.Messages.RadioStream;

public record CleanUpMuTunerCacheNotification() : INotification;