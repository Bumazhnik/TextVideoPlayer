using System.CommandLine;
using System.CommandLine.Binding;

namespace TextVideoPlayer;

public class OptionsBinder:BinderBase<Options>
{
    private readonly OptionAdapter<bool>[] boolOptions =
    [
        new(
            ["--notepad", "-n"],
            () => false,
            (o,v)=> o.Notepad=v,
            "Use notepad as output"),
        new(
            ["--colored", "-c"],
            () => true,
            (o,v)=> o.Colored=v,
            "Colored"),
        new(
            ["--full-colored", "-f"],
            () => false,
            (o,v)=> o.FullColored=v,
            "Full colored"),
        new(
            ["--invert-grayscale", "-i"],
            () => false,
            (o,v)=> o.InvertGrayscale=v,
            "Invert grayscale"),

        new(
            ["--timeline", "-t"],
            () => false,
            (o,v)=> o.Timeline=v,
            "Timeline at the bottom")
    ];

    private readonly OptionAdapter<string>[] stringOptions =
    [
        new(
        ["--video-file",
        "-v"],
        () => "video.mp4",
        (o,v)=> o.VideoPath=v,
        "Video file"),
        new(
        ["--grayscale-chars",
        "-g"],
        () => " .:-=+*#%@",
        (o,v)=> o.GrayscaleChars=v,
        "Grayscale characters"
        ),
    ];
    private readonly OptionAdapter<int>[] intOptions =
    [
        new(
            ["--accuracy",
                "-a"],
            () => 4,
            (o,v)=> o.ColorAccuracy=Math.Max(1,Math.Min(255,v)),
            "Color accuracy (1~255), where 1 is the most accurate. More accurate = less performant"),
        new(
            ["--width",
                "-w"],
            () => 100,
            (o,v)=> o.Width=v,
            "Width (characters)"),
    ];
    public void CopyOptionsToCommand(Command command)
    {
        foreach (var item in boolOptions)
        {
            command.AddOption(item);
        }
        foreach (var item in stringOptions)
        {
            command.AddOption(item);
        }
        foreach (var item in intOptions)
        {
            command.AddOption(item);
        }
    }
    protected override Options GetBoundValue(BindingContext bindingContext)
    {
        Options options = new Options();
        foreach (var item in boolOptions)
        {
            item.Apply(options,bindingContext.ParseResult.GetValueForOption(item));
        }
        foreach (var item in stringOptions)
        {
            item.Apply(options,bindingContext.ParseResult.GetValueForOption(item)!);
        }
        foreach (var item in intOptions)
        {
            item.Apply(options,bindingContext.ParseResult.GetValueForOption(item));
        }

        return options;
    }
}