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
    
    private static Options options;
    private static Process notepadProcess;

    private static IntPtr notepadChild;

    private static Application app;

    private static bool isClosed;
    private static bool isPlaying;

    [STAThread]
    static void Main(string[] args)
    {
        var rootCommand = new RootCommand();
        var optionsBinder = new OptionsBinder();
        optionsBinder.CopyOptionsToCommand(rootCommand);
        rootCommand.SetHandler(Start,optionsBinder);
        rootCommand.Invoke(args);
    }

    private static void Start(Options o)
    {
        options = o;
        ResetColor();
        Console.Clear();
        SetColoredMode();
        if (options.Notepad)
        {
            notepadProcess = Process.Start("notepad.exe");
            notepadProcess.WaitForInputIdle();
            notepadChild = Win32.FindWindowEx(notepadProcess.MainWindowHandle, new IntPtr(0), "Edit", null);
        }
        
        app = new Application();
        app.Startup += App_Startup;
        app.Exit += App_Exit;
        var controlThread = new Thread(Controls);
        controlThread.Start();
        Console.CancelKeyPress += (sender, args) =>
        {
            args.Cancel = true;
            ShutdownApp();
        };
        app.Run();

    }



    #region Application related
    private static void App_Startup(object sender, StartupEventArgs e)
    {
        mediaPlayer = new MediaPlayer();
        mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        mediaPlayer.ScrubbingEnabled = true;


        // Ensure the path is correct
        if (!File.Exists(options.VideoPath))
        {
            Console.WriteLine("Video file not found.");
            ShutdownApp();
            return;
        }

        mediaPlayer.Open(new Uri(options.VideoPath, UriKind.RelativeOrAbsolute));
    }
    private static void ShutdownApp()
    {
        app.Dispatcher.BeginInvoke(() =>
        {
            app.Shutdown();
        });
    }
    private static void App_Exit(object sender, ExitEventArgs e)
    {
        OnCloseApplication();
    }
    private static void OnCloseApplication()
    {
        isClosed = true;
        app.Dispatcher.BeginInvoke(()=>mediaPlayer.Close());
        ResetColor();
        Console.WriteLine("Application closed");
    }
    #endregion
    
    #region Controls

    private static void Controls()
    {
        while (true)
        {
            var key = ReadKey();
            if(isClosed)
                return;
            switch (key.Key)
            {
                case ConsoleKey.Q:
                    ShutdownApp();
                    return;
                case ConsoleKey.LeftArrow:
                    AddTime(TimeSpan.FromSeconds(-5));
                    break;
                case ConsoleKey.RightArrow:
                    AddTime(TimeSpan.FromSeconds(5));
                    break;
                case ConsoleKey.Spacebar:
                    PlayPauseControl();
                    break;
            }
        }
    }

    private static void PlayPauseControl()
    {
        app.Dispatcher.BeginInvoke(() =>
        {
            if(isPlaying)Pause();
            else Play();
        });
    }

    private static void Play()
    {
        mediaPlayer.Play();
        isPlaying = true;
    }

    private static void Pause()
    {
        mediaPlayer.Pause();
        isPlaying = false;
    }
    private static void AddTime(TimeSpan time)
    {
        app.Dispatcher.BeginInvoke(async () =>
        {
            mediaPlayer.Position += time;
            await Task.Delay(100);
            RenderFrame();
        });
    }

    #endregion
    
    #region Console related
        private static void SetColoredMode()
        {
            var handle = Win32.GetStdHandle( -11 );
            Win32.GetConsoleMode( handle, out var mode );
            Win32.SetConsoleMode( handle, mode | 0x4 );
        }
        private static ConsoleKeyInfo ReadKey()
        {
            while (true)
            {
                if (Console.KeyAvailable)
                    return Console.ReadKey(true);
                Thread.Sleep(50);
                if (isClosed)
                    return new ConsoleKeyInfo();
            }
        }
        private static void ResetColor()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }

    #endregion
    
    #region MediaPlayer events

    private static void MediaPlayer_MediaOpened(object? sender, EventArgs e)
    {
        int videoWidth = mediaPlayer.NaturalVideoWidth;
        int videoHeight = mediaPlayer.NaturalVideoHeight;

        if (videoWidth == 0 || videoHeight == 0)
        {
            Console.WriteLine("Could not get video dimensions.");
            ShutdownApp();
            return;
        }

        int width = options.Width;
        int height = (int)(options.Width * ((double)videoHeight / videoWidth) / 2);

        sb = new StringBuilder(width * height + height + 100);
        drawingVisual = new DrawingVisual();
        renderTarget = new RenderTargetBitmap(width,height, 96, 96, PixelFormats.Pbgra32);

        // Start playback and hook into the rendering event
        Play();
        CompositionTarget.Rendering += OnRendering;
    }
    private static void MediaPlayer_MediaEnded(object? sender, EventArgs e)
    {
        ShutdownApp();
    }
    private static void MediaPlayer_MediaFailed(object? sender, ExceptionEventArgs e)
    {
        Console.WriteLine("Media failed to load: " + e.ErrorException.Message);
        ShutdownApp();
    }

    #endregion
    
    #region Rendering
    private static void RenderFrame()
    {
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.DrawVideo(mediaPlayer, new Rect(0, 0, renderTarget.PixelWidth, renderTarget.PixelHeight));
        }

        renderTarget.Render(drawingVisual);
        
        ProcessFrame();
    }
    private static void OnRendering(object? sender, EventArgs g)
    {
        if(!isPlaying)
            return;
        RenderFrame();
    }
    private static int AdjustAccuracy(int value) => value / options.ColorAccuracy * options.ColorAccuracy;
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
                    int cR = AdjustAccuracy(red);
                    int cB = AdjustAccuracy(blue);
                    int cG = AdjustAccuracy(green);
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
                    int charIndex = (int)(avg / 255 * (options.GrayscaleChars.Length-1));
                    if (options.InvertGrayscale)
                        charIndex = options.GrayscaleChars.Length - 1 - charIndex;
                    sb.Append(options.GrayscaleChars[charIndex]);
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
        if (options.Timeline)
        {
            ResetColor();
            Console.Write($"{mediaPlayer.Position} : {mediaPlayer.NaturalDuration}");
        }
    }

    

    #endregion
}

