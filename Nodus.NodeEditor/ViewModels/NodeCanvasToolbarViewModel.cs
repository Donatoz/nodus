using System;
using System.Windows.Input;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.Services;
using ReactiveUI;

namespace Nodus.NodeEditor.ViewModels;

public class NodeCanvasToolbarViewModel
{
    public ICommand SaveGraphCommand { get; }
    public ICommand LoadGraphCommand { get; }
    
    protected INodeCanvasModel Model { get; }
    protected IServiceProvider ServiceProvider { get; }

    public NodeCanvasToolbarViewModel(IServiceProvider serviceProvider, INodeCanvasModel model)
    {
        ServiceProvider = serviceProvider;
        Model = model;

        SaveGraphCommand = ReactiveCommand.Create(SaveGraph);
        LoadGraphCommand = ReactiveCommand.Create(LoadGraph);
    }

    private void SaveGraph()
    {
        ServiceProvider.GetRequiredService<INodeCanvasSerializationService>().SaveGraph(Model);
    }

    private void LoadGraph()
    {
        ServiceProvider.GetRequiredService<INodeCanvasSerializationService>().PopulateCanvas(Model);
    }
}