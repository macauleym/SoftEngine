namespace SoftEngine.Models.Babylon;

public class BabylonMaterial
{
    public string Name { get; set; }
    public string Id { get; set; }
    public float[] Ambient { get; set; }
    public float[] Diffuse { get; set; }
    public float[] Specular { get; set; }
    public float SpecularPower { get; set; }
    public float[] Emissive { get; set; }
    public float Alpha { get; set; }
    public bool BackFaceCulling { get; set; }
}