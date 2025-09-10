using RadioApp.Common.PlayerProcessor;

namespace RadioApp.PlayerProcessors;

public class PlayerProcessorDebounceFrequencyService
{
    // Debounce frequency change 
    private const int FrequencyChangeThresholdMilliseconds = 500;
        
    private long _lastFrequencyChangeTime = 0;
    private readonly Lock _frequencyLock = new Lock();

    /// <summary>
    /// Checks debounce time
    /// </summary>
    /// <returns>true if debounce time exceeded, otherwise false</returns>
    public bool CheckLastTime()
    {
        lock (_frequencyLock)
        {
            // The capacitance measurement is not precise. So if the knob is somewhere on frequency measurement borders, it can send new frequency values very often.
            // So, we are debouncing this buzz here
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now - _lastFrequencyChangeTime < FrequencyChangeThresholdMilliseconds)
            {
                return false;
            }

            _lastFrequencyChangeTime = now;
        }
        return true;
    }
}