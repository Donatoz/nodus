namespace Nodus.Core.Common;

public interface IEvent
{
    private readonly struct EmptyEvent : IEvent { }

    public static IEvent Empty { get; } = new EmptyEvent();
}