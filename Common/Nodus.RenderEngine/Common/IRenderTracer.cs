using System.Reactive.Subjects;

namespace Nodus.RenderEngine.Common;

public interface IRenderTracer : IDisposable
{
    IObservable<RenderTraceException> ExceptionStream { get; }
    
    void Throw(Exception exception);
    void PutFrame(IRenderTraceFrame frame);
    void TryWithdrawFrame();
}

public sealed class RenderTraceException : Exception
{
    public RenderTraceException(string message, Exception exception) : base(message, exception)
    {
    }
}

public interface IRenderTraceFrame
{
    string Label { get; }
}

public readonly struct RenderTraceFrame(string label) : IRenderTraceFrame
{
    public string Label { get; } = label;
}

public class StackedRenderTracer : IRenderTracer
{
    public IObservable<RenderTraceException> ExceptionStream => exceptionSubject;

    private readonly Stack<IRenderTraceFrame> frames;
    private readonly Subject<RenderTraceException> exceptionSubject;

    public StackedRenderTracer()
    {
        frames = new Stack<IRenderTraceFrame>();
        exceptionSubject = new Subject<RenderTraceException>();
    }
    
    public void Throw(Exception exception)
    {
        var labels = new List<string>();

        while (frames.Any())
        {
            labels.Add(frames.Pop().Label);
        }

        var traceMessage = $"{Environment.NewLine}Frames:{Environment.NewLine}{string.Join(Environment.NewLine, labels)}";
        
        exceptionSubject.OnNext(new RenderTraceException(traceMessage, exception));
    }

    public void PutFrame(IRenderTraceFrame frame)
    {
        frames.Push(frame);
    }

    public void TryWithdrawFrame()
    {
        if (!frames.Any()) return;
        
        frames.Pop();
    }

    public void Dispose()
    {
        exceptionSubject.Dispose();
    }
}