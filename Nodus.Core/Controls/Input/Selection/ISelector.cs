using System;
using Nodus.Core.Reactive;

namespace Nodus.Core.Selection;

/// <summary>
/// Represents a selector for objects of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of objects to select.</typeparam>
public interface ISelector<T> : IDisposable where T : ISelectable
{
    /// <summary>
    /// The currently selected value.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <returns>The reactive property representing the currently selected value.</returns>
    IReactiveProperty<T?> CurrentlySelected { get; }

    /// <summary>
    /// Select a value.
    /// </summary>
    /// <param name="selectable">The value to be selected.</param>
    void Select(T selectable);

    /// <summary>
    /// Deselects all items.
    /// </summary>
    void DeselectAll();
}

/// <summary>
/// Represents a selector class used to select and deselect items.
/// </summary>
/// <typeparam name="T">The type of the selectable items.</typeparam>
public class Selector<T> : ISelector<T> where T : ISelectable
{
    private readonly MutableReactiveProperty<T?> currentlySelected;

    /// <summary>
    /// The currently selected value.
    /// </summary>
    public IReactiveProperty<T?> CurrentlySelected => currentlySelected;

    public Selector()
    {
        currentlySelected = new MutableReactiveProperty<T?>();
    }

    /// <summary>
    /// Clear the current selection and select the specified object.
    /// </summary>
    /// <typeparam name="T">The type of object to be selected</typeparam>
    /// <param name="selectable">The object to be selected</param>
    public void Select(T selectable)
    {
        DeselectAll();
        
        selectable.Select();
        currentlySelected.SetValue(selectable);
    }

    /// <summary>
    /// Deselect all currently selected items.
    /// </summary>
    public void DeselectAll()
    {
        currentlySelected.Value?.Deselect();
        currentlySelected.SetValue(default);
    }

    public void Dispose()
    {
        currentlySelected.Dispose();
    }
}