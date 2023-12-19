using Nodus.Core.Reactive;

namespace Nodus.Core.Selection;

public interface ISelector<T> where T : ISelectable
{
    IReactiveProperty<T?> CurrentlySelected { get; }
    void Select(T selectable);
    void DeselectAll();
}

public class Selector<T> : ISelector<T> where T : ISelectable
{
    private readonly MutableReactiveProperty<T?> currentlySelected;

    public IReactiveProperty<T?> CurrentlySelected => currentlySelected;

    public Selector()
    {
        currentlySelected = new MutableReactiveProperty<T?>();
    }
    
    public void Select(T selectable)
    {
        DeselectAll();
        
        selectable.Select();
        currentlySelected.SetValue(selectable);
    }

    public void DeselectAll()
    {
        currentlySelected.Value?.Deselect();
        currentlySelected.SetValue(default);
    }
}