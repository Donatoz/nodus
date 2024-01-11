using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nodus.Core.Logging;
using Nodus.Core.Reactive;
using Serilog.Events;

namespace Nodus.Core.ViewModels;

public class LogViewModel : IDisposable
{
    private readonly MutableReactiveProperty<IList<LogEvent>> messages;
    private readonly IDisposable messageContract;

    public IReactiveProperty<IEnumerable<LogEvent>> Messages => messages;

    protected virtual int MaxMessagesAmount => 10;
    
    public LogViewModel(INotifiableLogger logger)
    {
        messages = new MutableReactiveProperty<IList<LogEvent>>(new List<LogEvent>());
        messageContract = logger.EventStream.Subscribe(OnMessage);
    }

    private void OnMessage(LogEvent evt)
    {
        if (messages.Value.Count >= MaxMessagesAmount)
        {
            messages.Value.Remove(messages.Value.Last());
        }
        
        messages.Value.Insert(0, evt);
        messages.SetValue(new List<LogEvent>{evt});
    }

    public void Dispose()
    {
        messageContract.Dispose();
        messages.Dispose();
    }
}