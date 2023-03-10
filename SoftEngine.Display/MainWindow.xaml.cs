using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpDX;
using SoftEngine.Core.Importing;
using SoftEngine.Interfaces.Core.Importing;
using SoftEngine.Models;
using SoftEngine.Rendering;
using SoftEngine.Rendering.Transformation;

namespace SoftEngine.Display;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    Device device;
    Mesh[] meshes;
    Camera camera;
    Vector3 lightPosition;
    DateTime previousFrameTime;

    ILoadMesh meshLoader;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    async void Page_Loaded(object sender, RoutedEventArgs e)
    { 
        // Choose back buffer resolution.
        var width       = 640;
        var height      = 480;
        var defaultDpi  = 96;
        var pixelFormat = PixelFormats.Bgra32; 
        var bitMap      = new WriteableBitmap(
          width
        , height
        , defaultDpi
        , defaultDpi
        , pixelFormat
        , null
        );

        // The XAML Image control.
        // This is where the actual rendering will be pushed to.
        frontBuffer.Source = bitMap;
        
        device        = new Device(bitMap, new LeftHandMatrixBuilder());
        meshLoader    = new MeshFileImporter();
        meshes        = Array.Empty<Mesh>();
        
        lightPosition = Vector3.Zero;
        lightX.Text = $"{lightPosition.X}";
        lightY.Text = $"{lightPosition.Y}";
        lightZ.Text = $"{lightPosition.Z}";
        
        camera = new()
        { Position = new Vector3(0, 0, 10f)
        , Target   = Vector3.Zero
        };

        CompositionTarget.Rendering += CompositionTarget_Rendering;
    }

    async void Apply_Mesh_Click(object sender, RoutedEventArgs routedEventArgs)
    {
        meshes = await meshLoader.LoadJsonFileAsync($"{desiredMesh.Text}.babylon");
    }

    async void Apply_Light_Click(object sender, RoutedEventArgs routedEventArgs)
    {
        if (float.TryParse(lightX.Text, out var x)
        &&  float.TryParse(lightY.Text, out var y)
        &&  float.TryParse(lightZ.Text, out var z))
            lightPosition = new Vector3(x, y, z);
    }
    
    void ComputeFps()
    {
        var now = DateTime.Now;
        var currentFps = 1000.0 / (now - previousFrameTime).TotalMilliseconds;
        previousFrameTime = now;

        fps.Text = $"{currentFps:0.00} FPS";
    }
    
    void RenderLoop()
    {
        device.Clear(0, 0, 0, 255);

        foreach (var mesh in meshes)
        {
            // Rotating the mesh slightly during each frame.
            mesh.Rotation = new Vector3(
              mesh.Rotation.X// + 0.01f
            , mesh.Rotation.Y + 0.01f
            , mesh.Rotation.Z
            );
        }

        // Do the require matrix transformations.
        device.Render(camera, lightPosition, meshes);

        // Flush the back buffer to the front.
        device.Present();
    }
    
    void CompositionTarget_Rendering(object sender, object e)
    {
        ComputeFps();
        RenderLoop();
    }
}
