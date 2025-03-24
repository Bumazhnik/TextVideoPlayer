using System.CommandLine;
using System.CommandLine.Binding;

namespace TextVideoPlayer;

// Base interface to allow unified storage
public interface OptionAdapterBase<KOptions>
{
    void AddToCommand(Command command);
    void ApplyToOptions(KOptions options, BindingContext bindingContext);
}

// Generic class that extends the base interface
public class OptionAdapter<KOptions, T> : Option<T>, OptionAdapterBase<KOptions>
{
    private readonly Action<KOptions, T> _applyAction;

    public OptionAdapter(string[] aliases, Func<T> getDefaultValue, Action<KOptions, T> applyAction, string? description = null)
        : base(aliases, getDefaultValue, description)
    {
        _applyAction = applyAction;
    }

    public void AddToCommand(Command command)
    {
        command.AddOption(this);
    }

    public void ApplyToOptions(KOptions options, BindingContext bindingContext)
    {
        T? parsedValue = bindingContext.ParseResult.GetValueForOption(this);
        if (parsedValue is not null)
        {
            _applyAction(options, parsedValue);
        }
    }
}