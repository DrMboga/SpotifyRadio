namespace RadioApp.Common.Spotify;

public class AvailableDevicesResponse
{
    public string Id { get; set; } =  string.Empty;
    public string Name { get; set; } =  string.Empty;
}

public class AvailableDevicesApiResponse
{
    public AvailableDevicesResponse[]? Devices { get; set; }
}