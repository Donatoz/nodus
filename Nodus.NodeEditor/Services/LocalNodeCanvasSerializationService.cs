using System;
using System.IO;
using System.Linq;
using Avalonia.Platform.Storage;
using Nodus.NodeEditor.Meta.Serialization;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Services;

public class LocalNodeCanvasSerializationService : INodeCanvasSerializationService
{
    protected virtual INodeGraphSerializer Serializer => NodeGraphJsonSerializer.Default;

    protected readonly IStorageProvider storageProvider;

    public LocalNodeCanvasSerializationService(IStorageProvider storageProvider)
    {
        this.storageProvider = storageProvider;
    }
    
    public async void PopulateCanvas(INodeCanvasModel canvas)
    {
        var f = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            { Title = "Open Graph" });

        if (f.Any())
        {
            var s = await File.ReadAllTextAsync(f[0].Path.AbsolutePath);
            var graph = Serializer.Deserialize(s);

            if (graph == null)
            {
                throw new Exception($"Failed to deserialize graph: {s}");
            }
            
            canvas.LoadGraph(graph);
        }
    }

    public async void SaveGraph(INodeCanvasModel canvas)
    {
        var f = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            { DefaultExtension = "json", Title = "Save Graph" });

        if (f != null)
        {
            var serialized = Serializer.Serialize(canvas.SerializeToGraph());

            if (serialized is string s)
            {
                await File.WriteAllTextAsync(f.Path.AbsolutePath, s);
            }
        }
    }
}