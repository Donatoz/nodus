using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using Nodus.Core.Extensions;
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
    IEnumerable<T> CurrentlySelected { get; }
    IObservable<IChangeSet<T>> SelectedStream { get; }

    /// <summary>
    /// Select a value.
    /// </summary>
    /// <param name="selectable">The value to be selected.</param>
    void Select(T selectable);

    /// <summary>
    /// Deselects all items.
    /// </summary>
    void DeselectAll();

    void Deselect(T item);
}

/// <summary>
/// Represents a selector class used to select and deselect items.
/// </summary>
/// <typeparam name="T">The type of the selectable items.</typeparam>
public class Selector<T> : ISelector<T> where T : ISelectable
{
    private readonly ISourceList<T> currentlySelected;

    /// <summary>
    /// The currently selected value.
    /// </summary>
    public IEnumerable<T> CurrentlySelected => currentlySelected.Items;

    public IObservable<IChangeSet<T>> SelectedStream => currentlySelected.Connect();

    public Selector()
    {
        currentlySelected = new SourceList<T>();
    }

    /// <summary>
    /// Clear the current selection and select the specified object.
    /// </summary>
    /// <typeparam name="T">The type of object to be selected</typeparam>
    /// <param name="selectable">The object to be selected</param>
    public void Select(T selectable)
    {
        if (currentlySelected.Items.Contains(selectable)) return;
        
        selectable.Select();
        currentlySelected.Add(selectable);
    }

    /// <summary>
    /// Deselect all currently selected items.
    /// </summary>
    public void DeselectAll()
    {
        currentlySelected.Items.ForEach(x => x.Deselect());
        currentlySelected.Clear();
    }

    public void Deselect(T item)
    {
        if (!currentlySelected.Items.Contains(item)) return;
        
        item.Deselect();
        currentlySelected.Remove(item);
    }

    public void Dispose()
    {
    }
}