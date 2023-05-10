using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.RegularExpressions;

namespace cli;

public record ServiceName
{
	public string Value { get; } 
	public ServiceName(string value)
	{
		string pattern = @"^[A-Za-z][A-Za-z0-9_-]*$";
		bool isMatch = Regex.IsMatch(value, pattern);
			if (!isMatch)
		{
			throw new CliException($"Invalid {nameof(ServiceName)}. Input=[{value}] is invalid. Must be alpha numeric. Dashes and underscores are allowed. Must start with an alpha character.");
		}
		
		Value = value;
	}

	public static implicit operator string(ServiceName d) => d.Value;
	
	public override string ToString()
	{
		return Value;
	}
}

public class ArgValidator<T>
{
	private readonly Func<string, T> _factory;
	public ArgValidator(Func<string, T> factory)
	{
		_factory = factory;
	}

	public T GetValue(OptionResult parse)
	{
		var strValue = parse.GetValueOrDefault<string>();
		try
		{
			return _factory(strValue);
		}
		catch (Exception ex)
		{
			parse.ErrorMessage = ex.Message;
		}
		return default;
	}
	
	public T GetValue(ArgumentResult parse)
	{
		var strValue = parse.GetValueOrDefault<string>();
		try
		{
			return _factory(strValue);
		}
		catch (Exception ex)
		{
			parse.ErrorMessage = ex.Message;
		}
		return default;
	}
}
