using System.CommandLine;

namespace TextVideoPlayer;

public class OptionAdapter<T> : Option<T>
{
    private readonly Action<Options, T> _applyAction;
    public OptionAdapter(string[] aliases, Func<T> getDefaultValue, Action<Options,T> applyAction, string? description = null) : base(aliases, getDefaultValue, description)
    {
        this._applyAction = applyAction;
    }

    public void Apply(Options options, T value)
    {
        _applyAction(options, value);
    }
}