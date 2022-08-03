namespace tests;

public static class OpenApiFixtures
{
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
