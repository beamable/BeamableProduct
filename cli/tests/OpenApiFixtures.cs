namespace tests;

public static class OpenApiFixtures
{
	#region event-players object

	public const string EventPlayersObjectApi = @"
{
    ""openapi"": ""3.0.1"",
    ""info"": {
        ""title"": ""event-players object"",
        ""contact"": {
            ""name"": ""Beamable Support"",
            ""url"": ""https://api.beamable.com"",
            ""email"": ""support@beamable.com""
        },
        ""version"": ""1.0""
    },
    ""servers"": [
        {
            ""url"": ""https://api.beamable.com""
        }
    ],
    ""paths"": {
        ""/object/event-players/{objectId}/claim-entitlements"": {
            ""post"": {
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""required"": true,
                        ""schema"": {
                            ""type"": ""string""
                        }
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/EventClaimEntitlementsRequest""
                            }
                        }
                    }
                },
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/CommonResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/object/event-players/{objectId}/"": {
            ""get"": {
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""required"": true,
                        ""schema"": {
                            ""type"": ""string""
                        }
                    }
                ],
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EventPlayerView""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        },
        ""/object/event-players/{objectId}/claim"": {
            ""post"": {
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""required"": true,
                        ""schema"": {
                            ""type"": ""string""
                        }
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/EventClaimRequest""
                            }
                        }
                    }
                },
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EventClaimResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        },
        ""/object/event-players/{objectId}/score"": {
            ""put"": {
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""required"": true,
                        ""schema"": {
                            ""type"": ""string""
                        }
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/EventScoreRequest""
                            }
                        }
                    }
                },
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/CommonResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        }
    },
    ""components"": {
        ""schemas"": {
            ""EventInventoryRewardItem"": {
                ""type"": ""object"",
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""properties"": {
                        ""type"": ""object""
                    }
                },
                ""additionalProperties"": false
            },
            ""ItemCreateRequest"": {
                ""type"": ""object"",
                ""properties"": {
                    ""contentId"": {
                        ""type"": ""string""
                    },
                    ""properties"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemProperty""
                        }
                    }
                },
                ""additionalProperties"": false
            },
            ""EventClaimResponse"": {
                ""title"": ""Event Claim Response"",
                ""type"": ""object"",
                ""properties"": {
                    ""view"": {
                        ""$ref"": ""#/components/schemas/EventPlayerStateView""
                    },
                    ""gameRspJson"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false
            },
            ""EventPlayerView"": {
                ""title"": ""Event Player View"",
                ""type"": ""object"",
                ""properties"": {
                    ""running"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/EventPlayerStateView""
                        }
                    },
                    ""done"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/EventPlayerStateView""
                        }
                    }
                },
                ""additionalProperties"": false
            },
            ""EventClaimEntitlementsRequest"": {
                ""title"": ""Event Claim Entitlements Request"",
                ""type"": ""object"",
                ""properties"": {
                    ""eventId"": {
                        ""type"": ""string""
                    },
                    ""generators"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/EntitlementGenerator""
                        }
                    }
                },
                ""additionalProperties"": false
            },
            ""CommonResponse"": {
                ""title"": ""Common Response"",
                ""type"": ""object"",
                ""properties"": {
                    ""result"": {
                        ""type"": ""string""
                    },
                    ""data"": {
                        ""type"": ""object""
                    }
                },
                ""additionalProperties"": false
            },
            ""EventRewardState"": {
                ""required"": [
                    ""min"",
                    ""earned"",
                    ""claimed""
                ],
                ""type"": ""object"",
                ""properties"": {
                    ""pendingInventoryRewards"": {
                        ""$ref"": ""#/components/schemas/EventInventoryPendingRewards""
                    },
                    ""currencies"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/EventInventoryRewardCurrency""
                        }
                    },
                    ""pendingCurrencyRewards"": {
                        ""type"": ""object""
                    },
                    ""pendingItemRewards"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemCreateRequest""
                        }
                    },
                    ""items"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/EventInventoryRewardItem""
                        }
                    },
                    ""min"": {
                        ""type"": ""number""
                    },
                    ""max"": {
                        ""type"": ""number""
                    },
                    ""earned"": {
                        ""type"": ""boolean""
                    },
                    ""claimed"": {
                        ""type"": ""boolean""
                    },
                    ""pendingEntitlementRewards"": {
                        ""type"": ""object""
                    },
                    ""obtain"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/EventRewardObtain""
                        }
                    }
                },
                ""additionalProperties"": false
            },
            ""EventScoreRequest"": {
                ""title"": ""Event Score Request"",
                ""required"": [
                    ""score""
                ],
                ""type"": ""object"",
                ""properties"": {
                    ""eventId"": {
                        ""type"": ""string""
                    },
                    ""score"": {
                        ""type"": ""number""
                    },
                    ""increment"": {
                        ""type"": ""boolean""
                    },
                    ""stats"": {
                        ""type"": ""object""
                    }
                },
                ""additionalProperties"": false
            },
            ""EntitlementGenerator"": {
                ""type"": ""object"",
                ""properties"": {
                    ""quantity"": {
                        ""type"": ""integer""
                    },
                    ""claimWindow"": {
                        ""$ref"": ""#/components/schemas/EntitlementClaimWindow""
                    },
                    ""params"": {
                        ""type"": ""object""
                    },
                    ""symbol"": {
                        ""type"": ""string""
                    },
                    ""specialization"": {
                        ""type"": ""string""
                    },
                    ""action"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false
            },
            ""EventRewardObtain"": {
                ""required"": [
                    ""count""
                ],
                ""type"": ""object"",
                ""properties"": {
                    ""symbol"": {
                        ""type"": ""string""
                    },
                    ""count"": {
                        ""type"": ""integer""
                    }
                },
                ""additionalProperties"": false
            },
            ""EventClaimRequest"": {
                ""title"": ""Event Claim Request"",
                ""type"": ""object"",
                ""properties"": {
                    ""eventId"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false
            },
            ""EventInventoryRewardCurrency"": {
                ""required"": [
                    ""amount""
                ],
                ""type"": ""object"",
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""amount"": {
                        ""type"": ""integer""
                    }
                },
                ""additionalProperties"": false
            },
            ""EventInventoryPendingRewards"": {
                ""required"": [
                    ""empty""
                ],
                ""type"": ""object"",
                ""properties"": {
                    ""currencies"": {
                        ""type"": ""object""
                    },
                    ""items"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemCreateRequest""
                        }
                    },
                    ""empty"": {
                        ""type"": ""boolean""
                    }
                },
                ""additionalProperties"": false
            },
            ""EventPlayerStateView"": {
                ""required"": [
                    ""score"",
                    ""rank"",
                    ""running"",
                    ""secondsRemaining""
                ],
                ""type"": ""object"",
                ""properties"": {
                    ""name"": {
                        ""type"": ""string""
                    },
                    ""running"": {
                        ""type"": ""boolean""
                    },
                    ""allPhases"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/EventPlayerPhaseView""
                        }
                    },
                    ""rank"": {
                        ""type"": ""integer""
                    },
                    ""score"": {
                        ""type"": ""number""
                    },
                    ""currentPhase"": {
                        ""$ref"": ""#/components/schemas/EventPlayerPhaseView""
                    },
                    ""secondsRemaining"": {
                        ""type"": ""integer""
                    },
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""leaderboardId"": {
                        ""type"": ""string""
                    },
                    ""rankRewards"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/EventRewardState""
                        }
                    },
                    ""groupRewards"": {
                        ""$ref"": ""#/components/schemas/EventPlayerGroupState""
                    },
                    ""scoreRewards"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/EventRewardState""
                        }
                    }
                },
                ""additionalProperties"": false
            },
            ""EntitlementClaimWindow"": {
                ""required"": [
                    ""open"",
                    ""close""
                ],
                ""type"": ""object"",
                ""properties"": {
                    ""open"": {
                        ""type"": ""integer""
                    },
                    ""close"": {
                        ""type"": ""integer""
                    }
                },
                ""additionalProperties"": false
            },
            ""EventPlayerPhaseView"": {
                ""required"": [
                    ""durationSeconds""
                ],
                ""type"": ""object"",
                ""properties"": {
                    ""name"": {
                        ""type"": ""string""
                    },
                    ""durationSeconds"": {
                        ""type"": ""integer""
                    },
                    ""rules"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/EventRule""
                        }
                    }
                },
                ""additionalProperties"": false
            },
            ""ItemProperty"": {
                ""type"": ""object"",
                ""properties"": {
                    ""name"": {
                        ""type"": ""string""
                    },
                    ""value"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false
            },
            ""EventRule"": {
                ""type"": ""object"",
                ""properties"": {
                    ""rule"": {
                        ""type"": ""string""
                    },
                    ""value"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false
            },
            ""EventPlayerGroupState"": {
                ""required"": [
                    ""groupScore"",
                    ""groupRank""
                ],
                ""type"": ""object"",
                ""properties"": {
                    ""groupScore"": {
                        ""type"": ""number""
                    },
                    ""groupId"": {
                        ""type"": ""string""
                    },
                    ""rankRewards"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/EventRewardState""
                        }
                    },
                    ""groupRank"": {
                        ""type"": ""integer""
                    },
                    ""scoreRewards"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/EventRewardState""
                        }
                    }
                },
                ""additionalProperties"": false
            }
        },
        ""securitySchemes"": {
            ""userRequired"": {
                ""type"": ""apiKey"",
                ""description"": ""Gamer Tag of the player."",
                ""name"": ""X-DE-GAMERTAG"",
                ""in"": ""header""
            },
            ""scope"": {
                ""type"": ""apiKey"",
                ""description"": ""Customer and project scope. This should contain the '<customer-id>.<project-id>'."",
                ""name"": ""X-DE-SCOPE"",
                ""in"": ""header""
            },
            ""api"": {
                ""type"": ""apiKey"",
                ""description"": ""Signed Request authentication using project secret key."",
                ""name"": ""X-DE-SIGNATURE"",
                ""in"": ""header""
            },
            ""user"": {
                ""type"": ""http"",
                ""description"": ""Bearer authentication with an player access token in the Authorization header."",
                ""scheme"": ""bearer"",
                ""bearerFormat"": ""Bearer <Access Token>""
            }
        }
    },
    ""externalDocs"": {
        ""description"": ""Beamable Documentation"",
        ""url"": ""https://docs.beamable.com""
    }
}";
	#endregion

	#region account basic
	public const string AccountBasicOpenApi = @"{
    ""info"": {
        ""title"": ""accounts basic"",
        ""version"": ""1.0"",
        ""contact"": {
            ""name"": ""Beamable Support"",
            ""url"": ""https://api.beamable.com"",
            ""email"": ""support@beamable.com""
        }
    },
    ""servers"": [
        {
            ""url"": ""https://api.beamable.com""
        }
    ],
    ""paths"": {
        ""/basic/accounts/me/device"": {
            ""delete"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/AccountPlayerView""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/DeleteDevicesRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/accounts/me"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/AccountPlayerView""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            },
            ""put"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/AccountPlayerView""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/AccountUpdate""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/accounts/me/third-party"": {
            ""delete"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/AccountPlayerView""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/ThirdPartyAvailableRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/accounts/get-personally-identifiable-information"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/AccountPersonallyIdentifiableInformationResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""query"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": false
                    }
                ],
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        },
        ""/basic/accounts/search"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/AccountSearchResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""query"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": false
                    },
                    {
                        ""name"": ""page"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""integer""
                        },
                        ""required"": true
                    },
                    {
                        ""name"": ""pagesize"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""integer""
                        },
                        ""required"": true
                    }
                ],
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/accounts/email-update/init"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EmptyResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/EmailUpdateRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/accounts/email-update/confirm"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EmptyResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/EmailUpdateConfirmation""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/accounts/available/third-party"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/AccountAvailableResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""thirdParty"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": false
                    },
                    {
                        ""name"": ""token"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": false
                    }
                ],
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        },
        ""/basic/accounts/admin/admin-user"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/AccountPortalView""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/AddAccountRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/accounts/register"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/AccountPlayerView""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/AccountRegistration""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/accounts/admin/me"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/AccountPortalView""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/accounts/password-update/init"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EmptyResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/PasswordUpdateRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        },
        ""/basic/accounts/admin/admin-users"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/GetAdminsResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/accounts/find"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/Account""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""query"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": false
                    }
                ],
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/accounts/available/device-id"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/AccountAvailableResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""deviceId"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": false
                    }
                ],
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        },
        ""/basic/accounts/available"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/AccountAvailableResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""email"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": false
                    }
                ],
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        },
        ""/basic/accounts/admin/new"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/Account""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/AddAccountRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/accounts/"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/Account""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""email"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": false
                    },
                    {
                        ""name"": ""gamerTag"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""integer""
                        },
                        ""required"": false
                    },
                    {
                        ""name"": ""thirdPartyAssoc"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""unknown""
                        },
                        ""required"": false
                    },
                    {
                        ""name"": ""withRealmMigration"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""boolean""
                        },
                        ""required"": false
                    },
                    {
                        ""name"": ""customerScoped"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""boolean""
                        },
                        ""required"": false
                    },
                    {
                        ""name"": ""deviceId"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": false
                    }
                ],
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            },
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/Account""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/accounts/password-update/confirm"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EmptyResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/PasswordUpdateConfirmation""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        }
    },
    ""components"": {
        ""schemas"": {
            ""PasswordUpdateConfirmation"": {
                ""properties"": {
                    ""code"": {
                        ""type"": ""string""
                    },
                    ""newPassword"": {
                        ""type"": ""string""
                    },
                    ""email"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Password Update Confirmation"",
                ""type"": ""object""
            },
            ""DeviceIdAvailableRequest"": {
                ""properties"": {
                    ""deviceId"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Device Id Available Request"",
                ""type"": ""object""
            },
            ""AccountUpdate"": {
                ""properties"": {
                    ""thirdParty"": {
                        ""type"": ""string""
                    },
                    ""hasThirdPartyToken"": {
                        ""type"": ""boolean""
                    },
                    ""country"": {
                        ""type"": ""string""
                    },
                    ""language"": {
                        ""type"": ""string""
                    },
                    ""gamerTagAssoc"": {
                        ""$ref"": ""#/components/schemas/GamerTagAssociation""
                    },
                    ""token"": {
                        ""type"": ""string""
                    },
                    ""deviceId"": {
                        ""type"": ""string""
                    },
                    ""userName"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Account Update"",
                ""type"": ""object"",
                ""required"": [
                    ""hasThirdPartyToken""
                ]
            },
            ""EmailUpdateRequest"": {
                ""properties"": {
                    ""newEmail"": {
                        ""type"": ""string""
                    },
                    ""codeType"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Email Update Request"",
                ""type"": ""object""
            },
            ""ThirdPartyAssociation"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""name"": {
                        ""type"": ""string""
                    },
                    ""email"": {
                        ""type"": ""string""
                    },
                    ""userBusinessId"": {
                        ""type"": ""string""
                    },
                    ""userAppId"": {
                        ""type"": ""string""
                    },
                    ""meta"": {
                        ""type"": ""object"",
                        ""additionalProperties"": true
                    },
                    ""appId"": {
                        ""type"": ""string""
                    }
                }
            },
            ""DeleteDevicesRequest"": {
                ""properties"": {
                    ""deviceIds"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Delete Devices Request"",
                ""type"": ""object""
            },
            ""ItemCreateRequest"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""contentId"": {
                        ""type"": ""string""
                    },
                    ""properties"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemProperty""
                        }
                    }
                }
            },
            ""AccountPersonallyIdentifiableInformationResponse"": {
                ""properties"": {
                    ""account"": {
                        ""$ref"": ""#/components/schemas/Account""
                    },
                    ""stats"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/StatsResponse""
                        }
                    },
                    ""paymentAudits"": {
                        ""$ref"": ""#/components/schemas/ListAuditResponse""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Account Personally Identifiable Information Response"",
                ""type"": ""object""
            },
            ""InFlightMessage"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""method"": {
                        ""type"": ""string""
                    },
                    ""body"": {
                        ""type"": ""string""
                    },
                    ""path"": {
                        ""type"": ""string""
                    },
                    ""gamerTag"": {
                        ""type"": ""integer""
                    },
                    ""shard"": {
                        ""type"": ""string""
                    },
                    ""service"": {
                        ""type"": ""string""
                    },
                    ""id"": {
                        ""type"": ""string""
                    }
                }
            },
            ""AccountPortalView"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""email"": {
                        ""type"": ""string""
                    },
                    ""roleString"": {
                        ""type"": ""string""
                    },
                    ""scopes"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""id"": {
                        ""type"": ""integer""
                    },
                    ""language"": {
                        ""type"": ""string""
                    },
                    ""roles"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/RoleMapping""
                        }
                    },
                    ""thirdPartyAppAssociations"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""required"": [
                    ""id""
                ]
            },
            ""SearchAccountsRequest"": {
                ""properties"": {
                    ""query"": {
                        ""type"": ""string""
                    },
                    ""page"": {
                        ""type"": ""integer""
                    },
                    ""pagesize"": {
                        ""type"": ""integer""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Search Accounts Request"",
                ""type"": ""object"",
                ""required"": [
                    ""page"",
                    ""pagesize""
                ]
            },
            ""PasswordUpdateRequest"": {
                ""properties"": {
                    ""email"": {
                        ""type"": ""string""
                    },
                    ""codeType"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Password Update Request"",
                ""type"": ""object""
            },
            ""PaymentAuditEntryViewModel"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""providerid"": {
                        ""type"": ""string""
                    },
                    ""history"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/PaymentHistoryEntryViewModel""
                        }
                    },
                    ""txid"": {
                        ""type"": ""integer""
                    },
                    ""providername"": {
                        ""type"": ""string""
                    },
                    ""version"": {
                        ""type"": ""string""
                    },
                    ""obtainItems"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemCreateRequest""
                        }
                    },
                    ""txstate"": {
                        ""type"": ""string""
                    },
                    ""updated"": {
                        ""type"": ""integer""
                    },
                    ""obtainCurrency"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/CurrencyChange""
                        }
                    },
                    ""entitlements"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/EntitlementGenerator""
                        }
                    },
                    ""details"": {
                        ""$ref"": ""#/components/schemas/PaymentDetailsEntryViewModel""
                    },
                    ""replayGuardValue"": {
                        ""type"": ""string""
                    },
                    ""gt"": {
                        ""type"": ""integer""
                    },
                    ""created"": {
                        ""type"": ""integer""
                    }
                },
                ""required"": [
                    ""gt"",
                    ""txid""
                ]
            },
            ""AccountPlayerView"": {
                ""properties"": {
                    ""email"": {
                        ""type"": ""string""
                    },
                    ""deviceIds"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""scopes"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""id"": {
                        ""type"": ""integer""
                    },
                    ""language"": {
                        ""type"": ""string""
                    },
                    ""thirdPartyAppAssociations"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Account Player View"",
                ""type"": ""object"",
                ""required"": [
                    ""id""
                ]
            },
            ""PaymentHistoryEntryViewModel"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""change"": {
                        ""type"": ""string""
                    },
                    ""data"": {
                        ""type"": ""string""
                    },
                    ""timestamp"": {
                        ""type"": ""string""
                    }
                }
            },
            ""AccountAvailableResponse"": {
                ""properties"": {
                    ""available"": {
                        ""type"": ""boolean""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Account Available Response"",
                ""type"": ""object"",
                ""required"": [
                    ""available""
                ]
            },
            ""EntitlementGenerator"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""quantity"": {
                        ""type"": ""integer""
                    },
                    ""claimWindow"": {
                        ""$ref"": ""#/components/schemas/EntitlementClaimWindow""
                    },
                    ""params"": {
                        ""type"": ""object"",
                        ""additionalProperties"": true
                    },
                    ""symbol"": {
                        ""type"": ""string""
                    },
                    ""specialization"": {
                        ""type"": ""string""
                    },
                    ""action"": {
                        ""type"": ""string""
                    }
                }
            },
            ""StatsResponse"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""integer""
                    },
                    ""stats"": {
                        ""type"": ""object"",
                        ""additionalProperties"": true
                    }
                },
                ""required"": [
                    ""id""
                ]
            },
            ""RoleMapping"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""projectId"": {
                        ""type"": ""string""
                    },
                    ""role"": {
                        ""type"": ""string""
                    }
                }
            },
            ""AccountRegistration"": {
                ""properties"": {
                    ""email"": {
                        ""type"": ""string""
                    },
                    ""password"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Account Registration"",
                ""type"": ""object""
            },
            ""EmailUpdateConfirmation"": {
                ""properties"": {
                    ""code"": {
                        ""type"": ""string""
                    },
                    ""password"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Email Update Confirmation"",
                ""type"": ""object""
            },
            ""GetAccountRequest"": {
                ""properties"": {
                    ""email"": {
                        ""type"": ""string""
                    },
                    ""gamerTag"": {
                        ""type"": ""integer""
                    },
                    ""thirdPartyAssoc"": {
                        ""$ref"": ""#/components/schemas/ThirdPartyAssociation""
                    },
                    ""withRealmMigration"": {
                        ""type"": ""boolean""
                    },
                    ""customerScoped"": {
                        ""type"": ""boolean""
                    },
                    ""deviceId"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Account Request"",
                ""type"": ""object""
            },
            ""GetAdminsResponse"": {
                ""properties"": {
                    ""accounts"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/AccountPortalView""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Admins Response"",
                ""type"": ""object""
            },
            ""PaymentDetailsEntryViewModel"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""reference"": {
                        ""type"": ""string""
                    },
                    ""name"": {
                        ""type"": ""string""
                    },
                    ""quantity"": {
                        ""type"": ""integer""
                    },
                    ""sku"": {
                        ""type"": ""string""
                    },
                    ""price"": {
                        ""type"": ""integer""
                    },
                    ""subcategory"": {
                        ""type"": ""string""
                    },
                    ""gameplace"": {
                        ""type"": ""string""
                    },
                    ""localPrice"": {
                        ""type"": ""string""
                    },
                    ""category"": {
                        ""type"": ""string""
                    },
                    ""localCurrency"": {
                        ""type"": ""string""
                    },
                    ""providerProductId"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""price"",
                    ""quantity""
                ]
            },
            ""CurrencyChange"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""symbol"": {
                        ""type"": ""string""
                    },
                    ""amount"": {
                        ""type"": ""integer""
                    },
                    ""originalAmount"": {
                        ""type"": ""integer""
                    }
                },
                ""required"": [
                    ""amount""
                ]
            },
            ""AddAccountRequest"": {
                ""properties"": {
                    ""email"": {
                        ""type"": ""string""
                    },
                    ""role"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Add Account Request"",
                ""type"": ""object""
            },
            ""EntitlementClaimWindow"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""open"": {
                        ""type"": ""integer""
                    },
                    ""close"": {
                        ""type"": ""integer""
                    }
                },
                ""required"": [
                    ""open"",
                    ""close""
                ]
            },
            ""GamerTagAssociation"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""projectId"": {
                        ""type"": ""string""
                    },
                    ""gamerTag"": {
                        ""type"": ""integer""
                    }
                },
                ""required"": [
                    ""gamerTag""
                ]
            },
            ""EmptyResponse"": {
                ""properties"": {
                    ""result"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Empty Response"",
                ""type"": ""object""
            },
            ""ItemProperty"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""name"": {
                        ""type"": ""string""
                    },
                    ""value"": {
                        ""type"": ""string""
                    }
                }
            },
            ""ThirdPartyAvailableRequest"": {
                ""properties"": {
                    ""thirdParty"": {
                        ""type"": ""string""
                    },
                    ""token"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Third Party Available Request"",
                ""type"": ""object""
            },
            ""AccountSearchResponse"": {
                ""properties"": {
                    ""accounts"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/Account""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Account Search Response"",
                ""type"": ""object""
            },
            ""ListAuditResponse"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""audits"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/PaymentAuditEntryViewModel""
                        }
                    }
                }
            },
            ""AccountAvailableRequest"": {
                ""properties"": {
                    ""email"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Account Available Request"",
                ""type"": ""object""
            },
            ""FindAccountRequest"": {
                ""properties"": {
                    ""query"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Find Account Request"",
                ""type"": ""object""
            },
            ""Account"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""inFlight"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/InFlightMessage""
                        }
                    },
                    ""createdTimeMillis"": {
                        ""type"": ""integer""
                    },
                    ""realmId"": {
                        ""type"": ""string""
                    },
                    ""email"": {
                        ""type"": ""string""
                    },
                    ""roleString"": {
                        ""type"": ""string""
                    },
                    ""deviceIds"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""privilegedAccount"": {
                        ""type"": ""boolean""
                    },
                    ""country"": {
                        ""type"": ""string""
                    },
                    ""wasMigrated"": {
                        ""type"": ""boolean""
                    },
                    ""id"": {
                        ""type"": ""integer""
                    },
                    ""gamerTags"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/GamerTagAssociation""
                        }
                    },
                    ""language"": {
                        ""type"": ""string""
                    },
                    ""roles"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/RoleMapping""
                        }
                    },
                    ""updatedTimeMillis"": {
                        ""type"": ""integer""
                    },
                    ""thirdParties"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ThirdPartyAssociation""
                        }
                    },
                    ""deviceId"": {
                        ""type"": ""string""
                    },
                    ""userName"": {
                        ""type"": ""string""
                    },
                    ""heartbeat"": {
                        ""type"": ""integer""
                    },
                    ""password"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""id"",
                    ""createdTimeMillis"",
                    ""updatedTimeMillis"",
                    ""privilegedAccount""
                ]
            }
        },
        ""securitySchemes"": {
            ""userRequired"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-GAMERTAG"",
                ""in"": ""header"",
                ""description"": ""Gamer Tag of the player.""
            },
            ""scope"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-SCOPE"",
                ""in"": ""header"",
                ""description"": ""Customer and project scope. This should contain the '<customer-id>.<project-id>'.""
            },
            ""api"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-SIGNATURE"",
                ""in"": ""header"",
                ""description"": ""Signed Request authentication using project secret key.""
            },
            ""user"": {
                ""type"": ""http"",
                ""description"": ""Bearer authentication with an player access token in the Authorization header."",
                ""scheme"": ""bearer"",
                ""bearerFormat"": ""Bearer <Access Token>""
            }
        }
    },
    ""security"": [],
    ""externalDocs"": {
        ""description"": ""Beamable Documentation"",
        ""url"": ""https://docs.beamable.com""
    },
    ""openapi"": ""3.0.2""
}";
	#endregion

	#region account object

	public const string AccountObjectOpenApi = @"{
    ""info"": {
        ""title"": ""accounts object"",
        ""version"": ""1.0"",
        ""contact"": {
            ""name"": ""Beamable Support"",
            ""url"": ""https://api.beamable.com"",
            ""email"": ""support@beamable.com""
        }
    },
    ""servers"": [
        {
            ""url"": ""https://api.beamable.com""
        }
    ],
    ""paths"": {
        ""/object/accounts/{objectId}/admin/email"": {
            ""put"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/Account""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""description"": ""AccountId of the player. Underlying objectId type is integer in format int64."",
                        ""required"": true,
                        ""x-beamable-object-id"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/EmailUpdateRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/object/accounts/{objectId}/available-roles"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/AvailableRolesResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""description"": ""AccountId of the player. Underlying objectId type is integer in format int64."",
                        ""required"": true,
                        ""x-beamable-object-id"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    }
                ],
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        },
        ""/object/accounts/{objectId}/role/report"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/AccountRolesReport""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""description"": ""AccountId of the player. Underlying objectId type is integer in format int64."",
                        ""required"": true,
                        ""x-beamable-object-id"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    }
                ],
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/object/accounts/{objectId}/role"": {
            ""put"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EmptyResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""description"": ""AccountId of the player. Underlying objectId type is integer in format int64."",
                        ""required"": true,
                        ""x-beamable-object-id"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/UpdateRole""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            },
            ""delete"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EmptyResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""description"": ""AccountId of the player. Underlying objectId type is integer in format int64."",
                        ""required"": true,
                        ""x-beamable-object-id"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/DeleteRole""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/object/accounts/{objectId}/admin/scope"": {
            ""put"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EmptyResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""description"": ""AccountId of the player. Underlying objectId type is integer in format int64."",
                        ""required"": true,
                        ""x-beamable-object-id"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/UpdateRole""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            },
            ""delete"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EmptyResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""description"": ""AccountId of the player. Underlying objectId type is integer in format int64."",
                        ""required"": true,
                        ""x-beamable-object-id"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/DeleteRole""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/object/accounts/{objectId}/admin/third-party"": {
            ""put"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EmptyResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""description"": ""AccountId of the player. Underlying objectId type is integer in format int64."",
                        ""required"": true,
                        ""x-beamable-object-id"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/TransferThirdPartyAssociation""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            },
            ""delete"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EmptyResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""description"": ""AccountId of the player. Underlying objectId type is integer in format int64."",
                        ""required"": true,
                        ""x-beamable-object-id"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/DeleteThirdPartyAssociation""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/object/accounts/{objectId}/"": {
            ""put"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/Account""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""description"": ""AccountId of the player. Underlying objectId type is integer in format int64."",
                        ""required"": true,
                        ""x-beamable-object-id"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/AccountUpdate""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/object/accounts/{objectId}/admin/forget"": {
            ""delete"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/Account""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""description"": ""AccountId of the player. Underlying objectId type is integer in format int64."",
                        ""required"": true,
                        ""x-beamable-object-id"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    }
                ],
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        }
    },
    ""components"": {
        ""schemas"": {
            ""AccountUpdate"": {
                ""properties"": {
                    ""thirdParty"": {
                        ""type"": ""string""
                    },
                    ""hasThirdPartyToken"": {
                        ""type"": ""boolean""
                    },
                    ""country"": {
                        ""type"": ""string""
                    },
                    ""language"": {
                        ""type"": ""string""
                    },
                    ""gamerTagAssoc"": {
                        ""$ref"": ""#/components/schemas/GamerTagAssociation""
                    },
                    ""token"": {
                        ""type"": ""string""
                    },
                    ""deviceId"": {
                        ""type"": ""string""
                    },
                    ""userName"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Account Update"",
                ""type"": ""object"",
                ""required"": [
                    ""hasThirdPartyToken""
                ]
            },
            ""EmailUpdateRequest"": {
                ""properties"": {
                    ""newEmail"": {
                        ""type"": ""string""
                    },
                    ""codeType"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Email Update Request"",
                ""type"": ""object"",
                ""required"": [
                    ""newEmail""
                ]
            },
            ""ThirdPartyAssociation"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""name"": {
                        ""type"": ""string""
                    },
                    ""email"": {
                        ""type"": ""string""
                    },
                    ""userBusinessId"": {
                        ""type"": ""string""
                    },
                    ""userAppId"": {
                        ""type"": ""string""
                    },
                    ""meta"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""string""
                        }
                    },
                    ""appId"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""name"",
                    ""appId"",
                    ""userAppId"",
                    ""meta""
                ]
            },
            ""InFlightMessage"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""method"": {
                        ""type"": ""string""
                    },
                    ""body"": {
                        ""type"": ""string""
                    },
                    ""path"": {
                        ""type"": ""string""
                    },
                    ""gamerTag"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""shard"": {
                        ""type"": ""string""
                    },
                    ""service"": {
                        ""type"": ""string""
                    },
                    ""id"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""service"",
                    ""id"",
                    ""method"",
                    ""path"",
                    ""body""
                ]
            },
            ""AccountRolesReport"": {
                ""properties"": {
                    ""accountId"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""email"": {
                        ""type"": ""string""
                    },
                    ""realms"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/RealmRolesReport""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Account Roles Report"",
                ""type"": ""object"",
                ""required"": [
                    ""accountId"",
                    ""email"",
                    ""realms""
                ]
            },
            ""DeleteThirdPartyAssociation"": {
                ""properties"": {
                    ""thirdParty"": {
                        ""type"": ""string""
                    },
                    ""userAppId"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Delete Third Party Association"",
                ""type"": ""object"",
                ""required"": [
                    ""thirdParty"",
                    ""userAppId""
                ]
            },
            ""DeleteRole"": {
                ""properties"": {
                    ""realm"": {
                        ""type"": ""string""
                    },
                    ""role"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Delete Role"",
                ""type"": ""object""
            },
            ""RoleMapping"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""projectId"": {
                        ""type"": ""string""
                    },
                    ""role"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""projectId"",
                    ""role""
                ]
            },
            ""UpdateRole"": {
                ""properties"": {
                    ""cid"": {
                        ""type"": ""string""
                    },
                    ""realm"": {
                        ""type"": ""string""
                    },
                    ""role"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Update Role"",
                ""type"": ""object""
            },
            ""AvailableRolesResponse"": {
                ""properties"": {
                    ""roles"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Available Roles Response"",
                ""type"": ""object"",
                ""required"": [
                    ""roles""
                ]
            },
            ""RealmRolesReport"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""realmName"": {
                        ""type"": ""string""
                    },
                    ""realmDisplayName"": {
                        ""type"": ""string""
                    },
                    ""roles"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""required"": [
                    ""realmName"",
                    ""realmDisplayName"",
                    ""roles""
                ]
            },
            ""GamerTagAssociation"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""projectId"": {
                        ""type"": ""string""
                    },
                    ""gamerTag"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""required"": [
                    ""projectId"",
                    ""gamerTag""
                ]
            },
            ""EmptyResponse"": {
                ""properties"": {
                    ""result"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Empty Response"",
                ""type"": ""object"",
                ""required"": [
                    ""result""
                ]
            },
            ""TransferThirdPartyAssociation"": {
                ""properties"": {
                    ""fromAccountId"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""thirdParty"": {
                        ""$ref"": ""#/components/schemas/ThirdPartyAssociation""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Transfer Third Party Association"",
                ""type"": ""object"",
                ""required"": [
                    ""fromAccountId"",
                    ""thirdParty""
                ]
            },
            ""Account"": {
                ""properties"": {
                    ""inFlight"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/InFlightMessage""
                        }
                    },
                    ""createdTimeMillis"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""realmId"": {
                        ""type"": ""string""
                    },
                    ""email"": {
                        ""type"": ""string""
                    },
                    ""roleString"": {
                        ""type"": ""string""
                    },
                    ""deviceIds"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""privilegedAccount"": {
                        ""type"": ""boolean""
                    },
                    ""country"": {
                        ""type"": ""string""
                    },
                    ""wasMigrated"": {
                        ""type"": ""boolean""
                    },
                    ""id"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""gamerTags"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/GamerTagAssociation""
                        }
                    },
                    ""language"": {
                        ""type"": ""string""
                    },
                    ""roles"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/RoleMapping""
                        }
                    },
                    ""updatedTimeMillis"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""thirdParties"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ThirdPartyAssociation""
                        }
                    },
                    ""deviceId"": {
                        ""type"": ""string""
                    },
                    ""userName"": {
                        ""type"": ""string""
                    },
                    ""heartbeat"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""password"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Account"",
                ""type"": ""object"",
                ""required"": [
                    ""id"",
                    ""gamerTags"",
                    ""thirdParties"",
                    ""createdTimeMillis"",
                    ""updatedTimeMillis"",
                    ""privilegedAccount""
                ]
            }
        },
        ""securitySchemes"": {
            ""userRequired"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-GAMERTAG"",
                ""in"": ""header"",
                ""description"": ""Gamer Tag of the player.""
            },
            ""scope"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-SCOPE"",
                ""in"": ""header"",
                ""description"": ""Customer and project scope. This should contain the '<customer-id>.<project-id>'.""
            },
            ""api"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-SIGNATURE"",
                ""in"": ""header"",
                ""description"": ""Signed Request authentication using project secret key.""
            },
            ""user"": {
                ""type"": ""http"",
                ""description"": ""Bearer authentication with an player access token in the Authorization header."",
                ""scheme"": ""bearer"",
                ""bearerFormat"": ""Bearer <Access Token>""
            }
        }
    },
    ""security"": [],
    ""externalDocs"": {
        ""description"": ""Beamable Documentation"",
        ""url"": ""https://docs.beamable.com""
    },
    ""openapi"": ""3.0.2""
}";
	#endregion

	#region inventory object

	public const string InventoryObjectOpenApi = @"{
    ""info"": {
        ""title"": ""inventory object"",
        ""version"": ""1.0"",
        ""contact"": {
            ""name"": ""Beamable Support"",
            ""url"": ""https://api.beamable.com"",
            ""email"": ""support@beamable.com""
        }
    },
    ""servers"": [
        {
            ""url"": ""https://api.beamable.com""
        }
    ],
    ""paths"": {
        ""/object/inventory/{objectId}/preview"": {
            ""put"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/PreviewVipBonusResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/InventoryUpdateRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        },
        ""/object/inventory/{objectId}/multipliers"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/MultipliersGetResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    }
                ],
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        },
        ""/object/inventory/{objectId}/proxy/transaction"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/CommonResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/InventoryProxyOperation""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/object/inventory/{objectId}/transaction"": {
            ""delete"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/CommonResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/EndTransactionRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/object/inventory/{objectId}/"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/InventoryView""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""scope"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": false
                    },
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    }
                ],
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            },
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/InventoryView""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/InventoryQueryRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            },
            ""put"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/CommonResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/InventoryUpdateRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        },
        ""/object/inventory/{objectId}/doop"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/DoopRequest""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/DoopRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        },
        ""/object/inventory/{objectId}/proxy/state"": {
            ""put"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/CommonResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/InventoryProxyState""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/object/inventory/{objectId}/transfer"": {
            ""put"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/CommonResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""objectId"",
                        ""in"": ""path"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/TransferRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        }
    },
    ""components"": {
        ""schemas"": {
            ""InventoryRuntimeFlags"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""newInstance"": {
                        ""type"": ""boolean""
                    }
                },
                ""required"": [
                    ""newInstance""
                ]
            },
            ""ItemCreateRequest"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""contentId"": {
                        ""type"": ""string""
                    },
                    ""properties"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemProperty""
                        }
                    }
                },
                ""required"": [
                    ""contentId"",
                    ""properties""
                ]
            },
            ""InventoryObject"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {}
            },
            ""InFlightMessage"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""method"": {
                        ""type"": ""string""
                    },
                    ""body"": {
                        ""type"": ""string""
                    },
                    ""path"": {
                        ""type"": ""string""
                    },
                    ""gamerTag"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""shard"": {
                        ""type"": ""string""
                    },
                    ""service"": {
                        ""type"": ""string""
                    },
                    ""id"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""service"",
                    ""id"",
                    ""method"",
                    ""path"",
                    ""body""
                ]
            },
            ""InventoryProxyUpdateRequest"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""transaction"": {
                        ""type"": ""string""
                    },
                    ""currencies"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    },
                    ""newItems"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemCreateRequest""
                        }
                    }
                },
                ""required"": [
                    ""id"",
                    ""transaction"",
                    ""currencies"",
                    ""newItems""
                ]
            },
            ""ItemGroup"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""items"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/Item""
                        }
                    }
                },
                ""required"": [
                    ""id"",
                    ""items""
                ]
            },
            ""ItemUpdateRequest"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""contentId"": {
                        ""type"": ""string""
                    },
                    ""id"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""properties"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemProperty""
                        }
                    }
                },
                ""required"": [
                    ""contentId"",
                    ""id"",
                    ""properties""
                ]
            },
            ""CommonResponse"": {
                ""properties"": {
                    ""result"": {
                        ""type"": ""string""
                    },
                    ""data"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Common Response"",
                ""type"": ""object"",
                ""required"": [
                    ""result"",
                    ""data""
                ]
            },
            ""InventoryProxyState"": {
                ""properties"": {
                    ""currencies"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    },
                    ""items"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""array"",
                            ""items"": {
                                ""$ref"": ""#/components/schemas/ItemProxy""
                            }
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Inventory Proxy State"",
                ""type"": ""object"",
                ""required"": [
                    ""currencies"",
                    ""items""
                ]
            },
            ""CurrencyPreview"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""amount"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""originalAmount"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""required"": [
                    ""id"",
                    ""amount"",
                    ""originalAmount""
                ]
            },
            ""CurrencyView"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""amount"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""properties"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/CurrencyProperty""
                        }
                    }
                },
                ""required"": [
                    ""id"",
                    ""amount"",
                    ""properties""
                ]
            },
            ""InventoryView"": {
                ""properties"": {
                    ""currencies"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/CurrencyView""
                        }
                    },
                    ""items"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemGroup""
                        }
                    },
                    ""scope"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Inventory View"",
                ""type"": ""object"",
                ""required"": [
                    ""currencies"",
                    ""items""
                ]
            },
            ""InventoryGetRequest"": {
                ""properties"": {
                    ""scope"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Inventory Get Request"",
                ""type"": ""object""
            },
            ""DoopRequest"": {
                ""properties"": {
                    ""stuff"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Doop Request"",
                ""type"": ""object"",
                ""required"": [
                    ""stuff""
                ]
            },
            ""MultipliersGetResponse"": {
                ""properties"": {
                    ""multipliers"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/VipBonus""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Multipliers Get Response"",
                ""type"": ""object"",
                ""required"": [
                    ""multipliers""
                ]
            },
            ""CurrencyProperty"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""name"": {
                        ""type"": ""string""
                    },
                    ""value"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""name"",
                    ""value""
                ]
            },
            ""EndTransactionRequest"": {
                ""properties"": {
                    ""transaction"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""End Transaction Request"",
                ""type"": ""object"",
                ""required"": [
                    ""transaction""
                ]
            },
            ""InventoryProxySettings"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""service"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""service""
                ]
            },
            ""Inventory"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""inFlight"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/InFlightMessage""
                        }
                    },
                    ""currencies"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/Currency""
                        }
                    },
                    ""completedTransactions"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/InventoryTransaction""
                        }
                    },
                    ""inventoryObjects"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/InventoryObject""
                        }
                    },
                    ""vip"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/VipGroup""
                        }
                    },
                    ""flags"": {
                        ""$ref"": ""#/components/schemas/InventoryRuntimeFlags""
                    },
                    ""items"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemGroup""
                        }
                    },
                    ""id"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""nextItemId"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""required"": [
                    ""id"",
                    ""currencies"",
                    ""items"",
                    ""completedTransactions"",
                    ""nextItemId"",
                    ""inventoryObjects""
                ]
            },
            ""Currency"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""amount"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""properties"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/CurrencyProperty""
                        }
                    }
                },
                ""required"": [
                    ""id"",
                    ""amount""
                ]
            },
            ""VipGroup"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""index"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    }
                },
                ""required"": [
                    ""id"",
                    ""index""
                ]
            },
            ""InventoryProxyOperation"": {
                ""properties"": {
                    ""proxyUpdate"": {
                        ""$ref"": ""#/components/schemas/InventoryProxyUpdateRequest""
                    },
                    ""proxySettings"": {
                        ""$ref"": ""#/components/schemas/InventoryProxySettings""
                    },
                    ""newInventory"": {
                        ""$ref"": ""#/components/schemas/Inventory""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Inventory Proxy Operation"",
                ""type"": ""object"",
                ""required"": [
                    ""proxyUpdate"",
                    ""proxySettings"",
                    ""newInventory""
                ]
            },
            ""InventoryUpdateRequest"": {
                ""properties"": {
                    ""currencies"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    },
                    ""empty"": {
                        ""type"": ""boolean""
                    },
                    ""currencyProperties"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""array"",
                            ""items"": {
                                ""$ref"": ""#/components/schemas/CurrencyProperty""
                            }
                        }
                    },
                    ""currencyContentIds"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""applyVipBonus"": {
                        ""type"": ""boolean""
                    },
                    ""itemContentIds"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""updateItems"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemUpdateRequest""
                        }
                    },
                    ""newItems"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemCreateRequest""
                        }
                    },
                    ""transaction"": {
                        ""type"": ""string""
                    },
                    ""deleteItems"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemDeleteRequest""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Inventory Update Request"",
                ""type"": ""object"",
                ""required"": [
                    ""itemContentIds"",
                    ""currencyContentIds"",
                    ""empty""
                ]
            },
            ""Item"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""updatedAt"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""proxyId"": {
                        ""type"": ""string""
                    },
                    ""id"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""properties"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemProperty""
                        }
                    },
                    ""createdAt"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""required"": [
                    ""id"",
                    ""properties""
                ]
            },
            ""ItemProperty"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""name"": {
                        ""type"": ""string""
                    },
                    ""value"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""name"",
                    ""value""
                ]
            },
            ""InventoryQueryRequest"": {
                ""properties"": {
                    ""scopes"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Inventory Query Request"",
                ""type"": ""object""
            },
            ""InventoryTransaction"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""expire"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""required"": [
                    ""id"",
                    ""expire""
                ]
            },
            ""ItemDeleteRequest"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""contentId"": {
                        ""type"": ""string""
                    },
                    ""id"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""required"": [
                    ""contentId"",
                    ""id""
                ]
            },
            ""ItemProxy"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""proxyId"": {
                        ""type"": ""string""
                    },
                    ""properties"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemProperty""
                        }
                    }
                },
                ""required"": [
                    ""proxyId"",
                    ""properties""
                ]
            },
            ""VipBonus"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""currency"": {
                        ""type"": ""string""
                    },
                    ""multiplier"": {
                        ""type"": ""number"",
                        ""format"": ""double""
                    },
                    ""roundToNearest"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    }
                },
                ""required"": [
                    ""currency"",
                    ""multiplier"",
                    ""roundToNearest""
                ]
            },
            ""PreviewVipBonusResponse"": {
                ""properties"": {
                    ""currencies"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/CurrencyPreview""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Preview Vip Bonus Response"",
                ""type"": ""object"",
                ""required"": [
                    ""currencies""
                ]
            },
            ""TransferRequest"": {
                ""properties"": {
                    ""transaction"": {
                        ""type"": ""string""
                    },
                    ""recipientPlayer"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""currencies"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Transfer Request"",
                ""type"": ""object"",
                ""required"": [
                    ""recipientPlayer""
                ]
            }
        },
        ""securitySchemes"": {
            ""userRequired"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-GAMERTAG"",
                ""in"": ""header"",
                ""description"": ""Gamer Tag of the player.""
            },
            ""scope"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-SCOPE"",
                ""in"": ""header"",
                ""description"": ""Customer and project scope. This should contain the '<customer-id>.<project-id>'.""
            },
            ""api"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-SIGNATURE"",
                ""in"": ""header"",
                ""description"": ""Signed Request authentication using project secret key.""
            },
            ""user"": {
                ""type"": ""http"",
                ""description"": ""Bearer authentication with an player access token in the Authorization header."",
                ""scheme"": ""bearer"",
                ""bearerFormat"": ""Bearer <Access Token>""
            }
        }
    },
    ""security"": [],
    ""externalDocs"": {
        ""description"": ""Beamable Documentation"",
        ""url"": ""https://docs.beamable.com""
    },
    ""openapi"": ""3.0.2""
}";

	#endregion

	#region inventory basic
	public const string InventoryBasicOpenApi = @"{
    ""info"": {
        ""title"": ""inventory basic"",
        ""version"": ""1.0"",
        ""contact"": {
            ""name"": ""Beamable Support"",
            ""url"": ""https://api.beamable.com"",
            ""email"": ""support@beamable.com""
        }
    },
    ""servers"": [
        {
            ""url"": ""https://api.beamable.com""
        }
    ],
    ""paths"": {
        ""/basic/inventory/items"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
												#/components/schemas/CurrencyPreview
                                    ""$ref"": ""#/components/schemas/ItemContentResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        },
        ""/basic/inventory/currency"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/CurrencyContentResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""security"": [
                    {
                        ""scope"": []
                    }
                ]
            }
        }
    },
    ""components"": {
        ""schemas"": {
            ""CurrencyContentResponse"": {
                ""properties"": {
                    ""content"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/CurrencyArchetype""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Currency Content Response"",
                ""type"": ""object"",
                ""required"": [
                    ""content""
                ]
            },
            ""CurrencyArchetype"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""symbol"": {
                        ""type"": ""string""
                    },
                    ""proxy"": {
                        ""$ref"": ""#/components/schemas/InventoryProxySettings""
                    },
                    ""clientPermission"": {
                        ""$ref"": ""#/components/schemas/ClientPermission""
                    },
                    ""startingAmount"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""required"": [
                    ""symbol""
                ]
            },
            ""ClientPermission"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""write_self"": {
                        ""type"": ""boolean""
                    }
                },
                ""required"": [
                    ""write_self""
                ]
            },
            ""InventoryProxySettings"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""service"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""service""
                ]
            },
            ""ItemArchetype"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""symbol"": {
                        ""type"": ""string""
                    },
                    ""proxy"": {
                        ""$ref"": ""#/components/schemas/InventoryProxySettings""
                    },
                    ""clientPermission"": {
                        ""$ref"": ""#/components/schemas/ClientPermission""
                    }
                },
                ""required"": [
                    ""symbol""
                ]
            },
            ""ItemContentResponse"": {
                ""properties"": {
                    ""content"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemArchetype""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Item Content Response"",
                ""type"": ""object"",
                ""required"": [
                    ""content""
                ]
            }
        },
        ""securitySchemes"": {
            ""userRequired"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-GAMERTAG"",
                ""in"": ""header"",
                ""description"": ""Gamer Tag of the player.""
            },
            ""scope"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-SCOPE"",
                ""in"": ""header"",
                ""description"": ""Customer and project scope. This should contain the '<customer-id>.<project-id>'.""
            },
            ""api"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-SIGNATURE"",
                ""in"": ""header"",
                ""description"": ""Signed Request authentication using project secret key.""
            },
            ""user"": {
                ""type"": ""http"",
                ""description"": ""Bearer authentication with an player access token in the Authorization header."",
                ""scheme"": ""bearer"",
                ""bearerFormat"": ""Bearer <Access Token>""
            }
        }
    },
    ""security"": [],
    ""externalDocs"": {
        ""description"": ""Beamable Documentation"",
        ""url"": ""https://docs.beamable.com""
    },
    ""openapi"": ""3.0.2""
}";

	#endregion

	#region social basic

	public const string SocialBasicOpenApi = @"{
    ""info"": {
        ""title"": ""social basic"",
        ""version"": ""1.0"",
        ""contact"": {
            ""name"": ""Beamable Support"",
            ""url"": ""https://api.beamable.com"",
            ""email"": ""support@beamable.com""
        }
    },
    ""servers"": [
        {
            ""url"": ""https://api.beamable.com""
        }
    ],
    ""paths"": {
        ""/basic/social/my"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/Social""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/social/friends/invite"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EmptyResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/SendFriendRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            },
            ""delete"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EmptyResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/SendFriendRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/social/friends"": {
            ""delete"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EmptyResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/PlayerIdRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/social/friends/import"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/EmptyResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/ImportFriendsRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/social/friends/make"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/CommonResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/MakeFriendshipRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/social/"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/GetSocialStatusesResponse""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""parameters"": [
                    {
                        ""name"": ""playerIds"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""array"",
                            ""items"": {
                                ""type"": ""string"",
                                ""format"": ""blah""
                            }
                        },
                        ""required"": true
                    }
                ],
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        },
        ""/basic/social/blocked"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/FriendshipStatus""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/PlayerIdRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            },
            ""delete"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/FriendshipStatus""
                                }
                            }
                        }
                    },
                    ""400"": {
                        ""description"": ""Bad Request""
                    }
                },
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/PlayerIdRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": []
                    }
                ]
            }
        }
    },
    ""components"": {
        ""schemas"": {
            ""InvitationDirection"": {
                ""type"": ""string"",
                ""enum"": [
                    ""incoming"",
                    ""outgoing""
                ]
            },
            ""FriendSource"": {
                ""type"": ""string"",
                ""enum"": [
                    ""native"",
                    ""facebook""
                ]
            },
            ""Player"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""playerId"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""playerId""
                ]
            },
            ""CommonResponse"": {
                ""properties"": {
                    ""result"": {
                        ""type"": ""string""
                    },
                    ""data"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Common Response"",
                ""type"": ""object"",
                ""required"": [
                    ""result"",
                    ""data""
                ]
            },
            ""Friend"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""playerId"": {
                        ""type"": ""string""
                    },
                    ""source"": {
                        ""$ref"": ""#/components/schemas/FriendSource""
                    }
                },
                ""required"": [
                    ""playerId"",
                    ""source""
                ]
            },
            ""Invite"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""playerId"": {
                        ""type"": ""string""
                    },
                    ""direction"": {
                        ""$ref"": ""#/components/schemas/InvitationDirection""
                    }
                },
                ""required"": [
                    ""playerId"",
                    ""direction""
                ]
            },
            ""Social"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""playerId"": {
                        ""type"": ""string""
                    },
                    ""friends"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/Friend""
                        }
                    },
                    ""blocked"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/Player""
                        }
                    },
                    ""invites"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/Invite""
                        }
                    }
                },
                ""required"": [
                    ""playerId"",
                    ""friends"",
                    ""blocked"",
                    ""invites""
                ]
            },
            ""GetSocialStatusesResponse"": {
                ""properties"": {
                    ""statuses"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/Social""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Social Statuses Response"",
                ""type"": ""object"",
                ""required"": [
                    ""statuses""
                ]
            },
            ""PlayerIdRequest"": {
                ""properties"": {
                    ""playerId"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Player Id Request"",
                ""type"": ""object"",
                ""required"": [
                    ""playerId""
                ]
            },
            ""EmptyResponse"": {
                ""properties"": {
                    ""result"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Empty Response"",
                ""type"": ""object"",
                ""required"": [
                    ""result""
                ]
            },
            ""FriendshipStatus"": {
                ""properties"": {
                    ""playerId"": {
                        ""type"": ""string""
                    },
                    ""friendId"": {
                        ""type"": ""string""
                    },
                    ""isBlocked"": {
                        ""type"": ""boolean""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Friendship Status"",
                ""type"": ""object"",
                ""required"": [
                    ""playerId"",
                    ""friendId"",
                    ""isBlocked""
                ]
            },
            ""MakeFriendshipRequest"": {
                ""properties"": {
                    ""gamerTag"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Make Friendship Request"",
                ""type"": ""object"",
                ""required"": [
                    ""gamerTag""
                ]
            },
            ""GetSocialStatusesRequest"": {
                ""properties"": {
                    ""playerIds"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Social Statuses Request"",
                ""type"": ""object"",
                ""required"": [
                    ""playerIds""
                ]
            },
            ""ImportFriendsRequest"": {
                ""properties"": {
                    ""source"": {
                        ""type"": ""string""
                    },
                    ""token"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Import Friends Request"",
                ""type"": ""object"",
                ""required"": [
                    ""source"",
                    ""token""
                ]
            },
            ""SendFriendRequest"": {
                ""properties"": {
                    ""gamerTag"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Send Friend Request"",
                ""type"": ""object"",
                ""required"": [
                    ""gamerTag""
                ]
            }
        },
        ""securitySchemes"": {
            ""userRequired"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-GAMERTAG"",
                ""in"": ""header"",
                ""description"": ""Gamer Tag of the player.""
            },
            ""scope"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-SCOPE"",
                ""in"": ""header"",
                ""description"": ""Customer and project scope. This should contain the '<customer-id>.<project-id>'.""
            },
            ""api"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-SIGNATURE"",
                ""in"": ""header"",
                ""description"": ""Signed Request authentication using project secret key.""
            },
            ""user"": {
                ""type"": ""http"",
                ""description"": ""Bearer authentication with an player access token in the Authorization header."",
                ""scheme"": ""bearer"",
                ""bearerFormat"": ""Bearer <Access Token>""
            }
        }
    },
    ""security"": [],
    ""externalDocs"": {
        ""description"": ""Beamable Documentation"",
        ""url"": ""https://docs.beamable.com""
    },
    ""openapi"": ""3.0.2""
}";

	#endregion
}
