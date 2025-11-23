using Silk.NET.Assimp;
using Silk.NET.Maths;
using VulkanTest.MeshData;

namespace VulkanTest.Import;

public static class ModelLoader
{
    public static unsafe Model LoadModel(string path)
    {
        using var assimp = Assimp.GetApi();
        var scene = assimp.ImportFile(path, (uint)PostProcessPreset.TargetRealTimeMaximumQuality);

        var vertexMap = new Dictionary<Vertex, uint>();
        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        VisitSceneNode(scene->MRootNode, scene, ref vertexMap, ref vertices, ref indices);
        assimp.ReleaseImport(scene);

        return new Model(vertices.ToArray(), indices.ToArray());
    }

    private static unsafe void VisitSceneNode(Node* node, Scene* scene, ref Dictionary<Vertex, uint> vertexMap,
        ref List<Vertex> vertices, ref List<uint> indices)
    {
        for (int m = 0; m < node->MNumMeshes; m++)
        {
            var mesh = scene->MMeshes[node->MMeshes[m]];

            for (int f = 0; f < mesh->MNumFaces; f++)
            {
                var face = mesh->MFaces[f];

                for (int i = 0; i < face.MNumIndices; i++)
                {
                    uint index = face.MIndices[i];

                    var position = mesh->MVertices[index];
                    var texture = mesh->MTextureCoords[0][(int)index];

                    Vertex vertex = new()
                    {
                        Pos = new Vector3D<float>(position.X, position.Y, position.Z),
                        Color = new Vector3D<float>(1, 1, 1),
                        //Flip Y for OBJ in Vulkan
                        TexCoord = new Vector2D<float>(texture.X, 1.0f - texture.Y)
                    };

                    if (vertexMap.TryGetValue(vertex, out var meshIndex))
                    {
                        indices.Add(meshIndex);
                    }
                    else
                    {
                        indices.Add((uint)vertices.Count);
                        vertexMap[vertex] = (uint)vertices.Count;
                        vertices.Add(vertex);
                    }
                }
            }
        }

        for (int c = 0; c < node->MNumChildren; c++)
        {
            VisitSceneNode(node->MChildren[c], scene, ref vertexMap, ref vertices, ref indices);
        }
    }
}