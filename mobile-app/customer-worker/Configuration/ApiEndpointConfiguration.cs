namespace GTEK.FSM.MobileApp.Configuration;

using Microsoft.Maui.Devices;

public sealed class ApiEndpointConfiguration
{
    public string ApiBaseUrl { get; }

    public ApiEndpointConfiguration()
    {
        var configuredBaseUrl = Environment.GetEnvironmentVariable("GTEK_FSM_API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            ApiBaseUrl = configuredBaseUrl.TrimEnd('/');
            return;
        }

#if DEBUG
        var configuredPort = Environment.GetEnvironmentVariable("GTEK_FSM_API_PORT");
        var port = string.IsNullOrWhiteSpace(configuredPort) ? "5000" : configuredPort;

    // Emulator should use 10.0.2.2; physical Android devices use the host LAN IP.
    string localHost;

if (DeviceInfo.Platform == DevicePlatform.Android)
{
    if (DeviceInfo.DeviceType == DeviceType.Virtual)
    {
        // Android Emulator
        localHost = "10.0.2.2";
    }
    else
    {
        // Physical device via USB (adb reverse)
        localHost = "127.0.0.1";
    }
}
else
{
    // Desktop or other platforms
    localHost = "localhost";
}

        ApiBaseUrl = $"http://{localHost}:{port}";
#else
        ApiBaseUrl = "https://api.gtek-fsm.example.com";
#endif
    }
}
