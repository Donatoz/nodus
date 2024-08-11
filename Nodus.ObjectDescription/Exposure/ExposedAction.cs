namespace Nodus.ObjectDescriptor;

public class ExposedAction : IExposedMethod
{
    public ExposureHeader Header { get; }

    private readonly Func<object?[]?, object?> context;

    public ExposedAction(ExposureHeader header, Func<object?[]?, object?> context)
    {
        Header = header;

        this.context = context;
    }
    
    public object? Invoke(object?[]? args = null)
    {
        return context.Invoke(args);
    }
}