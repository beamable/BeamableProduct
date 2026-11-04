using System.Diagnostics.CodeAnalysis;

// Unity serialization diagnostics for existing JSON payloads and legacy asset migrations.
// Keep exceptions member-scoped so new unsupported fields still produce diagnostics.
// This file must remain in the same assembly as the targeted declarations.

[assembly: SuppressMessage("Serialization", "UAC1001",
	Justification = "Preserve the public nullable request contract.", Scope = "member", Target = "~F:Beamable.Experimental.Api.Matchmaking.TicketReservationRequest.maxWaitDurationSecs")]
