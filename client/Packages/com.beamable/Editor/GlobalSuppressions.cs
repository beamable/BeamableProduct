using System.Diagnostics.CodeAnalysis;

// Unity serialization analyzer exceptions, one field each. Other fields stay checked.

// JSON contract fields that Unity does not persist.
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Editor.BeamCli.UI.CommandStringData.arguments", Justification = "JSON contract field; Unity does not persist this shape.")]

// Generated models. Regenerating a model that renames or adds such a field brings its warning back here.
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Editor.BeamCli.Commands.BeamDeployablePlan.notes", Justification = "Generated JSON model field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Editor.BeamCli.Commands.BeamDeployablePlan.references", Justification = "Generated JSON model field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Editor.BeamCli.Commands.BeamFetchCommandLogRecord.LogAttributes", Justification = "Generated JSON model field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Editor.BeamCli.Commands.BeamRealmConfigOutput.Config", Justification = "Generated JSON model field; Unity does not persist this shape.")]
