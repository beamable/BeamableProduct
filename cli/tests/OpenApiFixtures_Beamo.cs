namespace tests;

public static partial class OpenApiFixtures
{
	public const string BeamoBasic = @"{
    ""info"": {
        ""title"": ""beamo basic"",
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
        ""/basic/beamo/microservice/registrations"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/MicroserviceRegistrationsResponse""
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
                                ""$ref"": ""#/components/schemas/MicroserviceRegistrationsQuery""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""user"": [
                            ""tester""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/microservice/federation/traffic"": {
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
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/MicroserviceRegistrationRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""user"": [
                            ""developer""
                        ]
                    },
                    {
                        ""server"": []
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
                                ""$ref"": ""#/components/schemas/MicroserviceRegistrationRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""user"": [
                            ""developer""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/image/urls"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/PreSignedUrlsResponse""
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
                                ""$ref"": ""#/components/schemas/GetServiceURLsRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""user"": [
                            ""developer""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/metricsUrl"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/GetSignedUrlResponse""
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
                                ""$ref"": ""#/components/schemas/GetMetricsUrlRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""user"": [
                            ""tester""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/microservice/secret"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/MicroserviceSecretResponse""
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
                        ""user"": [
                            ""admin""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/queryLogs/result"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/GetSignedUrlResponse""
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
                                ""$ref"": ""#/components/schemas/Query""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""user"": [
                            ""tester""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/storage/performance"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/PerformanceResponse""
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
                        ""name"": ""endDate"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": false
                    },
                    {
                        ""name"": ""storageObjectName"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    },
                    {
                        ""name"": ""granularity"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    },
                    {
                        ""name"": ""startDate"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": false
                    },
                    {
                        ""name"": ""period"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": false
                    }
                ],
                ""security"": [
                    {
                        ""user"": [
                            ""admin""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/manifests"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/GetManifestsResponse""
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
                        ""name"": ""offset"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""integer"",
                            ""format"": ""int32""
                        },
                        ""required"": false
                    },
                    {
                        ""name"": ""limit"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""integer"",
                            ""format"": ""int32""
                        },
                        ""required"": false
                    },
                    {
                        ""name"": ""archived"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""boolean""
                        },
                        ""required"": false
                    }
                ],
                ""security"": [
                    {
                        ""user"": [
                            ""tester""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/templates"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/GetTemplatesResponse""
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
                        ""user"": [
                            ""tester""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/queryLogs"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/Query""
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
                                ""$ref"": ""#/components/schemas/GetLogsInsightUrlRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""user"": [
                            ""tester""
                        ]
                    },
                    {
                        ""server"": []
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
                                ""$ref"": ""#/components/schemas/Query""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""user"": [
                            ""tester""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/logsUrl"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/GetSignedUrlResponse""
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
                                ""$ref"": ""#/components/schemas/GetLogsUrlRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""user"": [
                            ""tester""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/image/commit"": {
            ""put"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/LambdaResponse""
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
                                ""$ref"": ""#/components/schemas/CommitImageRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""user"": [
                            ""developer""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/uploadAPI"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/GetLambdaURI""
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
                        ""user"": [
                            ""tester""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/status"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/GetStatusResponse""
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
                        ""user"": [
                            ""tester""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/manifest/current"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/GetCurrentManifestResponse""
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
                        ""name"": ""archived"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""boolean""
                        },
                        ""required"": false
                    }
                ],
                ""security"": [
                    {
                        ""user"": [
                            ""tester""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/manifest/pull"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/ManifestChecksums""
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
                                ""$ref"": ""#/components/schemas/PullBeamoManifestRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""user"": [
                            ""developer""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/registry"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/GetElasticContainerRegistryURI""
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
                        ""user"": [
                            ""tester""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/manifest/deploy"": {
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
                ""security"": [
                    {
                        ""user"": [
                            ""developer""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/microservice/federation"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/SupportedFederationsResponse""
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
                                ""$ref"": ""#/components/schemas/MicroserviceRegistrationsQuery""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""user"": [
                            ""tester""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/storage/connection"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/ConnectionString""
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
                        ""user"": [
                            ""admin""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        },
        ""/basic/beamo/manifest"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/GetManifestResponse""
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
                        ""name"": ""id"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    },
                    {
                        ""name"": ""archived"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""boolean""
                        },
                        ""required"": false
                    }
                ],
                ""security"": [
                    {
                        ""user"": [
                            ""tester""
                        ]
                    },
                    {
                        ""server"": []
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
                                    ""$ref"": ""#/components/schemas/PostManifestResponse""
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
                                ""$ref"": ""#/components/schemas/PostManifestRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""user"": [
                            ""developer""
                        ]
                    },
                    {
                        ""server"": []
                    }
                ]
            },
            ""parameters"": [
                {
                    ""name"": ""X-BEAM-SCOPE"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Customer and project scope. This should be in the form of '<customer-id>.<project-id>'."",
                    ""required"": true
                },
                {
                    ""name"": ""X-BEAM-GAMERTAG"",
                    ""in"": ""header"",
                    ""schema"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Override the Gamer Tag of the player. This is generally inferred by the auth token."",
                    ""required"": false
                }
            ]
        }
    },
    ""components"": {
        ""schemas"": {
            ""PullBeamoManifestRequest"": {
                ""properties"": {
                    ""sourceRealmPid"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Pull Beamo Manifest Request"",
                ""type"": ""object"",
                ""required"": [
                    ""sourceRealmPid""
                ]
            },
            ""SupportedFederation"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""type"": {
                        ""$ref"": ""#/components/schemas/FederationType""
                    },
                    ""nameSpace"": {
                        ""type"": ""string""
                    },
                    ""settings"": {
                        ""$ref"": ""#/components/schemas/OptionalJsonNodeWrapper""
                    }
                },
                ""required"": [
                    ""type""
                ]
            },
            ""GetManifestsResponse"": {
                ""properties"": {
                    ""manifests"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ManifestView""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Manifests Response"",
                ""type"": ""object"",
                ""required"": [
                    ""manifests""
                ]
            },
            ""GetLogsUrlRequest"": {
                ""properties"": {
                    ""startTime"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""serviceName"": {
                        ""type"": ""string""
                    },
                    ""nextToken"": {
                        ""type"": ""string""
                    },
                    ""filter"": {
                        ""type"": ""string""
                    },
                    ""endTime"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""limit"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Logs Url Request"",
                ""type"": ""object"",
                ""required"": [
                    ""serviceName""
                ]
            },
            ""GetLogsUrlHeader"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""key"": {
                        ""type"": ""string""
                    },
                    ""value"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""key"",
                    ""value""
                ]
            },
            ""UploadURL"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""key"": {
                        ""type"": ""string""
                    },
                    ""url"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""key"",
                    ""url""
                ]
            },
            ""GetManifestsRequest"": {
                ""properties"": {
                    ""offset"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    },
                    ""limit"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    },
                    ""archived"": {
                        ""type"": ""boolean""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Manifests Request"",
                ""type"": ""object""
            },
            ""GetCurrentManifestRequest"": {
                ""properties"": {
                    ""archived"": {
                        ""type"": ""boolean""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Current Manifest Request"",
                ""type"": ""object""
            },
            ""DatabasePerformanceRequest"": {
                ""properties"": {
                    ""endDate"": {
                        ""type"": ""string""
                    },
                    ""storageObjectName"": {
                        ""type"": ""string""
                    },
                    ""granularity"": {
                        ""type"": ""string""
                    },
                    ""startDate"": {
                        ""type"": ""string""
                    },
                    ""period"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Database Performance Request"",
                ""type"": ""object"",
                ""required"": [
                    ""storageObjectName"",
                    ""granularity""
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
            ""ServiceImageLayers"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""service"": {
                        ""$ref"": ""#/components/schemas/Reference""
                    },
                    ""layers"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""required"": [
                    ""service"",
                    ""layers""
                ]
            },
            ""MicroserviceRegistrationsQuery"": {
                ""properties"": {
                    ""serviceName"": {
                        ""type"": ""string""
                    },
                    ""routingKey"": {
                        ""type"": ""string""
                    },
                    ""federation"": {
                        ""$ref"": ""#/components/schemas/SupportedFederation""
                    },
                    ""localOnly"": {
                        ""type"": ""boolean""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Microservice Registrations Query"",
                ""type"": ""object""
            },
            ""PostManifestRequest"": {
                ""properties"": {
                    ""manifest"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ServiceReference""
                        }
                    },
                    ""comments"": {
                        ""type"": ""string""
                    },
                    ""autoDeploy"": {
                        ""type"": ""boolean""
                    },
                    ""storageReferences"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ServiceStorageReference""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Post Manifest Request"",
                ""type"": ""object"",
                ""required"": [
                    ""manifest""
                ]
            },
            ""ServiceStorageStatus"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""storageType"": {
                        ""type"": ""string""
                    },
                    ""isRunning"": {
                        ""type"": ""boolean""
                    },
                    ""isCurrent"": {
                        ""type"": ""boolean""
                    }
                },
                ""required"": [
                    ""id"",
                    ""storageType"",
                    ""isRunning"",
                    ""isCurrent""
                ]
            },
            ""PASlowQuery"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""line"": {
                        ""type"": ""string""
                    },
                    ""namespace"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""line"",
                    ""namespace""
                ]
            },
            ""GetSignedUrlResponse"": {
                ""properties"": {
                    ""headers"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/GetLogsUrlHeader""
                        }
                    },
                    ""url"": {
                        ""type"": ""string""
                    },
                    ""body"": {
                        ""type"": ""string""
                    },
                    ""method"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Signed Url Response"",
                ""type"": ""object"",
                ""required"": [
                    ""headers"",
                    ""url"",
                    ""body"",
                    ""method""
                ]
            },
            ""PreSignedUrlsResponse"": {
                ""properties"": {
                    ""response"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/URLResponse""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Pre Signed Urls Response"",
                ""type"": ""object"",
                ""required"": [
                    ""response""
                ]
            },
            ""ConnectionString"": {
                ""properties"": {
                    ""connectionString"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Connection String"",
                ""type"": ""object"",
                ""required"": [
                    ""connectionString""
                ]
            },
            ""ServiceTemplate"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""id""
                ]
            },
            ""MicroserviceRegistrationRequest"": {
                ""properties"": {
                    ""serviceName"": {
                        ""type"": ""string""
                    },
                    ""routingKey"": {
                        ""type"": ""string""
                    },
                    ""federation"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/SupportedFederation""
                        }
                    },
                    ""trafficFilterEnabled"": {
                        ""type"": ""boolean""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Microservice Registration Request"",
                ""type"": ""object"",
                ""required"": [
                    ""serviceName""
                ]
            },
            ""GetManifestRequest"": {
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""archived"": {
                        ""type"": ""boolean""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Manifest Request"",
                ""type"": ""object"",
                ""required"": [
                    ""id""
                ]
            },
            ""DatabaseMeasurement"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""dataPoints"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/DataPoint""
                        }
                    },
                    ""name"": {
                        ""type"": ""string""
                    },
                    ""units"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""dataPoints"",
                    ""name"",
                    ""units""
                ]
            },
            ""MicroserviceRegistrations"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""routingKey"": {
                        ""type"": ""string""
                    },
                    ""serviceName"": {
                        ""type"": ""string""
                    },
                    ""trafficFilterEnabled"": {
                        ""type"": ""boolean""
                    },
                    ""cid"": {
                        ""type"": ""string""
                    },
                    ""pid"": {
                        ""type"": ""string""
                    },
                    ""instanceCount"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    },
                    ""startedById"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""federation"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/SupportedFederation""
                        }
                    },
                    ""beamoName"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""instanceCount"",
                    ""serviceName"",
                    ""cid"",
                    ""pid""
                ]
            },
            ""Query"": {
                ""properties"": {
                    ""queryId"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Query"",
                ""type"": ""object"",
                ""required"": [
                    ""queryId""
                ]
            },
            ""URLResponse"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""serviceName"": {
                        ""type"": ""string""
                    },
                    ""s3URLs"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/UploadURL""
                        }
                    }
                },
                ""required"": [
                    ""serviceName"",
                    ""s3URLs""
                ]
            },
            ""ManifestView"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""createdByAccountId"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""checksum"": {
                        ""type"": ""string""
                    },
                    ""storageReference"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ServiceStorageReference""
                        }
                    },
                    ""manifest"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ServiceReference""
                        }
                    },
                    ""created"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""comments"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""id"",
                    ""manifest"",
                    ""created"",
                    ""checksum""
                ]
            },
            ""GetLogsInsightUrlRequest"": {
                ""properties"": {
                    ""startTime"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""serviceName"": {
                        ""type"": ""string""
                    },
                    ""filter"": {
                        ""type"": ""string""
                    },
                    ""endTime"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""order"": {
                        ""type"": ""string""
                    },
                    ""filters"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""limit"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Logs Insight Url Request"",
                ""type"": ""object"",
                ""required"": [
                    ""serviceName""
                ]
            },
            ""ServiceDependencyReference"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""storageType"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""id"",
                    ""storageType""
                ]
            },
            ""ManifestChecksum"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""checksum"": {
                        ""type"": ""string""
                    },
                    ""createdAt"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""required"": [
                    ""id"",
                    ""checksum"",
                    ""createdAt""
                ]
            },
            ""DataPoint"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""timestamp"": {
                        ""type"": ""string""
                    },
                    ""value"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""timestamp"",
                    ""value""
                ]
            },
            ""MicroserviceSecretResponse"": {
                ""properties"": {
                    ""secret"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Microservice Secret Response"",
                ""type"": ""object"",
                ""required"": [
                    ""secret""
                ]
            },
            ""Reference"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""arm"": {
                        ""type"": ""boolean""
                    },
                    ""archived"": {
                        ""type"": ""boolean""
                    }
                },
                ""required"": [
                    ""arm"",
                    ""archived""
                ]
            },
            ""ServiceStatus"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""isCurrent"": {
                        ""type"": ""boolean""
                    },
                    ""running"": {
                        ""type"": ""boolean""
                    },
                    ""serviceName"": {
                        ""type"": ""string""
                    },
                    ""imageId"": {
                        ""type"": ""string""
                    },
                    ""serviceDependencyReferences"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ServiceDependencyReference""
                        }
                    }
                },
                ""required"": [
                    ""serviceName"",
                    ""running"",
                    ""imageId"",
                    ""isCurrent""
                ]
            },
            ""MicroserviceRegistrationsResponse"": {
                ""properties"": {
                    ""registrations"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/MicroserviceRegistrations""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Microservice Registrations Response"",
                ""type"": ""object"",
                ""required"": [
                    ""registrations""
                ]
            },
            ""PASuggestedIndex"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""weight"": {
                        ""type"": ""string""
                    },
                    ""impact"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""namespace"": {
                        ""type"": ""string""
                    },
                    ""index"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""required"": [
                    ""id"",
                    ""impact"",
                    ""index"",
                    ""namespace"",
                    ""weight""
                ]
            },
            ""GetLambdaURI"": {
                ""properties"": {
                    ""uri"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Lambda URI"",
                ""type"": ""object"",
                ""required"": [
                    ""uri""
                ]
            },
            ""GetManifestResponse"": {
                ""properties"": {
                    ""manifest"": {
                        ""$ref"": ""#/components/schemas/ManifestView""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Manifest Response"",
                ""type"": ""object"",
                ""required"": [
                    ""manifest""
                ]
            },
            ""DatabaseMeasurements"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""measurements"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/DatabaseMeasurement""
                        }
                    },
                    ""groupId"": {
                        ""type"": ""string""
                    },
                    ""links"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/Link""
                        }
                    },
                    ""hostId"": {
                        ""type"": ""string""
                    },
                    ""granularity"": {
                        ""type"": ""string""
                    },
                    ""end"": {
                        ""type"": ""string""
                    },
                    ""databaseName"": {
                        ""type"": ""string""
                    },
                    ""start"": {
                        ""type"": ""string""
                    },
                    ""processId"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""databaseName"",
                    ""links""
                ]
            },
            ""ServiceReference"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""containerHealthCheckPort"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""archived"": {
                        ""type"": ""boolean""
                    },
                    ""serviceName"": {
                        ""type"": ""string""
                    },
                    ""enabled"": {
                        ""type"": ""boolean""
                    },
                    ""components"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ServiceComponent""
                        }
                    },
                    ""arm"": {
                        ""type"": ""boolean""
                    },
                    ""checksum"": {
                        ""type"": ""string""
                    },
                    ""templateId"": {
                        ""type"": ""string""
                    },
                    ""imageId"": {
                        ""type"": ""string""
                    },
                    ""imageCpuArch"": {
                        ""type"": ""string""
                    },
                    ""dependencies"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ServiceDependencyReference""
                        }
                    },
                    ""comments"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""serviceName"",
                    ""enabled"",
                    ""imageId"",
                    ""templateId"",
                    ""archived"",
                    ""checksum"",
                    ""arm""
                ]
            },
            ""OptionalJsonNodeWrapper"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""x-beamable-json-object"": true,
                ""properties"": {
                    ""node"": {
                        ""type"": ""string""
                    }
                }
            },
            ""GetStatusResponse"": {
                ""properties"": {
                    ""services"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ServiceStatus""
                        }
                    },
                    ""isCurrent"": {
                        ""type"": ""boolean""
                    },
                    ""storageStatuses"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ServiceStorageStatus""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Status Response"",
                ""type"": ""object"",
                ""required"": [
                    ""services"",
                    ""isCurrent""
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
            ""ServiceComponent"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""name"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""name""
                ]
            },
            ""PerformanceResponse"": {
                ""properties"": {
                    ""namespaces"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/PANamespace""
                        }
                    },
                    ""indexes"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/PASuggestedIndex""
                        }
                    },
                    ""queries"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/PASlowQuery""
                        }
                    },
                    ""databaseMeasurements"": {
                        ""$ref"": ""#/components/schemas/DatabaseMeasurements""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Performance Response"",
                ""type"": ""object"",
                ""required"": [
                    ""namespaces"",
                    ""indexes"",
                    ""queries"",
                    ""databaseMeasurements""
                ]
            },
            ""GetTemplatesResponse"": {
                ""properties"": {
                    ""templates"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ServiceTemplate""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Templates Response"",
                ""type"": ""object"",
                ""required"": [
                    ""templates""
                ]
            },
            ""ServiceStorageReference"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""archived"": {
                        ""type"": ""boolean""
                    },
                    ""enabled"": {
                        ""type"": ""boolean""
                    },
                    ""storageType"": {
                        ""type"": ""string""
                    },
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""checksum"": {
                        ""type"": ""string""
                    },
                    ""templateId"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""id"",
                    ""storageType"",
                    ""enabled"",
                    ""archived"",
                    ""checksum""
                ]
            },
            ""SupportedFederationRegistration"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""serviceName"": {
                        ""type"": ""string""
                    },
                    ""routingKey"": {
                        ""type"": ""string""
                    },
                    ""federation"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/SupportedFederation""
                        }
                    },
                    ""trafficFilterEnabled"": {
                        ""type"": ""boolean""
                    }
                },
                ""required"": [
                    ""serviceName"",
                    ""trafficFilterEnabled""
                ]
            },
            ""SupportedFederationsResponse"": {
                ""properties"": {
                    ""registrations"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/SupportedFederationRegistration""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Supported Federations Response"",
                ""type"": ""object"",
                ""required"": [
                    ""registrations""
                ]
            },
            ""ManifestChecksums"": {
                ""properties"": {
                    ""manifests"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ManifestChecksum""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Manifest Checksums"",
                ""type"": ""object"",
                ""required"": [
                    ""manifests""
                ]
            },
            ""LambdaResponse"": {
                ""properties"": {
                    ""statusCode"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    },
                    ""body"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Lambda Response"",
                ""type"": ""object"",
                ""required"": [
                    ""statusCode""
                ]
            },
            ""GetCurrentManifestResponse"": {
                ""properties"": {
                    ""manifest"": {
                        ""$ref"": ""#/components/schemas/ManifestView""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Current Manifest Response"",
                ""type"": ""object"",
                ""required"": [
                    ""manifest""
                ]
            },
            ""GetServiceURLsRequest"": {
                ""properties"": {
                    ""requests"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ServiceImageLayers""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Service UR Ls Request"",
                ""type"": ""object"",
                ""required"": [
                    ""requests""
                ]
            },
            ""FederationType"": {
                ""type"": ""string"",
                ""enum"": [
                    ""IFederatedPlayerInit"",
                    ""IFederatedInventory"",
                    ""IFederatedLogin"",
                    ""IFederatedGameServer"",
                    ""IFederatedCommerce""
                ]
            },
            ""PostManifestResponse"": {
                ""properties"": {
                    ""manifest"": {
                        ""$ref"": ""#/components/schemas/ManifestChecksum""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Post Manifest Response"",
                ""type"": ""object""
            },
            ""GetMetricsUrlRequest"": {
                ""properties"": {
                    ""startTime"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""serviceName"": {
                        ""type"": ""string""
                    },
                    ""metricName"": {
                        ""type"": ""string""
                    },
                    ""endTime"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""period"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Metrics Url Request"",
                ""type"": ""object"",
                ""required"": [
                    ""serviceName"",
                    ""metricName""
                ]
            },
            ""CommitImageRequest"": {
                ""properties"": {
                    ""service"": {
                        ""$ref"": ""#/components/schemas/Reference""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Commit Image Request"",
                ""type"": ""object"",
                ""required"": [
                    ""service""
                ]
            },
            ""GetElasticContainerRegistryURI"": {
                ""properties"": {
                    ""uri"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Elastic Container Registry URI"",
                ""type"": ""object"",
                ""required"": [
                    ""uri""
                ]
            },
            ""PANamespace"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""namespace"": {
                        ""type"": ""string""
                    },
                    ""type"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""namespace"",
                    ""type""
                ]
            },
            ""Link"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""href"": {
                        ""type"": ""string""
                    },
                    ""rel"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""href"",
                    ""rel""
                ]
            }
        },
        ""securitySchemes"": {
            ""server"": {
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
        ""url"": ""https://help.beamable.com/CLI-Latest""
    },
    ""openapi"": ""3.0.2""
}";
}
