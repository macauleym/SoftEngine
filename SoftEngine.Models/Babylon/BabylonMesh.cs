namespace SoftEngine.Models.Babylon;

public class BabylonMesh
{
    public string Name { get; set; }
    public string Id { get; set; }
    public float[] Position           { get; set; }
    public float[] Rotation           { get; set; }
    public float[] Scaling            { get; set; }
    public bool IsVisible             { get; set; }
    public bool IsEnabled             { get; set; }
    public float BillboardMode        { get; set; }
    public float UvCount              { get; set; }
    public float[] Vertices           { get; set; }
    public float[] Indices            { get; set; }
    public BabylonSubMesh[] SubMeshes { get; set; }
}
