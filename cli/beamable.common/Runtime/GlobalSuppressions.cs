using System.Diagnostics.CodeAnalysis;

// Unity serialization analyzer exceptions, one field each. Other fields stay checked.

// JSON contract fields that Unity does not persist.
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.Api.Events.EventItem.properties", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Common.Api.Groups.GroupUpdateProperties.requirement", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.Api.Inventory.InventoryObject`1.Properties", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.Api.Inventory.InventoryUpdateRequest.currencies", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.Api.Inventory.InventoryView.currencies", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.Api.Inventory.InventoryView.currencyProperties", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.Api.Inventory.InventoryView.items", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.Api.Inventory.ItemView.properties", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Common.Api.Inventory.InventoryUpdateBuilder.applyVipBonus", Justification = "Unity persists _serializedApplyVipBonus via callbacks; this nullable field is the API contract.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Experimental.Api.Lobbies.Lobby.data", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Experimental.Api.Lobbies.LobbyPlayer.stats", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Experimental.Api.Lobbies.CreateLobbyRequest.maxPlayers", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Experimental.Api.Lobbies.CreateLobbyRequest.passcodeLength", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Experimental.Api.Lobbies.UpdateLobbyRequest.maxPlayers", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Common.Api.Mail.SearchMailRequestClause.forSender", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Common.Api.Mail.SearchMailRequestClause.limit", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Common.Api.Mail.SearchMailRequestClause.start", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.Api.Realms.RealmConfigView.Config", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.Api.Realms.GetRealmConfigDTO.config", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Common.Api.Tournaments.TournamentEntry.nextStageChange", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Common.Api.Tournaments.TournamentEntry.previousStageChange", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.BeamCli.Contracts.CliOtelLogRecord.Attributes", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Common.BeamCli.ReportDataPoint.data", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.ServerInfo.playerData", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.FederatedInventoryProxyState.currencies", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.FederatedInventoryProxyState.items", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.MessageRailSendResponse.sentPayloadFed", Justification = "JSON contract field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.MessageRailSendResponse.params", Justification = "JSON contract field; Unity does not persist this shape.")]

// Federation contract dictionaries.
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.CampaignOfferAmount.properties", Justification = "Federation JSON requires this field.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.CampaignOfferItem.localizations", Justification = "Federation JSON requires this field.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.CampaignOfferItem.properties", Justification = "Federation JSON requires this field.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.CampaignOfferReason.properties", Justification = "Federation JSON requires this field.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.CampaignOfferGrantContext.extraDataFed", Justification = "Federation JSON requires this field.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.CampaignOfferRedeemRequest.params", Justification = "Federation JSON requires this field.")]
[assembly: SuppressMessage("Serialization", "UAC1015", Scope = "member", Target = "~F:Beamable.Common.MessageRailRecipient.data", Justification = "Federation JSON requires this field.")]

// Content fields that share a name with a legacy FormerlySerializedAs alias.
[assembly: SuppressMessage("Serialization", "UAC1018", Scope = "member", Target = "~F:Beamable.Common.Content.EventContent.phases", Justification = "Keeps the legacy FormerlySerializedAs alias alongside the current field name.")]
[assembly: SuppressMessage("Serialization", "UAC1018", Scope = "member", Target = "~F:Beamable.Common.Content.SimGameType.teams", Justification = "Keeps the legacy FormerlySerializedAs alias alongside the current field name.")]
[assembly: SuppressMessage("Serialization", "UAC1018", Scope = "member", Target = "~F:Beamable.Common.Shop.OfferObtainCurrency.symbol", Justification = "Keeps the legacy FormerlySerializedAs alias alongside the current field name.")]
[assembly: SuppressMessage("Serialization", "UAC1018", Scope = "member", Target = "~F:Beamable.Common.Shop.OfferObtainItem.contentId", Justification = "Keeps the legacy FormerlySerializedAs alias alongside the current field name.")]
[assembly: SuppressMessage("Serialization", "UAC1018", Scope = "member", Target = "~F:Beamable.Common.Shop.SKUProductIds.itunes", Justification = "Keeps the legacy FormerlySerializedAs alias alongside the current field name.")]
[assembly: SuppressMessage("Serialization", "UAC1018", Scope = "member", Target = "~F:Beamable.Common.Shop.SKUProductIds.googleplay", Justification = "Keeps the legacy FormerlySerializedAs alias alongside the current field name.")]

// Generated models. Regenerating a model that renames or adds such a field brings its warning back here.
[assembly: SuppressMessage("Serialization", "UAC1009", Scope = "member", Target = "~F:Beamable.Api.Autogenerated.Models.AudienceRequest.include", Justification = "Generated JSON model field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Api.Autogenerated.Models.ContentMeta.data", Justification = "Generated JSON model field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Api.Autogenerated.Models.SupportedFederation.settings", Justification = "Generated JSON model field; Unity does not persist this shape.")]
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member", Target = "~F:Beamable.Api.Autogenerated.Models.FederationInfo.settings", Justification = "Generated JSON model field; Unity does not persist this shape.")]
