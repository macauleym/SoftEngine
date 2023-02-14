using SharpDX;

namespace SoftEngine.Models;

public class Mesh
{
    public string Name        { get; set; }
    public Color4 Color       { get; set; }
    public Vertex[] Vertices { get; set; }
    public Face[] Faces       { get; set; }
    public Vector3 Position   { get; set; }
    public Vector3 Rotation   { get; set; }

    public Mesh(
      string name
    , Color4 color
    , int verticesCount
    , int facesCount)
    {
        Name     = name;
        Color    = color;
        Vertices = new Vertex[verticesCount];
        Faces    = new Face[facesCount];
    }
}

// Sample creating a cube
//var mesh = new Mesh("Cube", 8);
//mesh.Vertices[0] = new Vector3(-1, 1, 1);
//mesh.Vertices[1] = new Vector3(1, 1, 1);
//mesh.Vertices[2] = new Vector3(-1, -1, 1);
//mesh.Vertices[3] = new Vector3(-1, -1, -1);
//mesh.Vertices[4] = new Vector3(-1, 1, -1);
//mesh.Vertices[5] = new Vector3(1, 1, -1);
//mesh.Vertices[6] = new Vector3(1, -1, 1);
//mesh.Vertices[7] = new Vector3(1, -1, -1); 
