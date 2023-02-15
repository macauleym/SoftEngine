using SharpDX;
using SoftEngine.Models;
using SoftEngine.Interfaces.Core.Transformation;

namespace SoftEngine.Rendering.Transformation;

public class LeftHandMatrixBuilder : IBuildMatrixTransform
{
    public Matrix BuildViewMatrix(Camera viewSource) =>
        Matrix.LookAtLH(
          viewSource.Position
        , viewSource.Target
        , Vector3.UnitY
        );

    public Matrix BuildProjectionMatrix(
      float viewSize
    , float aspect
    , float nearClip
    , float farClip
    ) => Matrix.PerspectiveFovLH(
      viewSize
    , aspect
    , nearClip
    , farClip
    );

    public Matrix BuildWorldMatrix(Mesh toTransform) =>
        Matrix.RotationYawPitchRoll(
          toTransform.Rotation.Y
        , toTransform.Rotation.X
        , toTransform.Rotation.Z
        )
        * Matrix.Translation(toTransform.Position);
}
