namespace TextVideoPlayer;

public class Options
{
    public string VideoPath { get; set; } = "";
    public bool Colored { get; set; }
    public bool FullColored { get; set; }
    public bool Notepad { get; set; }
    public bool InvertGrayscale { get; set; }
    public bool Timeline { get; set; }
    public int Width { get; set; }
    public string GrayscaleChars { get; set; } = "";
    public int ColorAccuracy { get; set; }
}