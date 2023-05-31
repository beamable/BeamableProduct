using System.CommandLine.Parsing;

namespace cli
{
	public class ArgValidator<T>
	{
		private readonly Func<string, T> _factory;
		public ArgValidator(Func<string, T> factory)
		{
			_factory = factory;
		}

		public T GetValue(OptionResult parse)
		{
			if (parse == null) return default;
			
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
}
