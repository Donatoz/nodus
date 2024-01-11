using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using Serilog;
using Serilog.Events;

namespace Nodus.Core.Logging;

public interface INotifiableLogger : ILogger
{
    IObservable<LogEvent> EventStream { get; }
}

public class LoggerWrapper : INotifiableLogger
{
    public IObservable<LogEvent> EventStream => eventSubject;
    
    private readonly ISubject<LogEvent> eventSubject;
    private ILogger wrappedLogger;

    public LoggerWrapper(ILogger wrappedLogger)
    {
        this.wrappedLogger = wrappedLogger;
        eventSubject = new Subject<LogEvent>();
    }
    
    public void Write(LogEvent logEvent)
    {
        wrappedLogger.Write(logEvent);
        eventSubject.OnNext(logEvent);
    }
}