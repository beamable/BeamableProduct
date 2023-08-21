using System;
using System.Runtime.Serialization;

namespace Beamable.Server;

/// <summary>
/// Represents exceptions specific to Beamable microservices.
/// </summary>
public class BeamableMicroserviceException : Exception
{
	/// <summary>
	/// The error code for an unhandled exception.
	/// </summary>
    public const string kBMS_UNHANDLED_EXCEPTION_ERROR_CODE = "000";
	/// <summary>
	/// The error code for duplicated parameter names.
	/// </summary>
    public const string kBMS_ERROR_CODE_DUPLICATED_PARAMTER_NAME = "001";
	/// <summary>
	/// The error code for unsupported overloaded methods.
	/// </summary>
    public const string kBMS_ERROR_CODE_OVERLOADED_METHOD_UNSUPPORTED = "002";
        
	/// <summary>
	/// The error code associated with the exception.
	/// </summary>
    public string ErrorCode;

	/// <summary>
	/// Initializes a new instance of the <see cref="BeamableMicroserviceException"/> class.
	/// </summary>
    public BeamableMicroserviceException()
    {
    }

	/// <summary>
	/// Initializes a new instance of the <see cref="BeamableMicroserviceException"/> class with serialized data.
	/// </summary>
    protected BeamableMicroserviceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BeamableMicroserviceException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public BeamableMicroserviceException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BeamableMicroserviceException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BeamableMicroserviceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
