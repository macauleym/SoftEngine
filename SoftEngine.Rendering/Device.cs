using System;
using System.Windows;
using System.Windows.Media.Imaging;
using SharpDX;
using SoftEngine.Core.Models;
using SoftEngine.Core.Extensions;
using SoftEngine.Interfaces.Core.Transformation;

using MatrixDX = SharpDX.Matrix;

namespace SoftEngine.Rendering;

public class Device
{
    // The (4) represents the 4 pixel values; Red, Green, Blue, and Alpha.
    const int pixelValueCount = 4;
    
    // Offset to convert from a 1D based array (the back buffer) 
    // to a 2D array (the screen).
    const int coordOffset = 255;
    
    byte[] backBuffer;
    WriteableBitmap bitMap;
    IBuildMatrixTransform matrixBuilder;
    
    public Device(WriteableBitmap bmp, IBuildMatrixTransform matrixTransformBuilder)
    {
        bitMap = bmp;
        matrixBuilder = matrixTransformBuilder;
        
        // The back buffer's size is the number of pixels we want to draw
        // onto the screen.
        var bufferSize = bitMap.PixelWidth * bitMap.PixelHeight * pixelValueCount;
        backBuffer = new byte[bufferSize];
    }

    /// <summary>
    /// Clears the backbuffer with a color of the given bytes.
    /// </summary>
    /// <param name="r">RED byte</param>
    /// <param name="g">GREEN byte</param>
    /// <param name="b">BLUE byte</param>
    /// <param name="a">ALPHA byte</param>
    public void Clear(byte r, byte g, byte b, byte a)
    {
        for (var i = 0; i < backBuffer.Length; i += 4)
        {
            // Windows uses the BGRA format.
            backBuffer[i] = b;
            backBuffer[i + 1] = g;
            backBuffer[i + 2] = r;
            backBuffer[i + 3] = a;
        }
    }

    /// <summary>
    /// Once we have everything set up, we flush the back buffer
    /// into the front buffer for viewing.
    /// </summary>
    public void Present() =>
        bitMap.WritePixels(
            new Int32Rect
            ( 0
            , 0
            , bitMap.PixelWidth
            , bitMap.PixelHeight
            )
        , backBuffer
        , bitMap.PixelWidth * pixelValueCount
        , 0
        );

    /// <summary>
    /// Put a specific pixel of a given color on the screen
    /// at the given x,y coordinates.
    /// </summary>
    /// <param name="x">X screen coordinate.</param>
    /// <param name="y">Y screen coordinate.</param>
    /// <param name="color">Color to draw the point as.</param>
    public void PutPixel(int x, int y, Color4 color)
    {
        var index = (x + y * bitMap.PixelWidth) * pixelValueCount;

        backBuffer[index]     = (byte)(color.Blue  * coordOffset);
        backBuffer[index + 1] = (byte)(color.Green * coordOffset);
        backBuffer[index + 2] = (byte)(color.Red   * coordOffset);
        backBuffer[index + 3] = (byte)(color.Alpha * coordOffset);
    }

    /// <summary>
    /// Transform 3D coordinates into 2D coordinates.
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="transMatrix"></param>
    /// <returns></returns>
    public Vector3 Project(Vector3 coord, MatrixDX transMatrix)
    {
        // Transform the coordinates.
        var point = Vector3.TransformCoordinate(coord, transMatrix);
        
        // Transformed coordinates will be based on a coordinate
        // system starting in the center of the screen. Drawing normally
        // starts from the top left, so we need to transform again
        // to center the coords in the top left.
        var x = point.X * bitMap.PixelWidth + bitMap.PixelWidth / 2f;
        var y = -point.Y * bitMap.PixelHeight + bitMap.PixelHeight / 2f;

        return new Vector3(x, y, point.Z);
    }

    
    /// <summary>
    /// Calls PutPixel, with the addition of the clipping operation.
    /// </summary>
    /// <param name="point">Point to be drawn.</param>
    /// <param name="color">The color to draw the point as.</param>
    public void DrawPoint(Vector3 point, Color4 color)
    {
        if (point.IsInRangeOf(bitMap.PixelWidth, bitMap.PixelHeight))
            PutPixel(
              (int)point.X
            , (int)point.Y
            , color
            );
    }

    public void DrawLine(Vector3 point1, Vector3 point2, Color4 color)
    {
        var dist = (point2 - point1).Length();
        
        // If the distance between the 2 points
        // is less than 2 pixels, we exit.
        if (dist < 2)
            return;
        
        // Find the midpoint.
        Vector3 midPoint = point1 + (point2 - point1) / 2;
        
        // Draw the midpoint.
        DrawPoint(midPoint, color);
        
        // Recursive operation between first and mid points,
        // and second and mid points.
        DrawLine(point1, midPoint, color);
        DrawLine(midPoint, point2, color);
    }

    public void DrawBLine(Vector3 point1, Vector3 point2, Color4 color)
    {
        var x1 = (int)point1.X;
        var y1 = (int)point1.Y;
        var x2 = (int)point2.X;
        var y2 = (int)point2.Y;

        var dx = Math.Abs(x2 - x1);
        var dy = Math.Abs(y2 - y1);
        var sx = (x1 < x2) ? 1 : -1;
        var sy = (y1 < y2) ? 1 : -1;
        var err = dx - dy;

        for (;;)
        {
            DrawPoint(new Vector3(x1, y1, point1.Z), color);

            if ((x1 == x2) && (y1 == y2))
                break;

            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }
    }
    
    /// <summary>
    /// Draw the scanline between 2 points, from left to right.
    /// (PointA, PointB) -> (PointC, PointD)
    /// Points must be sorted before drawing the line.
    /// </summary>
    /// <param name="currentY">Starting point.</param>
    /// <param name="pointA"></param>
    /// <param name="pointB"></param>
    /// <param name="pointC"></param>
    /// <param name="pointD"></param>
    /// <param name="color">Color to draw line as.</param>
    void ProcessScanLine(
      int currentY
    , Vector3 pointA
    , Vector3 pointB
    , Vector3 pointC
    , Vector3 pointD
    , Color4 color)
    {
        // Using current Y, we can calculate the current gradiant.
        // This can be used to get the start and end X values
        // to draw between.
        // IF pointA.Y == pointB.Y OR pointC.Y == pointD.Y THEN
        // force the gradiant to be `1`.
        var gradiant1 = pointA.Y.CompareWith(pointB.Y, FloatComparison.NotEqual) 
            ? (currentY - pointA.Y) / (pointB.Y - pointA.Y)
            : 1;
        var gradiant2 = pointC.Y.CompareWith(pointD.Y, FloatComparison.NotEqual)
            ? (currentY - pointC.Y) / (pointD.Y - pointC.Y)
            : 1;

        var startX = (int)pointA.X.InterpolateTo(pointB.X, gradiant1);
        var endX   = (int)pointC.X.InterpolateTo(pointD.X, gradiant2);
        
        // Draw a line from left (startX) to right (endX).
        for (var currentX = startX; currentX < endX; currentX++)
            DrawPoint(new Vector3(currentX, currentY, 0f), color);
    }

    public void DrawTriangle(
      Vector3 point1
    , Vector3 point2
    , Vector3 point3
    , Color4 color
    ) {
        // Sorting the points in order always keeps point1, then 2, then 3.
        // This keeps point1 always "up", giving it the lowest possible to
        // be near the top of the screen.
        if (point1.Y > point2.Y)
            (point2, point1) = (point1, point2);

        if (point2.Y > point3.Y)
            (point3, point2) = (point2, point3);

        if (point1.Y > point3.Y)
            (point3, point1) = (point1, point3);
        
        // Sorting done.
        // Now calculate inverse slopes.
        float dPoint1Point2;
        float dPoint1Point3;

        if (point2.Y - point1.Y > 0)
            dPoint1Point2 = (point2.X - point1.X) / (point2.Y - point1.Y);
        else
            dPoint1Point2 = 0;

        if (point3.Y - point1.Y > 0)
            dPoint1Point3 = (point3.X - point1.X) / (point3.Y - point1.Y);
        else
            dPoint1Point3 = 0;
        
        
        // First case where triangles are as follows:
        /* Point1
         * -
         * --
         * - -
         * -  -
         * -   -
         * -    - Point2
         * -   -
         * -  -
         * - -
         * --
         * -
         * Point3
         */
        if (dPoint1Point2 > dPoint1Point3)
            for (var y = (int)point1.Y; y <= (int)point3.Y; y++)
                if (y < point2.Y)
                    ProcessScanLine(y, point1, point3, point1, point2, color);
                else
                    ProcessScanLine(y, point1, point3, point2, point3, color);
        // Case where points are as follows:
        /*             Point1
         *             -
         *            --
         *           - -
         *          -  -
         *         -   -
         * Point2 -    -
         *         -   -
         *          -  -
         *            --
         *             -
         *        Point3 
         */
        else
            for (var y = (int)point1.Y; y <= (int)point3.Y; y++)
                if (y < point2.Y)
                    ProcessScanLine(y, point1, point2, point1, point3, color);
                else
                    ProcessScanLine(y, point2, point3, point1, point3, color);
                    
    }
    
    /// <summary>
    /// Core method of the engine.
    /// Re-computes each vertex projection for each frame.
    /// </summary>
    /// <param name="camera">Camera that will act as the view into the scene.</param>
    /// <param name="meshes">The array of meshes to be rendered.</param>
    public void Render(Camera camera, params Mesh[] meshes)
    {
        var viewMatrix = matrixBuilder.BuildViewMatrix(camera);
        var projectionMatrix = matrixBuilder.BuildProjectionMatrix(
          1.12f
        , (float)(bitMap.PixelWidth / bitMap.Height)
        , 0.01f
        , 1f
        );

        foreach (var mesh in meshes)
        {
            // Ensure rotation is applied *BEFORE* translation.
            // Because these transformations in 3D are NOT commutative.
            var worldMatrix = matrixBuilder.BuildWorldMatrix(mesh);

            var transformationMatrix = worldMatrix * viewMatrix * projectionMatrix;
            for (var index = 0; index < mesh.Vertices.Length - 1; index++)
            {
                // Project the 3D coordinates onto the 2D space.
                var point1 = Project(mesh.Vertices[index], transformationMatrix);
                var point2 = Project(mesh.Vertices[index + 1], transformationMatrix);
                
                // Now draw the point onto the screen.
                DrawLine(point1, point2, mesh.Color);
            }

            var faceIndex = 0;
            foreach (var face in mesh.Faces)
            {
                var vertexA = mesh.Vertices[face.A];
                var vertexB = mesh.Vertices[face.B];
                var vertexC = mesh.Vertices[face.C];

                var pixelA = Project(vertexA, transformationMatrix);
                var pixelB = Project(vertexB, transformationMatrix);
                var pixelC = Project(vertexC, transformationMatrix);
                
                // Wireframe lines.
                DrawBLine(pixelA, pixelB, mesh.Color);
                DrawBLine(pixelB, pixelC, mesh.Color);
                DrawBLine(pixelC, pixelA, mesh.Color);
                
                // Drawing face triangles.
                var color = .25f + (faceIndex % mesh.Faces.Length) * .75f / mesh.Faces.Length;
                DrawTriangle(
                  pixelA
                , pixelB
                , pixelC
                , new Color4(color, color, color, 1)
                );

                faceIndex++;
            }
        }
    }
}
