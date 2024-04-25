using System;
using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using System.Windows.Input;
using Avalonia.Threading;
using DynamicData;
using Nodus.Core.Common;
using Nodus.Core.Logging;
using ReactiveUI;
using Serilog.Events;

namespace Nodus.Core.ViewModels;

public class LogViewModel : PopupViewModel
{
    private ReadOnlyObservableCollection<LogItem> messages;
    public ReadOnlyObservableCollection<LogItem> Messages => messages;
    
    public ICommand ClearCommand { get; }
    
    private readonly IDisposable messageContract;
    private readonly IDisposable eventsListContract;
    private readonly SourceList<LogEvent> eventsList;
    
    protected virtual int MaxMessagesAmount => 150;
    
    public LogViewModel(INotifiableLogger logger)
    {
        eventsList = new SourceList<LogEvent>();
        messageContract = logger.EventStream.Subscribe(OnMessage);

        eventsListContract = eventsList.Connect()
            .Transform(x => new LogItem(x.MessageTemplate.Text, x.Timestamp, x.Level))
            .Bind(out messages)
            .Subscribe();

        ClearCommand = ReactiveCommand.Create(Clear);
    }

    private void OnMessage(LogEvent evt)
    {
        Dispatcher.UIThread.Invoke(() => AddMessage(evt));
    }

    private void AddMessage(LogEvent evt)
    {
        if (eventsList.Count >= MaxMessagesAmount)
        {
            eventsList.RemoveAt(eventsList.Count - 1);
        }
        
        eventsList.Add(evt);
    }

    private void Clear()
    {
        eventsList.Clear();
    }

    protected override ISubject<IEvent> CreateSubject()
    {
        return new Subject<IEvent>();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (!disposing) return;
        
        messageContract.Dispose();
        eventsListContract.Dispose();
        eventsList.Dispose();
    }
}

public readonly struct LogItem
{
    public string Message { get; }
    public DateTimeOffset Timestamp { get; }
    public LogEventLevel Level { get; }

    public LogItem(string message, DateTimeOffset timestamp, LogEventLevel level)
    {
        Message = message;
        Timestamp = timestamp;
        Level = level;
    }
}