using System.Diagnostics.CodeAnalysis;

// Unity serialization analyzer exceptions, one field each. Other fields stay checked.

// JSON contract fields that Unity does not persist.
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Experimental.Api.Matchmaking.TicketReservationRequest.maxWaitDurationSecs", Justification = "JSON contract field; Unity does not persist this shape.")]

// Runtime error payloads.
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Player.RegistrationResult.innerException", Justification = "Public error payload; Unity cannot persist System.Exception.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Player.PasswordResetResult.innerException", Justification = "Public error payload; Unity cannot persist System.Exception.")]
