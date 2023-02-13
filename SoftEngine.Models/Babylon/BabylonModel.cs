namespace SoftEngine.Models.Babylon;

public class BabylonModel
{
    public bool AutoClear              { get; set; }
    public float[] ClearColor          { get; set; }
    public float[] AmbientColor        { get; set; }
    public float[] Gravity             { get; set; }
    public BabylonCamera[] Cameras     { get; set; }
    public string ActiveCamera         { get; set; }
    public BabylonLight[] Lights       { get; set; }
    public BabylonMaterial[] Materials { get; set; }
    public BabylonMesh[] Meshes        { get; set; }
    public object[] MultiMaterials     { get; set; }
}
