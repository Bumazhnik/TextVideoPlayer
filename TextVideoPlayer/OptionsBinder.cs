using System.CommandLine;
using System.CommandLine.Binding;

namespace TextVideoPlayer;


public class OptionsBinder<KOptions> : BinderBase<KOptions> where KOptions : new()
{
    private readonly OptionAdapterBase<KOptions>[] allOptions;

    public OptionsBinder(OptionAdapterBase<KOptions>[] allOptions)
    {
        this.allOptions = allOptions;
    }

    public void CopyOptionsToCommand(Command command)
    {
        foreach (var option in allOptions)
        {
            option.AddToCommand(command);
        }
    }

    protected override KOptions GetBoundValue(BindingContext bindingContext)
    {
        KOptions options = new KOptions();
        foreach (var option in allOptions)
        {
            option.ApplyToOptions(options, bindingContext);
        }
        return options;
    }
}
