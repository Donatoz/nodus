using System;
using System.Windows.Input;
using FlowEditor.Models.Extensions;
using ReactiveUI;

namespace FlowEditor.ViewModels;

public class ContextExtensionViewModel : ReactiveObject
{
    public ICommand RemoveCommand { get; }
    public Type ExtensionType => model.GetType();
    
    private readonly IFlowContextExtension model;
    
    public ContextExtensionViewModel(IFlowContextExtension model, Action onRemove)
    {
        this.model = model;
        RemoveCommand = ReactiveCommand.Create(onRemove);
    }
}