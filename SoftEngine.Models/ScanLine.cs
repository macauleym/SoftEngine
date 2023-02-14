namespace SoftEngine.Models;
    
public struct ScanLine
{
    // The current Y position of the scan line.
    public int CurrentY;
    
    // The dot product between the current face
    // normal and the projected light directions. 
    public float NormalDotLightA;
    public float NormalDotLightB;
    public float NormalDotLightC;
    public float NormalDotLightD;
}
