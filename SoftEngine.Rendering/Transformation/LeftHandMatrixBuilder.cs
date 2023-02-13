using SharpDX;
using SoftEngine.Core.Models;
using SoftEngine.Interfaces.Core.Transformation;

using MatrixDX = SharpDX.Matrix;

namespace SoftEngine.Rendering.Transformation;

public class LeftHandMatrixBuilder : IBuildMatrixTransform
{
    public Matrix BuildViewMatrix(Camera viewSource) =>
        Matrix.LookAtLH(
            viewSource.Position
            , viewSource.Target
            , Vector3.UnitY
        );

    public Matrix BuildProjectionMatrix(float viewSize, float aspect, float nearClip, float farClip) =>
        MatrixDX.PerspectiveFovLH(
            viewSize
            , aspect
            , nearClip
            , farClip
        );

    public Matrix BuildWorldMatrix(Mesh toTransform) =>
        MatrixDX.RotationYawPitchRoll(
            toTransform.Rotation.Y
            , toTransform.Rotation.X
            , toTransform.Rotation.Z
        )
        * MatrixDX.Translation(toTransform.Position);
}
