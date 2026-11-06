using System.Diagnostics.CodeAnalysis;

// Unity serialization diagnostics for existing JSON payloads and legacy asset migrations.
// Keep exceptions member-scoped so new unsupported fields still produce diagnostics.
// This file must remain in the same assembly as the targeted declarations.

[assembly: SuppressMessage("Serialization", "UAC1015",
	Justification = "Reflection JSON requires this dictionary.", Scope = "member", Target = "~F:Beamable.Editor.BeamCli.Commands.BeamDeployablePlan.notes")]

[assembly: SuppressMessage("Serialization", "UAC1015",
	Justification = "Reflection JSON requires this dictionary.", Scope = "member", Target = "~F:Beamable.Editor.BeamCli.Commands.BeamDeployablePlan.references")]

[assembly: SuppressMessage("Serialization", "UAC1015",
	Justification = "Reflection JSON requires this dictionary.", Scope = "member", Target = "~F:Beamable.Editor.BeamCli.Commands.BeamFetchCommandLogRecord.LogAttributes")]

[assembly: SuppressMessage("Serialization", "UAC1015",
	Justification = "Reflection JSON requires this dictionary.", Scope = "member", Target = "~F:Beamable.Editor.BeamCli.Commands.BeamRealmConfigOutput.Config")]
