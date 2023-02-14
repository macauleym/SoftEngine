﻿using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using SharpDX;
using SoftEngine.Models;
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
    
    readonly byte[] backBuffer;
    readonly float[] depthBuffer;
    readonly object[] lockBuffer;
    readonly WriteableBitmap bitMap;
    readonly int renderWidth;
    readonly int renderHeight;
    readonly IBuildMatrixTransform matrixBuilder;
    
    public Device(WriteableBitmap bmp, IBuildMatrixTransform matrixTransformBuilder)
    {
        bitMap        = bmp;
        renderWidth   = bitMap.PixelWidth;
        renderHeight  = bitMap.PixelHeight;
        matrixBuilder = matrixTransformBuilder;
        
        // The back buffer's size is the number of pixels we want to draw
        // onto the screen.
        var bufferSize = renderWidth * renderHeight * pixelValueCount;
        backBuffer     = new byte[bufferSize];
        depthBuffer    = new float[renderWidth * renderHeight];
        lockBuffer     = new object[renderWidth * renderHeight];
        for (var i = 0; i < lockBuffer.Length; i++)
            lockBuffer[i] = new();
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
        // Clear the back buffer.
        for (var backIndex = 0; backIndex < backBuffer.Length; backIndex += pixelValueCount)
        {
            // Windows uses the BGRA format.
            backBuffer[backIndex    ] = b;
            backBuffer[backIndex + 1] = g;
            backBuffer[backIndex + 2] = r;
            backBuffer[backIndex + 3] = a;
        }
        
        // Clear the depth buffer.
        for (var depthIndex = 0; depthIndex < depthBuffer.Length; depthIndex++)
            depthBuffer[depthIndex] = float.MaxValue;
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
            , renderWidth
            , renderHeight
            )
        , backBuffer
        , renderWidth * pixelValueCount
        , 0
        );

    /// <summary>
    /// Put a specific pixel of a given color on the screen
    /// at the given x,y coordinates, and z depth.
    /// </summary>
    /// <param name="x">X screen coordinate.</param>
    /// <param name="y">Y screen coordinate.</param>
    /// <param name="z">Z depth.</param>
    /// <param name="color">Color to draw the point as.</param>
    public void PutPixel(int x, int y, float z, Color4 color)
    {
        var index           = x + y * renderWidth;
        var indexWithPixelCount = index * pixelValueCount;

        lock (lockBuffer[index])
        {
            if (depthBuffer[index] < z)
                return;

            depthBuffer[index] = z;

            backBuffer[indexWithPixelCount] = (byte)(color.Blue * coordOffset);
            backBuffer[indexWithPixelCount + 1] = (byte)(color.Green * coordOffset);
            backBuffer[indexWithPixelCount + 2] = (byte)(color.Red * coordOffset);
            backBuffer[indexWithPixelCount + 3] = (byte)(color.Alpha * coordOffset);
        }
    }

    /// <summary>
    /// Transform the coordinates of a given vertex into 2D and 3D space.
    /// Also transforms the normal of the vertex into 3D space.  
    /// </summary>
    /// <param name="vertex">The vertex to transform.</param>
    /// <param name="transMatrix">The local matrix to transform to 2D space.</param>
    /// <param name="worldMatrix">The world matrix to transform to 3D space.</param>
    /// <returns>A new vertex with the transformed values.</returns>
    public Vertex Project(Vertex vertex, MatrixDX transMatrix, MatrixDX worldMatrix)
    {
        // Transform the coordinates into 2D space.
        var point = Vector3.TransformCoordinate(vertex.Coordinates, transMatrix);
        
        // Transform the coordinates and the normals into 3D space.
        var pointWorld = Vector3.TransformCoordinate(vertex.Coordinates, worldMatrix);
        var normalWorld = Vector3.TransformCoordinate(vertex.Normal, worldMatrix);
        
        // Transformed coordinates will be based on a coordinate
        // system starting in the center of the screen. Drawing normally
        // starts from the top left, so we need to transform again
        // to move from the center to the top left.
        var x =  point.X * renderWidth  + renderWidth / 2f;
        var y = -point.Y * renderHeight + renderHeight / 2f;

        return new ()
        { Coordinates      = new Vector3(x, y, point.Z)
        , Normal           = normalWorld
        , WorldCoordinates = pointWorld
        };
    }
    
    /// <summary>
    /// Calls PutPixel, with the addition of the clipping operation.
    /// </summary>
    /// <param name="point">Point to be drawn.</param>
    /// <param name="color">The color to draw the point as.</param>
    public void DrawPoint(Vector3 point, Color4 color)
    {
        if (point.IsInRangeOf(renderWidth, renderHeight))
            PutPixel(
              (int)point.X
            , (int)point.Y
            , point.Z
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
      ScanLine line
    , Vertex vertexA
    , Vertex vertexB
    , Vertex vertexC
    , Vertex vertexD
    , Color4 color)
    {
        var pointA = vertexA.Coordinates;
        var pointB = vertexB.Coordinates;
        var pointC = vertexC.Coordinates;
        var pointD = vertexD.Coordinates;
        
        // Using current Y, we can calculate the current gradiant.
        // This can be used to get the start and end X values
        // to draw between.
        // IF pointA.Y == pointB.Y OR pointC.Y == pointD.Y THEN
        // force the gradiant to be `1`.
        var gradiant1 = pointA.Y.CompareWith(pointB.Y, FloatComparison.NotEqual) 
            ? (line.CurrentY - pointA.Y) / (pointB.Y - pointA.Y)
            : 1;
        var gradiant2 = pointC.Y.CompareWith(pointD.Y, FloatComparison.NotEqual)
            ? (line.CurrentY - pointC.Y) / (pointD.Y - pointC.Y)
            : 1;

        var startX = (int)pointA.X.InterpolateTo(pointB.X, gradiant1);
        var endX   = (int)pointC.X.InterpolateTo(pointD.X, gradiant2);
        
        // Account for the Z depth of the line/face.
        // Start and end Z values.
        var startZ = pointA.Z.InterpolateTo(pointB.Z, gradiant1);
        var endZ   = pointC.Z.InterpolateTo(pointD.Z, gradiant2);  
        
        // Draw a line from left (startX) to right (endX).
        for (var currentX = startX; currentX < endX; currentX++)
        {
            var gradiantZ = (currentX - startX) / (float)(endX - startX);
            var currentZ = startZ.InterpolateTo(endZ, gradiantZ);
            var normalDotLight = line.NormalDotLightA; 

            // Since we now have the ability to calculate the dot product
            // of the face normal and the light direction, we can use this
            // to affect the color of the point to "shade" it. Right now, this
            // is only flat shading.
            DrawPoint(new Vector3(currentX, line.CurrentY, currentZ), color * normalDotLight);
        }
    }

    float ComputeNormalDotLight(Vector3 vertex, Vector3 normal, Vector3 lightPosition)
    {
        var lightDirection = lightPosition - vertex;
        
        normal.Normalize();
        lightDirection.Normalize();

        return Math.Max(0, Vector3.Dot(normal, lightDirection));
    }
    
    public void DrawTriangle(
      Vertex vertex1
    , Vertex vertex2
    , Vertex vertex3
    , Vector3 lightPosition
    , Color4 color
    ) {
        // Sorting the vertexs in order always keeps vertex1, then 2, then 3.
        // This keeps vertex1 always "up", giving it the lowest possible to
        // be near the top of the screen.
        if (vertex1.Coordinates.Y > vertex2.Coordinates.Y)
            (vertex2, vertex1) = (vertex1, vertex2);

        if (vertex2.Coordinates.Y > vertex3.Coordinates.Y)
            (vertex2, vertex3) = (vertex3, vertex2);

        if (vertex1.Coordinates.Y > vertex2.Coordinates.Y)
            (vertex2, vertex1) = (vertex1, vertex2);
        
        var coords1 = vertex1.Coordinates;
        var coords2 = vertex2.Coordinates;
        var coords3 = vertex3.Coordinates;
        
        // The face's normal vector is the average of each vertex's normal.
        // The center point is the average of the world coords.
        var faceNormal = (vertex1.Normal + vertex2.Normal + vertex3.Normal) / 3;
        var centerPoint = (vertex1.WorldCoordinates + vertex2.WorldCoordinates + vertex3.WorldCoordinates) / 3;
        
        // Get the dot product of the face normal and the light direction.
        // As this will give us the dot product as a value between 0 and 1,
        // we can use this to modify the intensity of the color of the face.
        // This simulates the idea of "shading".
        var normalDotLight = ComputeNormalDotLight(centerPoint, faceNormal, lightPosition);
        var line = new ScanLine
        { NormalDotLightA = normalDotLight
        };

        // Now calculate inverse slopes.
        float dPoint1Point2
            , dPoint1Point3;

        if (coords2.Y - coords1.Y > 0)
            dPoint1Point2 = (coords2.X - coords1.X) / (coords2.Y - coords1.Y);
        else
            dPoint1Point2 = 0;

        if (coords3.Y - coords1.Y > 0)
            dPoint1Point3 = (coords3.X - coords1.X) / (coords3.Y - coords1.Y);
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
            for (var y = (int)coords1.Y; y <= (int)coords3.Y; y++)
            {
                line.CurrentY = y;
                
                if (y < coords2.Y)
                    ProcessScanLine(
                        line
                        , vertex1
                        , vertex3
                        , vertex1
                        , vertex2
                        , color);
                else
                    ProcessScanLine(
                        line
                        , vertex1
                        , vertex3
                        , vertex2
                        , vertex3
                        , color);
            }

        // Case where vertexs are as follows:
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
            for (var y = (int)coords1.Y; y <= (int)coords3.Y; y++)
            {
                line.CurrentY = y;
                
                if (y < coords2.Y)
                    ProcessScanLine(
                        line
                        , vertex1
                        , vertex2
                        , vertex1
                        , vertex3
                        , color
                    );
                else
                    ProcessScanLine(
                        line
                        , vertex2
                        , vertex3
                        , vertex1
                        , vertex3
                        , color
                    );
            }
    }

    /// <summary>
    /// Core method of the engine.
    /// Re-computes each vertex projection for each frame.
    /// </summary>
    /// <param name="camera">Camera that will act as the view into the scene.</param>
    /// <param name="lightPosition"></param>
    /// <param name="meshes">The array of meshes to be rendered.</param>
    public void Render(Camera camera, Vector3 lightPosition, params Mesh[] meshes)
    {
        var viewMatrix       = matrixBuilder.BuildViewMatrix(camera);
        var projectionMatrix = matrixBuilder.BuildProjectionMatrix(
          1.12f
        , (float)(renderWidth / bitMap.Height)
        , 0.01f
        , 1f
        );

        foreach (var mesh in meshes)
        {
            // Ensure rotation is applied *BEFORE* translation.
            // Because these transformations in 3D are NOT commutative.
            var worldMatrix = matrixBuilder.BuildWorldMatrix(mesh);

            var transformationMatrix = worldMatrix * viewMatrix * projectionMatrix;
            /*
            for (var index = 0; index < mesh.Vertices.Length - 1; index++)
            {
                // Project the 3D coordinates onto the 2D space.
                var point1 = Project(mesh.Vertices[index], transformationMatrix);
                var point2 = Project(mesh.Vertices[index + 1], transformationMatrix);
                
                // Now draw the point onto the screen.
                DrawLine(point1, point2, mesh.Color);
            }
            */

            Parallel.For(0, mesh.Faces.Length, faceIndex =>
            {
                var face = mesh.Faces[faceIndex];
                var vertexA = mesh.Vertices[face.A];
                var vertexB = mesh.Vertices[face.B];
                var vertexC = mesh.Vertices[face.C];

                var pixelA = Project(vertexA, transformationMatrix, worldMatrix);
                var pixelB = Project(vertexB, transformationMatrix, worldMatrix);
                var pixelC = Project(vertexC, transformationMatrix, worldMatrix);

                // Wireframe lines.
                /*
                DrawBLine(pixelA, pixelB, mesh.Color);
                DrawBLine(pixelB, pixelC, mesh.Color);
                DrawBLine(pixelC, pixelA, mesh.Color);
                */

                // Drawing face triangles.
                var color = .25f + (faceIndex % mesh.Faces.Length) * .75f / mesh.Faces.Length;
                DrawTriangle(
                  pixelA
                , pixelB
                , pixelC
                , lightPosition
                , new Color4(color, color, color, 1)
                );

                faceIndex++;
            });
        }
    }
}
