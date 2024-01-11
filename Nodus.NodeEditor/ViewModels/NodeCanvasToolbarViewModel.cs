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
    
    protected INodeCanvasModel CanvasModel { get; }
    protected IServiceProvider ServiceProvider { get; }
    protected INodeCanvasViewModel CanvasViewModel { get; }

    // TODO: Try to remove associations with canvas models
    public NodeCanvasToolbarViewModel(IServiceProvider serviceProvider, INodeCanvasModel canvasModel, INodeCanvasViewModel vm)
    {
        ServiceProvider = serviceProvider;
        CanvasModel = canvasModel;
        CanvasViewModel = vm;

        SaveGraphCommand = ReactiveCommand.Create(SaveGraph);
        LoadGraphCommand = ReactiveCommand.Create(LoadGraph);
    }

    private void SaveGraph()
    {
        ServiceProvider.GetRequiredService<INodeCanvasSerializationService>().SaveGraph(CanvasModel);
    }

    private void LoadGraph()
    {
        ServiceProvider.GetRequiredService<INodeCanvasSerializationService>().PopulateCanvas(CanvasModel);
    }
}