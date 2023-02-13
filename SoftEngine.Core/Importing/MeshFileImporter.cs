using Newtonsoft.Json;
using SharpDX;
using SoftEngine.Interfaces.Core.Importing;
using SoftEngine.Models;
using SoftEngine.Models.Babylon;

namespace SoftEngine.Core.Importing;

public class MeshFileImporter : ILoadMesh
{
    static Mesh ConvertBabylonModel(BabylonMesh babylonMesh)
    {
        var verticesArray = babylonMesh.Vertices;

        // Faces
        var indicesArray = babylonMesh.Indices;

        var uvCount = babylonMesh.UvCount;

        // Depending on the number of texture coords per vertex, we're jumping
        // in the vertices array by 6, 8, and 10 frames.
        var verticesStep = (int)uvCount switch
        { 0 => 6
        , 1 => 8
        , 2 => 10
        , _ => 1
        };

        // The number of intersecting vertices.
        var verticesCount = verticesArray.Length / verticesStep;

        // Number of faces, logically the size of the array divided by 3.
        // (A; B; C)
        var facesCount = indicesArray.Length / 3;
        var mesh = new Mesh(
              babylonMesh.Name
            , new Color4()
            , verticesCount
            , facesCount
        );
            
        // Get the position set by Blender.
        var position = babylonMesh.Position;
        mesh.Position = new Vector3(
          position[0]
        , position[1]
        , position[2]
        );

        // Fill in the vertices array first.
        for (var vertIndex = 0; vertIndex < verticesCount; vertIndex++)
        {
            var x = verticesArray[vertIndex * verticesStep    ];
            var y = verticesArray[vertIndex * verticesStep + 1];
            var z = verticesArray[vertIndex * verticesStep + 2];

            mesh.Vertices[vertIndex] = new Vector3(x, y, z);
        }

        // Now fill in the faces.
        for (var faceIndex = 0; faceIndex < facesCount; faceIndex++)
        {
            var a = (int)indicesArray[faceIndex * 3    ];
            var b = (int)indicesArray[faceIndex * 3 + 1];
            var c = (int)indicesArray[faceIndex * 3 + 2];

            mesh.Faces[faceIndex] = new Face { A = a, B = b, C = c };
        }
        
        return mesh;
    }
    
    public async Task<Mesh[]> LoadJsonFileAsync(string fileName)
    {
        var file = $@"E:\GitHub\SoftEngine\Meshes\{fileName}";
        var data = await File.ReadAllTextAsync(file);
        var babylonModel = JsonConvert.DeserializeObject<BabylonModel>(data);
        if (babylonModel == null)
            return Array.Empty<Mesh>();

        return babylonModel.Meshes
            .Select(ConvertBabylonModel)
            .ToArray();
    }
}