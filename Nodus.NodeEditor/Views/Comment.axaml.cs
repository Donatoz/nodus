using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.ViewModels;
using ReactiveUI;

namespace Nodus.NodeEditor.Views;

public partial class Comment : GraphElement
{
    protected override Control ContainerControl => Container;
    protected override Control BodyControl => Body;

    private CommentViewModel? viewModel;
    private IDisposable? editModeContract;
    private bool isEditMode;
    
    protected virtual TimeSpan DestroyAnimationDuration => TimeSpan.FromMilliseconds(100);
    
    public Comment()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        viewModel = DataContext as CommentViewModel;
        
        editModeContract?.Dispose();
        editModeContract = viewModel?.IsInEditMode.AlterationStream
            .Subscribe(OnSwitchEditMode);
    }
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.GetCurrentPoint(Container).Properties.IsLeftButtonPressed && e.ClickCount == 2 && DataContext is CommentViewModel vm)
        {
            vm.SwitchEditMode.Execute(null);
        }
    }

    private void OnSwitchEditMode(bool editMode)
    {
        if (isEditMode && !editMode)
        {
            viewModel?.Content.SetValue(ContentTextbox.Text ?? "Comment");
        }

        isEditMode = editMode;
    }
    
    public override void DestroySelf(Action onDestructionComplete)
    {
        Root.SwitchStyleVisibility(false);
        Observable.FromAsync(() => Task.Delay(DestroyAnimationDuration))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Concat(Observable.Return(Unit.Default).Do(_ => onDestructionComplete.Invoke()))
            .Subscribe();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        editModeContract?.Dispose();
    }
}