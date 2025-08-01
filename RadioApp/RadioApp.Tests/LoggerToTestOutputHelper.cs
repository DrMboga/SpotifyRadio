using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace RadioApp.Tests;

public static class LoggerToTestOutputHelper
{
    public static void RegisterTestOutputHelper<T>(this Mock<ILogger<T>> loggerMock, ITestOutputHelper output) where T : class
    {
        loggerMock.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()))
            .Callback((LogLevel level, EventId id, object state, Exception ex, Delegate formatter) =>
            {
                var message = formatter.DynamicInvoke(state, ex);
                var now = DateTimeOffset.Now;
                output.WriteLine($"[{now:HH:mm:ss.fff} {level}] |{typeof(T).Name}|: {message}");
                if (ex != null)
                {
                    output.WriteLine(ex.ToString());
                }
            });
    }
}