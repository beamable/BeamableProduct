namespace tests;

public static class OpenApiFixtures
{
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
                }
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
            ""InventoryProxyUpdateRequest"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""integer""
                    },
                    ""transaction"": {
                        ""type"": ""string""
                    },
                    ""currencies"": {
                        ""type"": ""object"",
                        ""additionalProperties"": true
                    },
                    ""newItems"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemCreateRequest""
                        }
                    }
                },
                ""required"": [
                    ""id""
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
                }
            },
            ""ItemUpdateRequest"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""contentId"": {
                        ""type"": ""string""
                    },
                    ""id"": {
                        ""type"": ""integer""
                    },
                    ""properties"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemProperty""
                        }
                    }
                },
                ""required"": [
                    ""id""
                ]
            },
            ""CommonResponse"": {
                ""properties"": {
                    ""result"": {
                        ""type"": ""string""
                    },
                    ""data"": {
                        ""type"": ""object"",
                        ""additionalProperties"": true
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Common Response"",
                ""type"": ""object""
            },
            ""InventoryProxyState"": {
                ""properties"": {
                    ""currencies"": {
                        ""type"": ""object"",
                        ""additionalProperties"": true
                    },
                    ""items"": {
                        ""type"": ""object"",
                        ""additionalProperties"": true
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Inventory Proxy State"",
                ""type"": ""object""
            },
            ""CurrencyPreview"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
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
                        ""type"": ""integer""
                    },
                    ""properties"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/CurrencyProperty""
                        }
                    }
                },
                ""required"": [
                    ""amount""
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
                ""type"": ""object""
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
                ""type"": ""object""
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
                }
            },
            ""EndTransactionRequest"": {
                ""properties"": {
                    ""transaction"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""End Transaction Request"",
                ""type"": ""object""
            },
            ""InventoryProxySettings"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""service"": {
                        ""type"": ""string""
                    }
                }
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
                        ""type"": ""integer""
                    },
                    ""nextItemId"": {
                        ""type"": ""integer""
                    }
                },
                ""required"": [
                    ""id"",
                    ""nextItemId""
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
                        ""type"": ""integer""
                    },
                    ""properties"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/CurrencyProperty""
                        }
                    }
                },
                ""required"": [
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
                        ""type"": ""integer""
                    }
                },
                ""required"": [
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
                ""type"": ""object""
            },
            ""InventoryUpdateRequest"": {
                ""properties"": {
                    ""currencies"": {
                        ""type"": ""object"",
                        ""additionalProperties"": true
                    },
                    ""empty"": {
                        ""type"": ""boolean""
                    },
                    ""currencyProperties"": {
                        ""type"": ""object"",
                        ""additionalProperties"": true
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
                    ""empty""
                ]
            },
            ""Item"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""updatedAt"": {
                        ""type"": ""integer""
                    },
                    ""proxyId"": {
                        ""type"": ""string""
                    },
                    ""id"": {
                        ""type"": ""integer""
                    },
                    ""properties"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ItemProperty""
                        }
                    },
                    ""createdAt"": {
                        ""type"": ""integer""
                    }
                },
                ""required"": [
                    ""id""
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
                }
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
                        ""type"": ""integer""
                    }
                },
                ""required"": [
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
                        ""type"": ""integer""
                    }
                },
                ""required"": [
                    ""id""
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
                        ""type"": ""number""
                    },
                    ""roundToNearest"": {
                        ""type"": ""integer""
                    }
                },
                ""required"": [
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
                ""type"": ""object""
            },
            ""TransferRequest"": {
                ""properties"": {
                    ""transaction"": {
                        ""type"": ""string""
                    },
                    ""recipientPlayer"": {
                        ""type"": ""integer""
                    },
                    ""currencies"": {
                        ""type"": ""object"",
                        ""additionalProperties"": true
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
                ""type"": ""object""
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
                        ""type"": ""integer""
                    }
                }
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
                }
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
                }
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
                ""type"": ""object""
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
}
