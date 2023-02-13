namespace SoftEngine.Models.Babylon;

public class BabylonLight
{
    public string Name { get; set; }
    public string Id { get; set; }
    public float Type { get; set; }
    public float[] Data { get; set; }
    public float Intensity { get; set; }
    public float[] Diffuse { get; set; }
    public float[] Specular { get; set; }
}
