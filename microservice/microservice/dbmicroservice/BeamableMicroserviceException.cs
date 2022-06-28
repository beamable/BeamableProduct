using System;
using System.Runtime.Serialization;

namespace Beamable.Server;

public class BeamableMicroserviceException : Exception
{
	public const string kBMS_UNHANDLED_EXCEPTION_ERROR_CODE = "000";
	public const string kBMS_ERROR_CODE_DUPLICATED_PARAMTER_NAME = "001";
	public const string kBMS_ERROR_CODE_OVERLOADED_METHOD_UNSUPPORTED = "002";

	public string ErrorCode;

	public BeamableMicroserviceException()
	{
	}

	protected BeamableMicroserviceException(SerializationInfo info, StreamingContext context) : base(info, context)
	{
	}

	public BeamableMicroserviceException(string message) : base(message)
	{
	}

	public BeamableMicroserviceException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
