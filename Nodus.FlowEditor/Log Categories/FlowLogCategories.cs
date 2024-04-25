using Nodus.Core.Logging;
using Serilog;
using Serilog.Events;

namespace FlowEditor;

public interface IFlowLogger : INotifiableLogger { }

internal sealed class FlowLoggerWrapper : LoggerNotifiableWrapper, IFlowLogger
{
    public FlowLoggerWrapper(ILogger wrappedLogger) : base(wrappedLogger)
    {
    }
}