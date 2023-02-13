using SharpDX;
using SoftEngine.Models;

namespace SoftEngine.Interfaces.Core.Transformation
{
    public interface IBuildMatrixTransform
    {
        Matrix BuildViewMatrix(Camera viewSource);

        Matrix BuildProjectionMatrix(float viewSize, float aspect, float nearClip, float farClip);

        Matrix BuildWorldMatrix(Mesh toTransform);
    }
}
