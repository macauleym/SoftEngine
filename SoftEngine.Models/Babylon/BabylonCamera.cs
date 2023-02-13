namespace SoftEngine.Models.Babylon;

public class BabylonCamera
{
    public string Name          { get; set; }
    public string Id            { get; set; }
    public float[] Position     { get; set; }
    public float[] Target       { get; set; }
    public float Fov            { get; set; }
    public float MinZ           { get; set; }
    public float MaxZ           { get; set; }
    public float Speed          { get; set; }
    public float Inertia        { get; set; }
    public bool CheckCollisions { get; set; }
    public bool ApplyGravity    { get; set; }
    public float[] Ellipsoid    { get; set; }
}
