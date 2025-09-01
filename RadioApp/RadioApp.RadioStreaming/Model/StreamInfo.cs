namespace RadioApp.RadioStreaming.Model;

public record StreamInfo(
    string Url,
    string? Format = null,
    string? Codec = null,
    int? BitrateKbps = null,
    string? Referer = null);