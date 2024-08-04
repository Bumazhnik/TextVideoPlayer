using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Text;
using System.CommandLine;
namespace TextVideoPlayer;



class Program
{
    private static MediaPlayer mediaPlayer;
    private static RenderTargetBitmap renderTarget;
    private static DrawingVisual drawingVisual;
    private static StringBuilder sb;

    private const string ASCII = " .:-=+*#%@";
    private static Options options;
    private static Process notepadProcess;

    private static IntPtr notepadChild;

    [STAThread]
    static void Main(string[] args)
    {

        var rootCommand = new RootCommand();
        var notepadOption = new Option<bool>(
            ["--notepad", "-n"],
            () => false,
            "Use notepad as output");
        var coloredOption = new Option<bool>(
            ["--colored", "-c"],
            () => true,
            "Colored");
        var fullColoredOption = new Option<bool>(
            ["--full-colored", "-f"],
            () => false,
            "Full colored");

        var widthOption = new Option<int>(
            new[] { "--width", "-w" },
            () => 100,
            "Width (characters)");
        var videoFileOption =  new Option<string>(
            new[] { "--video-file", "-v" },
            () => "video.mp4",
            "Video file");
        
        var invertColorsOption = new Option<bool>(
            ["--invert-grayscale", "-i"],
            () => false,
            "Invert grayscale");
        rootCommand.AddOption(notepadOption);
        rootCommand.AddOption(coloredOption);
        rootCommand.AddOption(fullColoredOption);
        rootCommand.AddOption(widthOption);
        rootCommand.AddOption(videoFileOption);
        rootCommand.AddOption(invertColorsOption);
        rootCommand.SetHandler((n,c,f,w,v,i) =>
        {
            options = new()
            {

                Notepad = n,
                Colored = c || f,
                FullColored = f,
                Width = w,
                VideoPath = v,
                InvertGrayscale = i
            };
            Start();

        },notepadOption,coloredOption,fullColoredOption,widthOption,videoFileOption,invertColorsOption);
        rootCommand.Invoke(args);

    }

    private static void Start()
    {
        Console.Clear();
        var handle = Win32.GetStdHandle( -11 );
        Win32.GetConsoleMode( handle, out var mode );
        Win32.SetConsoleMode( handle, mode | 0x4 );
        if (options.Notepad)
        {
            notepadProcess = Process.Start("notepad.exe");
            notepadProcess.WaitForInputIdle();
            notepadChild = Win32.FindWindowEx(notepadProcess.MainWindowHandle, new IntPtr(0), "Edit", null);

        }
        var app = new Application();
        app.Startup += App_Startup;
        app.Exit += App_Exit;
        app.Run();
    }
    private static void App_Exit(object sender, ExitEventArgs e)
    {
        Console.Clear();
    }

    private static void App_Startup(object sender, StartupEventArgs e)
    {
        mediaPlayer = new MediaPlayer();
        mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

        // Ensure the path is correct
        if (!File.Exists(options.VideoPath))
        {
            Console.WriteLine("Video file not found.");
            return;
        }

        mediaPlayer.Open(new Uri(options.VideoPath, UriKind.RelativeOrAbsolute));
    }



    private static void MediaPlayer_MediaOpened(object? sender, EventArgs e)
    {
        int videoWidth = mediaPlayer.NaturalVideoWidth;
        int videoHeight = mediaPlayer.NaturalVideoHeight;

        if (videoWidth == 0 || videoHeight == 0)
        {
            Console.WriteLine("Could not get video dimensions.");
            return;
        }

        int width = options.Width;
        int height = (int)(options.Width * ((double)videoHeight / videoWidth) / 2);

        sb = new StringBuilder(width * height + height + 100);
        drawingVisual = new DrawingVisual();
        renderTarget = new RenderTargetBitmap(width,height, 96, 96, PixelFormats.Pbgra32);

        // Start playback and hook into the rendering event
        mediaPlayer.Play();
        CompositionTarget.Rendering += OnRendering;
    }
    private static void MediaPlayer_MediaEnded(object? sender, EventArgs e)
    {
        Application.Current.Shutdown();
    }
    private static void MediaPlayer_MediaFailed(object? sender, ExceptionEventArgs e)
    {
        Console.WriteLine("Media failed to load: " + e.ErrorException.Message);
    }
    private static void OnRendering(object? sender, EventArgs g)
    {
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.DrawVideo(mediaPlayer, new Rect(0, 0, renderTarget.PixelWidth, renderTarget.PixelHeight));
        }

        renderTarget.Render(drawingVisual);
        
        ProcessFrame();
    }



    static void ProcessFrame()
    {
        int width = renderTarget.PixelWidth;
        int height = renderTarget.PixelHeight;
        int stride = width * ((renderTarget.Format.BitsPerPixel + 7) / 8);

        byte[] pixels = new byte[height * stride];
        renderTarget.CopyPixels(pixels, stride, 0);
        Console.CursorLeft = 0;
        Console.CursorTop = 0;
        int prevR = -1;
        int prevG = -1;
        int prevB = -1;
        // Process pixels
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * stride + x * 4;
        
                int blue = pixels[index];
                int green = pixels[index + 1];
                int red = pixels[index + 2];
                if(options.Colored && !options.Notepad)
                {
                    int cR = red / 4 * 4;
                    int cB = blue / 4 * 4;
                    int cG = green / 4 * 4;
                    if (prevR != cR || prevB != cB || prevG != cG)
                    {
                        prevR = cR;
                        prevB = cB;
                        prevG = cG;
                        if(options.FullColored)
                            sb.Append("\x1b[48;2;");
                        else
                            sb.Append("\x1b[38;2;");
                        sb.Append(cR)
                            .Append(';')
                            .Append(cG)
                            .Append(';')
                            .Append(cB)
                            .Append('m');
                    }
                }

                if (options.FullColored)
                {
                    sb.Append(' ');
                }
                else
                {
                    double avg = (double)(red + green + blue) / 3;
                    int asciiIndex = (int)(avg / 255 * (ASCII.Length-1));
                    if (options.InvertGrayscale)
                        asciiIndex = ASCII.Length - 1 - asciiIndex;
                    sb.Append(ASCII[asciiIndex]);
                }

            }
            sb.Append('\n');
        }

        if (options.Notepad)
        {
            Win32.SendMessage(notepadChild, Win32.WM_SETTEXT, 0, sb.ToString());
        }
        else
        {
            Console.Write(sb);
        }
        sb.Clear();
    }
}

