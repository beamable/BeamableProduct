namespace Beamable.Common.CronExpression
{
	public struct ErrorData
	{
		public bool IsError { get; private set; }
		public string ErrorMessage
		{
			get => _errorMessage;
			set
			{
				IsError = !string.IsNullOrWhiteSpace(value);
				_errorMessage = value;
			}
		}
		private string _errorMessage;

		public ErrorData(string errorMessage) : this()
		{
			ErrorMessage = errorMessage;
		}
	}
}
