// This file generated by a copy-operation from another project. 
// Edits to this file will be overwritten by the build process. 

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
