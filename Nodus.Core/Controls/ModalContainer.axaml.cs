using System;
using System.Diagnostics;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Nodus.Core.Extensions;
using Nodus.Core.ViewModels;

namespace Nodus.Core.Controls;

public partial class ModalContainer : UserControl
{
    private IDisposable? modalStateContract;
    private PointerPoint currentPointerPoint;
    
    public ModalContainer()
    {
        InitializeComponent();
        
        AddHandler(Modal.ModalStateChangedEvent, OnModalStateChanged);
    }

    private void OnModalStateChanged(object? sender, ModalStateEventArgs e)
    {
        if (!e.IsOpened && DataContext is ModalCanvasViewModel vm)
        {
            vm.CloseModal();
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime l)
        {
            l.MainWindow?.AddHandler(Window.PointerMovedEvent, OnPointerMoved);
            l.MainWindow?.AddHandler(PointerPressedEvent, OnPointerPressed);
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        modalStateContract?.Dispose();

        if (DataContext is ModalCanvasViewModel vm)
        {
            modalStateContract = vm.EventStream.OnEvent<ModalStateEvent>(OnModalStateChanged);
        }
    }

    private void OnModalStateChanged(ModalStateEvent evt)
    {
        if (evt.IsOpen)
        {
            ModalDraggable.SetValue(Canvas.LeftProperty, currentPointerPoint.Position.X);
            ModalDraggable.SetValue(Canvas.TopProperty, currentPointerPoint.Position.Y);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        currentPointerPoint = e.GetCurrentPoint(ModalCanvas);
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ModalCanvasViewModel { IsModalOpened: true } vm)
        {
            if (e.Pointer.Captured is Control c && c.HasVisualAncestorOrSelf(ModalDraggable)) return;

            vm.CloseModal();
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        modalStateContract?.Dispose();
    }
}