using Microsoft.Extensions.Logging;
namespace SchoolERP.UnitTests.TestCommon.Fakes;
public sealed record CapturedLog(LogLevel Level,EventId EventId,string Message,Exception? Exception,IReadOnlyDictionary<string,object?> State);
public sealed class TestLoggerProvider:ILoggerProvider
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<CapturedLog> _entries=new();public IReadOnlyCollection<CapturedLog> Entries=>_entries.ToArray();public ILogger CreateLogger(string categoryName)=>new Logger(_entries);public void Dispose(){}
    private sealed class Logger(System.Collections.Concurrent.ConcurrentQueue<CapturedLog> entries):ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)where TState:notnull=>null;public bool IsEnabled(LogLevel logLevel)=>true;
        public void Log<TState>(LogLevel level,EventId eventId,TState state,Exception? exception,Func<TState,Exception?,string> formatter){var values=state as IEnumerable<KeyValuePair<string,object?>>;entries.Enqueue(new(level,eventId,formatter(state,exception),exception,values?.ToDictionary(x=>x.Key,x=>x.Value)??new Dictionary<string,object?>()));}
    }
}
