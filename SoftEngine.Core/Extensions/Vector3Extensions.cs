using SharpDX;

namespace SoftEngine.Core.Extensions
{
    public static class Vector3Extensions
    {
        public static bool IsInRangeOf(this Vector3 point, float xMax, float yMax) =>
            point.X >= 0
            && point.Y >= 0
            && point.X < xMax
            && point.Y < yMax;

        public static Vector3 ToNormal(this Vector3 vector3)
        {
            vector3.Normalize();

            return vector3;
        }
    }
}