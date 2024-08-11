using System.Numerics;
using Nodus.RenderEngine.Common;
using Silk.NET.Assimp;
using File = System.IO.File;

namespace Nodus.RenderEngine.Serialization;

/// <summary>
/// Represents a factory for creating geometry.
/// </summary>
public interface IGeometryFactory
{
    /// <summary>
    /// Creates geometry primitives from a byte array representing the geometry data.
    /// </summary>
    /// <param name="data">The byte array containing the geometry data.</param>
    /// <returns>An enumerable collection of <see cref="IGeometryPrimitive"/>.</returns>
    IEnumerable<IGeometryPrimitive> CreateFromMemory(ReadOnlySpan<byte> data);
}

public static class GeometryFactoryExtensions
{
    /// <summary>
    /// Creates geometry primitives from a file located at the specified path.
    /// </summary>
    /// <param name="path">The path to the file containing the geometry data.</param>
    /// <returns>An enumerable collection of <see cref="IGeometryPrimitive"/>.</returns>
    public static IEnumerable<IGeometryPrimitive> CreateFromFile(this IGeometryFactory factory, string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Failed to load geometry: file not found at path: {path}");
        }
        
        return factory.CreateFromMemory(File.ReadAllBytes(path));
    }
}

/// <summary>
/// Represents a factory for creating geometry using the Assimp library.
/// </summary>
public sealed unsafe class AssimpGeometryFactory : IGeometryFactory
{
    private readonly Assimp api;
    private readonly uint postProcess;
    
    public AssimpGeometryFactory(Assimp? api = null, PostProcessSteps? postProcess = null)
    {
        this.api = api ?? Assimp.GetApi();
        this.postProcess = (uint)(postProcess ?? PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs);
    }
    
    public IEnumerable<IGeometryPrimitive> CreateFromMemory(ReadOnlySpan<byte> data)
    {
        var buffer = new List<IGeometryPrimitive>();

        fixed (byte* b = data)
        {
            var scene = api.ImportFileFromMemory(b, (uint)data.Length, postProcess, (byte*)null);
            
            ProcessSceneNode(scene->MRootNode, scene, buffer);
        }

        return buffer;
    }
    
    private void ProcessSceneNode(Node* node, Scene* scene, ICollection<IGeometryPrimitive> buffer)
    {
        for (var i = 0; i < node->MNumMeshes; i++)
        {
            buffer.Add(CreateMeshGeometry(scene->MMeshes[node->MMeshes[i]]));
        }

        for (var i = 0; i < node->MNumChildren; i++)
        {
            ProcessSceneNode(node->MChildren[i], scene, buffer);
        }
    }

    private IGeometryPrimitive CreateMeshGeometry(Mesh* mesh)
    {
        var indices = new List<uint>();
        var vertices = new Vertex[mesh->MNumVertices];

        for (var i = 0; i < mesh->MNumVertices; i++)
        {
            var vertex = new Vertex
            {
                Position = mesh->MVertices[i],
                Normal = mesh->MNormals[i]
            };

            if (mesh->MTextureCoords[0] != null)
            {
                vertex.TexCoord = new Vector2(mesh->MTextureCoords[0][i].X, mesh->MTextureCoords[0][i].Y);
            }
            
            vertices[i] = vertex;
        }

        for (var i = 0; i < mesh->MNumFaces; i++)
        {
            var face = mesh->MFaces[i];

            for (var j = 0; j < face.MNumIndices; j++)
            {
                indices.Add(face.MIndices[j]);
            }
        }

        return new GeometryPrimitive(vertices, indices.ToArray());
    }
}