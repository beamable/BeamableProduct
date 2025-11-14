namespace tests;

public static partial class OpenApiFixtures
{
	public const string SessionBasic = @"{
    ""info"": {
        ""title"": ""session basic"",
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
        ""/basic/session/heartbeat"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/SessionHeartbeat""
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
        ""/basic/session/history"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/SessionHistoryResponse""
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
                        ""name"": ""dbid"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        },
                        ""required"": true
                    },
                    {
                        ""name"": ""month"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""integer"",
                            ""format"": ""int32""
                        },
                        ""required"": false
                    },
                    {
                        ""name"": ""year"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""integer"",
                            ""format"": ""int32""
                        },
                        ""required"": false
                    }
                ],
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""developer"": []
                    }
                ]
            }
        },
        ""/basic/session/status"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/OnlineStatusResponses""
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
                            ""type"": ""string""
                        },
                        ""required"": true
                    },
                    {
                        ""name"": ""intervalSecs"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        },
                        ""required"": true
                    }
                ],
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""tester"": []
                    }
                ]
            }
        },
        ""/basic/session/client/history"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/SessionClientHistoryResponse""
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
                        ""name"": ""month"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""integer"",
                            ""format"": ""int32""
                        },
                        ""required"": false
                    },
                    {
                        ""name"": ""year"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""integer"",
                            ""format"": ""int32""
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
        ""/basic/session/"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/StartSessionResponse""
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
                                ""$ref"": ""#/components/schemas/StartSessionRequest""
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
            ""SessionHistoryRequest"": {
                ""properties"": {
                    ""dbid"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""month"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    },
                    ""year"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Session History Request"",
                ""type"": ""object"",
                ""required"": [
                    ""dbid""
                ]
            },
            ""SessionHeartbeat"": {
                ""properties"": {
                    ""gt"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""heartbeat"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Session Heartbeat"",
                ""type"": ""object"",
                ""required"": [
                    ""gt""
                ]
            },
            ""Era"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""value"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    }
                },
                ""required"": [
                    ""value""
                ]
            },
            ""OnlineStatusResponses"": {
                ""properties"": {
                    ""players"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/PlayerOnlineStatusResponse""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Online Status Responses"",
                ""type"": ""object"",
                ""required"": [
                    ""players""
                ]
            },
            ""SessionHistoryResponse"": {
                ""properties"": {
                    ""payments"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""totalPaid"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/PaymentTotal""
                        }
                    },
                    ""sessions"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""date"": {
                        ""$ref"": ""#/components/schemas/LocalDate""
                    },
                    ""installDate"": {
                        ""type"": ""string""
                    },
                    ""daysPlayed"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Session History Response"",
                ""type"": ""object"",
                ""required"": [
                    ""date"",
                    ""sessions"",
                    ""payments"",
                    ""totalPaid"",
                    ""daysPlayed""
                ]
            },
            ""LocalDate"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""dayOfYear"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    },
                    ""leapYear"": {
                        ""type"": ""boolean""
                    },
                    ""chronology"": {
                        ""$ref"": ""#/components/schemas/IsoChronology""
                    },
                    ""dayOfWeek"": {
                        ""type"": ""object"",
                        ""format"": ""unknown"",
                        ""enum"": [
                            ""SATURDAY"",
                            ""MONDAY"",
                            ""THURSDAY"",
                            ""TUESDAY"",
                            ""FRIDAY"",
                            ""WEDNESDAY"",
                            ""SUNDAY""
                        ]
                    },
                    ""monthValue"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    },
                    ""dayOfMonth"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    },
                    ""year"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    },
                    ""era"": {
                        ""$ref"": ""#/components/schemas/Era""
                    },
                    ""month"": {
                        ""type"": ""object"",
                        ""format"": ""unknown"",
                        ""enum"": [
                            ""DECEMBER"",
                            ""APRIL"",
                            ""JULY"",
                            ""SEPTEMBER"",
                            ""JUNE"",
                            ""FEBRUARY"",
                            ""OCTOBER"",
                            ""AUGUST"",
                            ""NOVEMBER"",
                            ""MARCH"",
                            ""MAY"",
                            ""JANUARY""
                        ]
                    }
                },
                ""required"": [
                    ""year"",
                    ""month"",
                    ""dayOfYear"",
                    ""dayOfWeek"",
                    ""monthValue"",
                    ""dayOfMonth"",
                    ""chronology"",
                    ""leapYear"",
                    ""era""
                ]
            },
            ""StartSessionRequest"": {
                ""properties"": {
                    ""source"": {
                        ""type"": ""string""
                    },
                    ""customParams"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""string""
                        }
                    },
                    ""shard"": {
                        ""type"": ""string""
                    },
                    ""locale"": {
                        ""type"": ""string""
                    },
                    ""deviceParams"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""string""
                        }
                    },
                    ""language"": {
                        ""$ref"": ""#/components/schemas/SessionLanguageContext""
                    },
                    ""time"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""platform"": {
                        ""type"": ""string""
                    },
                    ""gamer"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""device"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Start Session Request"",
                ""type"": ""object""
            },
            ""CohortEntry"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""trial"": {
                        ""type"": ""string""
                    },
                    ""cohort"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""trial"",
                    ""cohort""
                ]
            },
            ""OnlineStatusRequest"": {
                ""properties"": {
                    ""playerIds"": {
                        ""type"": ""string""
                    },
                    ""intervalSecs"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Online Status Request"",
                ""type"": ""object"",
                ""required"": [
                    ""playerIds"",
                    ""intervalSecs""
                ]
            },
            ""SessionClientHistoryRequest"": {
                ""properties"": {
                    ""month"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    },
                    ""year"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Session Client History Request"",
                ""type"": ""object""
            },
            ""PlayerOnlineStatusResponse"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""playerId"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""online"": {
                        ""type"": ""boolean""
                    },
                    ""lastSeen"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""required"": [
                    ""playerId"",
                    ""online"",
                    ""lastSeen""
                ]
            },
            ""GamerTag"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""tag"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""alias"": {
                        ""type"": ""string""
                    },
                    ""added"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""trials"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/CohortEntry""
                        }
                    },
                    ""platform"": {
                        ""type"": ""string""
                    },
                    ""user"": {
                        ""$ref"": ""#/components/schemas/User""
                    }
                },
                ""required"": [
                    ""tag"",
                    ""platform""
                ]
            },
            ""User"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""name"": {
                        ""type"": ""string""
                    },
                    ""email"": {
                        ""type"": ""string""
                    },
                    ""gamerTag"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""username"": {
                        ""type"": ""string""
                    },
                    ""lastName"": {
                        ""type"": ""string""
                    },
                    ""firstName"": {
                        ""type"": ""string""
                    },
                    ""id"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""cid"": {
                        ""type"": ""string""
                    },
                    ""lang"": {
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
                ""required"": [
                    ""id"",
                    ""email"",
                    ""firstName"",
                    ""lastName"",
                    ""username"",
                    ""gamerTag"",
                    ""lang"",
                    ""name""
                ]
            },
            ""SessionLanguageContext"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""code"": {
                        ""type"": ""string""
                    },
                    ""ctx"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""code"",
                    ""ctx""
                ]
            },
            ""IsoChronology"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""calendarType"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""id"",
                    ""calendarType""
                ]
            },
            ""SessionClientHistoryResponse"": {
                ""properties"": {
                    ""date"": {
                        ""$ref"": ""#/components/schemas/LocalDate""
                    },
                    ""sessions"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""installDate"": {
                        ""type"": ""string""
                    },
                    ""daysPlayed"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Session Client History Response"",
                ""type"": ""object"",
                ""required"": [
                    ""date"",
                    ""sessions"",
                    ""daysPlayed""
                ]
            },
            ""PaymentTotal"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""currencyCode"": {
                        ""type"": ""string""
                    },
                    ""totalRevenue"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""required"": [
                    ""totalRevenue""
                ]
            },
            ""StartSessionResponse"": {
                ""properties"": {
                    ""result"": {
                        ""type"": ""string""
                    },
                    ""gamer"": {
                        ""$ref"": ""#/components/schemas/GamerTag""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Start Session Response"",
                ""type"": ""object"",
                ""required"": [
                    ""result""
                ]
            }
        },
        ""securitySchemes"": {
            ""admin"": {
                ""type"": ""custom"",
                ""description"": ""Requires privileged user with an admin scope.""
            },
            ""scope"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-SCOPE"",
                ""in"": ""header"",
                ""description"": ""Customer and project scope. This should contain the '<customer-id>.<project-id>'.""
            },
            ""userRequired"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-GAMERTAG"",
                ""in"": ""header"",
                ""description"": ""Gamer Tag of the player.""
            },
            ""developer"": {
                ""type"": ""custom"",
                ""description"": ""Requires privileged user with an developer scope.""
            },
            ""tester"": {
                ""type"": ""custom"",
                ""description"": ""Requires privileged user with an tester scope.""
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
        ""url"": ""https://help.beamable.com/CLI-Latest""
    },
    ""openapi"": ""3.0.2""
}";

	#region proto-actor

	public const string ProtoActor = @"{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Beamable API"",
    ""contact"": {
      ""name"": ""Beamable Support"",
      ""url"": ""https://beamable.com/contact-us"",
      ""email"": ""support@beamable.com""
    },
    ""version"": ""1.0"",
    ""x-beamable-semantic-type"": ""Semantic type information for use in automatic SDK generation""
  },
  ""servers"": [
    {
      ""url"": ""https://api.beamable.com""
    }
  ],
  ""paths"": {
    ""/api/auth/refresh-token"": {
      ""post"": {
        ""tags"": [
          ""Auth""
        ],
        ""summary"": ""Generate a new access token for previously authenticated account."",
        ""requestBody"": {
          ""description"": ""`RefreshTokenAuthRequest`"",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/RefreshTokenAuthRequest""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/RefreshTokenAuthRequest""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/RefreshTokenAuthRequest""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/AuthResponse""
                }
              }
            }
          },
          ""400"": {
            ""description"": ""Bad Request"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/ProblemDetails""
                }
              }
            }
          }
        }
      }
    },
    ""/api/auth/server"": {
      ""post"": {
        ""tags"": [
          ""Auth""
        ],
        ""summary"": ""Generate a new access token for a machine with a shared secret"",
        ""requestBody"": {
          ""description"": ""`ServerTokenAuthRequest`"",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/ServerTokenAuthRequest""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/ServerTokenAuthRequest""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/ServerTokenAuthRequest""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/ServerTokenResponse""
                }
              }
            }
          },
          ""400"": {
            ""description"": ""Bad Request"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/ProblemDetails""
                }
              }
            }
          }
        }
      }
    },
    ""/api/lobbies"": {
      ""get"": {
        ""tags"": [
          ""Lobby""
        ],
        ""summary"": ""Query for active lobbies"",
        ""parameters"": [
          {
            ""name"": ""Skip"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""integer"",
              ""format"": ""int32""
            }
          },
          {
            ""name"": ""Limit"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""integer"",
              ""format"": ""int32""
            }
          },
          {
            ""name"": ""MatchType"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""string"",
              ""x-beamable-semantic-type"": ""ContentId""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/LobbyQueryResponse""
                }
              }
            }
          }
        }
      },
      ""post"": {
        ""tags"": [
          ""Lobby""
        ],
        ""summary"": ""Create a lobby. A leader is not necessary to create a lobby."",
        ""requestBody"": {
          ""description"": ""The Create request."",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/CreateLobby""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/CreateLobby""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/CreateLobby""
              }
            }
          }
        },
        ""responses"": {
          ""201"": {
            ""description"": ""Created"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Lobby""
                }
              }
            }
          }
        }
      }
    },
    ""/api/lobbies/{id}"": {
      ""get"": {
        ""tags"": [
          ""Lobby""
        ],
        ""summary"": ""Get the current status of a lobby by id."",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""The lobby id."",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Lobby""
                }
              }
            }
          }
        }
      },
      ""put"": {
        ""tags"": [
          ""Lobby""
        ],
        ""summary"": ""Join a lobby"",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""Id of the lobby"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""requestBody"": {
          ""description"": ""The join lobby request. Includes tags."",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/JoinLobby""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/JoinLobby""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/JoinLobby""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Lobby""
                }
              }
            }
          }
        }
      },
      ""delete"": {
        ""tags"": [
          ""Lobby""
        ],
        ""summary"": ""Remove the requested player from the lobby. The host is able to remove anyone. Others may\r\nonly remove themselves without error."",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""Id of the party"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""requestBody"": {
          ""description"": ""Request including the player requested to remove"",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/RemoveFromLobby""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/RemoveFromLobby""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/RemoveFromLobby""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Acknowledge""
                }
              }
            }
          }
        }
      }
    },
    ""/api/lobbies/passcode"": {
      ""put"": {
        ""tags"": [
          ""Lobby""
        ],
        ""summary"": ""Join a lobby by passcode."",
        ""requestBody"": {
          ""description"": ""The join lobby request. Includes tags."",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/JoinLobbyByPasscode""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/JoinLobbyByPasscode""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/JoinLobbyByPasscode""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Lobby""
                }
              }
            }
          }
        }
      }
    },
    ""/api/lobbies/{id}/metadata"": {
      ""put"": {
        ""tags"": [
          ""Lobby""
        ],
        ""summary"": ""Update the properties of a lobby"",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""Id of the lobby"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""requestBody"": {
          ""description"": ""The update lobby request."",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/UpdateLobby""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/UpdateLobby""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/UpdateLobby""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Lobby""
                }
              }
            }
          }
        }
      }
    },
    ""/api/lobbies/{id}/tags"": {
      ""put"": {
        ""tags"": [
          ""Lobby""
        ],
        ""summary"": ""Add the request tags to the requested player."",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""Id of the party"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""requestBody"": {
          ""description"": ""Includes the player ID and tags to add."",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/AddTags""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/AddTags""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/AddTags""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Lobby""
                }
              }
            }
          }
        }
      },
      ""delete"": {
        ""tags"": [
          ""Lobby""
        ],
        ""summary"": ""Remove the request tags from the requested player."",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""Id of the party"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""requestBody"": {
          ""description"": ""Includes the player ID and the tags to remove."",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/RemoveTags""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/RemoveTags""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/RemoveTags""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Lobby""
                }
              }
            }
          }
        }
      }
    },
    ""/api/mailbox/publish"": {
      ""post"": {
        ""tags"": [
          ""Mailbox""
        ],
        ""requestBody"": {
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/MessageRequest""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/MessageRequest""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/MessageRequest""
              }
            }
          }
        },
        ""responses"": {
          ""204"": {
            ""description"": ""No Content""
          }
        }
      }
    },
    ""/api/matchmaking/matches/{id}"": {
      ""get"": {
        ""tags"": [
          ""Match""
        ],
        ""summary"": ""Fetch a match by ID."",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""Match ID"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Match""
                }
              }
            }
          }
        }
      }
    },
    ""/api/parties"": {
      ""post"": {
        ""tags"": [
          ""Party""
        ],
        ""summary"": ""Create a party for the current player."",
        ""requestBody"": {
          ""description"": ""Argument to pass to the party actor to initialize state."",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/CreateParty""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/CreateParty""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/CreateParty""
              }
            }
          }
        },
        ""responses"": {
          ""201"": {
            ""description"": ""Created"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Party""
                }
              }
            }
          }
        }
      }
    },
    ""/api/parties/{id}/metadata"": {
      ""put"": {
        ""tags"": [
          ""Party""
        ],
        ""summary"": ""Updates party state."",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""Id of the party"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""requestBody"": {
          ""description"": ""Argument to pass to the party actor to update state."",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/UpdateParty""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/UpdateParty""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/UpdateParty""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Party""
                }
              }
            }
          }
        }
      }
    },
    ""/api/parties/{id}"": {
      ""get"": {
        ""tags"": [
          ""Party""
        ],
        ""summary"": ""Return the status of a party."",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""Id of the party"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Party""
                }
              }
            }
          }
        }
      },
      ""put"": {
        ""tags"": [
          ""Party""
        ],
        ""summary"": ""Join a party"",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""Id of the party"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Party""
                }
              }
            }
          }
        }
      }
    },
    ""/api/parties/{id}/promote"": {
      ""put"": {
        ""tags"": [
          ""Party""
        ],
        ""summary"": ""Promote a party member to leader."",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""Id of the party"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""requestBody"": {
          ""description"": ""Player to promote to leader"",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/PromoteNewLeader""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/PromoteNewLeader""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/PromoteNewLeader""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Party""
                }
              }
            }
          }
        }
      }
    },
    ""/api/parties/{id}/invite"": {
      ""post"": {
        ""tags"": [
          ""Party""
        ],
        ""summary"": ""Invite a player to a party"",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""Id of the party"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""requestBody"": {
          ""description"": ""Player to invite to the party"",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/InviteToParty""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/InviteToParty""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/InviteToParty""
              }
            }
          }
        },
        ""responses"": {
          ""204"": {
            ""description"": ""No Content""
          }
        }
      },
      ""delete"": {
        ""tags"": [
          ""Party""
        ],
        ""summary"": ""Cancel party invitation."",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""Id of the party"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""requestBody"": {
          ""description"": ""Player to be uninvited"",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/CancelInviteToParty""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/CancelInviteToParty""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/CancelInviteToParty""
              }
            }
          }
        },
        ""responses"": {
          ""204"": {
            ""description"": ""No Content""
          }
        }
      }
    },
    ""/api/parties/{id}/members"": {
      ""delete"": {
        ""tags"": [
          ""Party""
        ],
        ""summary"": ""Remove the requested player from the party. The leader is able to remove anyone. Others may\r\nonly remove themselves without error."",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""Id of the party"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""requestBody"": {
          ""description"": ""The leave party request"",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/LeaveParty""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/LeaveParty""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/LeaveParty""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    },
    ""/api/players/{playerId}/parties/invites"": {
      ""get"": {
        ""tags"": [
          ""Player""
        ],
        ""summary"": ""Return list of party invites for player."",
        ""parameters"": [
          {
            ""name"": ""playerId"",
            ""in"": ""path"",
            ""description"": ""PlayerId"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/PartyInvitesForPlayerResponse""
                }
              }
            }
          }
        }
      }
    },
    ""/api/players/{playerId}/presence"": {
      ""put"": {
        ""tags"": [
          ""PlayerPresence""
        ],
        ""parameters"": [
          {
            ""name"": ""playerId"",
            ""in"": ""path"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string""
            }
          }
        ],
        ""responses"": {
          ""204"": {
            ""description"": ""No Content""
          }
        }
      },
      ""get"": {
        ""tags"": [
          ""PlayerPresence""
        ],
        ""parameters"": [
          {
            ""name"": ""playerId"",
            ""in"": ""path"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/OnlineStatus""
                }
              }
            }
          }
        }
      }
    },
    ""/api/players/{playerId}/presence/status"": {
      ""put"": {
        ""tags"": [
          ""PlayerPresence""
        ],
        ""parameters"": [
          {
            ""name"": ""playerId"",
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
                ""$ref"": ""#/components/schemas/SetPresenceStatusRequest""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/SetPresenceStatusRequest""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/SetPresenceStatusRequest""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/OnlineStatus""
                }
              }
            }
          }
        }
      }
    },
    ""/api/presence/query"": {
      ""post"": {
        ""tags"": [
          ""Presence""
        ],
        ""requestBody"": {
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/OnlineStatusQuery""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/OnlineStatusQuery""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/OnlineStatusQuery""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/PlayersStatusResponse""
                }
              }
            }
          }
        }
      }
    },
    ""/api/internal/scheduler/job/execute"": {
      ""post"": {
        ""tags"": [
          ""Scheduler""
        ],
        ""summary"": ""Called by the Dispatcher lambda function to start a job execution at the appropriate time."",
        ""requestBody"": {
          ""description"": """",
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/JobExecutionEvent""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/JobExecutionEvent""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/JobExecutionEvent""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/JobExecutionResult""
                }
              }
            }
          }
        }
      }
    },
    ""/api/internal/scheduler/job"": {
      ""post"": {
        ""tags"": [
          ""Scheduler""
        ],
        ""requestBody"": {
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/JobDefinitionSaveRequest""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/JobDefinitionSaveRequest""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/JobDefinitionSaveRequest""
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/JobDefinition""
                }
              }
            }
          }
        }
      }
    },
    ""/api/scheduler/jobs"": {
      ""get"": {
        ""tags"": [
          ""Scheduler""
        ],
        ""parameters"": [
          {
            ""name"": ""source"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""string""
            }
          },
          {
            ""name"": ""name"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""string""
            }
          },
          {
            ""name"": ""limit"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""integer"",
              ""format"": ""int32"",
              ""default"": 1000
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""$ref"": ""#/components/schemas/JobDefinition""
                  }
                }
              }
            }
          }
        }
      }
    },
    ""/api/scheduler/job/{jobId}"": {
      ""get"": {
        ""tags"": [
          ""Scheduler""
        ],
        ""parameters"": [
          {
            ""name"": ""jobId"",
            ""in"": ""path"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/JobDefinition""
                }
              }
            }
          }
        }
      },
      ""delete"": {
        ""tags"": [
          ""Scheduler""
        ],
        ""parameters"": [
          {
            ""name"": ""jobId"",
            ""in"": ""path"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string""
            }
          }
        ],
        ""responses"": {
          ""204"": {
            ""description"": ""No Content""
          }
        }
      }
    },
    ""/api/scheduler/job/{jobId}/activity"": {
      ""get"": {
        ""tags"": [
          ""Scheduler""
        ],
        ""parameters"": [
          {
            ""name"": ""jobId"",
            ""in"": ""path"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string""
            }
          },
          {
            ""name"": ""limit"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""integer"",
              ""format"": ""int32"",
              ""default"": 1000
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""$ref"": ""#/components/schemas/JobActivity""
                  }
                }
              }
            }
          },
          ""400"": {
            ""description"": ""Bad Request"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/ProblemDetails""
                }
              }
            }
          }
        }
      }
    },
    ""/api/scheduler/job/{jobId}/next-executions"": {
      ""get"": {
        ""tags"": [
          ""Scheduler""
        ],
        ""parameters"": [
          {
            ""name"": ""jobId"",
            ""in"": ""path"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string""
            }
          },
          {
            ""name"": ""from"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""date-time""
            }
          },
          {
            ""name"": ""limit"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""integer"",
              ""format"": ""int32"",
              ""default"": 1000
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""type"": ""string"",
                    ""format"": ""date-time""
                  }
                }
              }
            }
          },
          ""400"": {
            ""description"": ""Bad Request"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/ProblemDetails""
                }
              }
            }
          }
        }
      }
    },
    ""/api/matchmaking/tickets"": {
      ""post"": {
        ""tags"": [
          ""Ticket""
        ],
        ""summary"": ""Create a ticket representing 1 or more players to be matched\r\nwith others."",
        ""requestBody"": {
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/TicketReservationRequest""
              }
            },
            ""text/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/TicketReservationRequest""
              }
            },
            ""application/*+json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/TicketReservationRequest""
              }
            }
          }
        },
        ""responses"": {
          ""201"": {
            ""description"": ""Created"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/TicketReservationResponse""
                }
              }
            }
          }
        }
      }
    },
    ""/api/matchmaking/tickets/{id}"": {
      ""get"": {
        ""tags"": [
          ""Ticket""
        ],
        ""summary"": ""Fetch a ticket by ID."",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""Ticket ID"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Ticket""
                }
              }
            }
          }
        }
      },
      ""delete"": {
        ""tags"": [
          ""Ticket""
        ],
        ""summary"": ""Cancel a pending ticket. If no ticket with the id exists, this will\r\nstill return a 204."",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string"",
              ""format"": ""uuid""
            }
          }
        ],
        ""responses"": {
          ""204"": {
            ""description"": ""No Content""
          }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""Acknowledge"": {
        ""type"": ""object"",
        ""additionalProperties"": false
      },
      ""AddTags"": {
        ""type"": ""object"",
        ""properties"": {
          ""playerId"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          },
          ""tags"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/Tag""
            },
            ""nullable"": true,
            ""readOnly"": true
          },
          ""replace"": {
            ""type"": ""boolean""
          }
        },
        ""additionalProperties"": false
      },
      ""AuthResponse"": {
        ""type"": ""object"",
        ""properties"": {
          ""accessToken"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""refreshToken"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      },
      ""CancelInviteToParty"": {
        ""type"": ""object"",
        ""properties"": {
          ""playerId"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          }
        },
        ""additionalProperties"": false
      },
      ""CreateLobby"": {
        ""type"": ""object"",
        ""properties"": {
          ""name"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""description"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""restriction"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""matchType"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""ContentId""
          },
          ""playerTags"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/Tag""
            },
            ""nullable"": true,
            ""readOnly"": true
          },
          ""passcodeLength"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          },
          ""maxPlayers"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          }
        },
        ""additionalProperties"": false
      },
      ""CreateParty"": {
        ""type"": ""object"",
        ""properties"": {
          ""restriction"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""leader"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          },
          ""maxSize"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          }
        },
        ""additionalProperties"": false
      },
      ""CronTrigger"": {
        ""type"": ""object"",
        ""properties"": {
          ""type"": {
            ""type"": ""string""
          },
          ""expression"": {
            ""type"": ""string""
          }
        },
        ""additionalProperties"": false
      },
      ""ExactTrigger"": {
        ""type"": ""object"",
        ""properties"": {
          ""type"": {
            ""type"": ""string""
          },
          ""executeAt"": {
            ""type"": ""string"",
            ""format"": ""date-time""
          }
        },
        ""additionalProperties"": false
      },
      ""HttpCall"": {
        ""type"": ""object"",
        ""properties"": {
          ""type"": {
            ""type"": ""string""
          },
          ""uri"": {
            ""type"": ""string""
          },
          ""method"": {
            ""type"": ""string""
          },
          ""headers"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/StringStringKeyValuePair""
            },
            ""nullable"": true
          },
          ""body"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""contentType"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      },
      ""InviteToParty"": {
        ""type"": ""object"",
        ""properties"": {
          ""playerId"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          }
        },
        ""additionalProperties"": false
      },
      ""JobActivity"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""string""
          },
          ""jobId"": {
            ""type"": ""string""
          },
          ""executionId"": {
            ""type"": ""string""
          },
          ""timestamp"": {
            ""type"": ""string"",
            ""format"": ""date-time""
          },
          ""state"": {
            ""$ref"": ""#/components/schemas/JobState""
          },
          ""message"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      },
      ""JobDefinition"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""string""
          },
          ""name"": {
            ""type"": ""string""
          },
          ""owner"": {
            ""type"": ""string""
          },
          ""triggers"": {
            ""type"": ""array"",
            ""items"": {
              ""oneOf"": [
                {
                  ""$ref"": ""#/components/schemas/CronTrigger""
                },
                {
                  ""$ref"": ""#/components/schemas/ExactTrigger""
                }
              ]
            }
          },
          ""jobAction"": {
            ""oneOf"": [
              {
                ""$ref"": ""#/components/schemas/HttpCall""
              },
              {
                ""$ref"": ""#/components/schemas/PublishMessage""
              },
              {
                ""$ref"": ""#/components/schemas/ServiceCall""
              }
            ]
          },
          ""retryPolicy"": {
            ""$ref"": ""#/components/schemas/JobRetryPolicy""
          },
          ""lastUpdate"": {
            ""type"": ""string"",
            ""format"": ""date-time""
          },
          ""source"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      },
      ""JobDefinitionSaveRequest"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""name"": {
            ""type"": ""string""
          },
          ""triggers"": {
            ""type"": ""array"",
            ""items"": {
              ""oneOf"": [
                {
                  ""$ref"": ""#/components/schemas/CronTrigger""
                },
                {
                  ""$ref"": ""#/components/schemas/ExactTrigger""
                }
              ]
            }
          },
          ""jobAction"": {
            ""oneOf"": [
              {
                ""$ref"": ""#/components/schemas/HttpCall""
              },
              {
                ""$ref"": ""#/components/schemas/PublishMessage""
              },
              {
                ""$ref"": ""#/components/schemas/ServiceCall""
              }
            ]
          },
          ""retryPolicy"": {
            ""$ref"": ""#/components/schemas/JobRetryPolicy""
          },
          ""source"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      },
      ""JobExecutionEvent"": {
        ""type"": ""object"",
        ""properties"": {
          ""jobId"": {
            ""type"": ""string""
          },
          ""executionId"": {
            ""type"": ""string""
          },
          ""executionTime"": {
            ""type"": ""string"",
            ""format"": ""date-time""
          },
          ""retryPolicy"": {
            ""$ref"": ""#/components/schemas/JobRetryPolicy""
          },
          ""retryCount"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          }
        },
        ""additionalProperties"": false
      },
      ""JobExecutionResult"": {
        ""type"": ""object"",
        ""properties"": {
          ""isSuccess"": {
            ""type"": ""boolean""
          },
          ""message"": {
            ""type"": ""string""
          }
        },
        ""additionalProperties"": false
      },
      ""JobRetryPolicy"": {
        ""type"": ""object"",
        ""properties"": {
          ""maxRetryCount"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          },
          ""retryDelayMs"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          },
          ""useExponentialBackoff"": {
            ""type"": ""boolean""
          }
        },
        ""additionalProperties"": false
      },
      ""JobState"": {
        ""enum"": [
          ""ENQUEUED"",
          ""DISPATCHED"",
          ""RUNNING"",
          ""DONE"",
          ""CANCELED"",
          ""ERROR""
        ],
        ""type"": ""string""
      },
      ""JoinLobby"": {
        ""type"": ""object"",
        ""properties"": {
          ""tags"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/Tag""
            },
            ""nullable"": true,
            ""readOnly"": true
          }
        },
        ""additionalProperties"": false
      },
      ""JoinLobbyByPasscode"": {
        ""type"": ""object"",
        ""properties"": {
          ""passcode"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""tags"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/Tag""
            },
            ""nullable"": true,
            ""readOnly"": true
          }
        },
        ""additionalProperties"": false
      },
      ""LeaveParty"": {
        ""type"": ""object"",
        ""properties"": {
          ""playerId"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          }
        },
        ""additionalProperties"": false
      },
      ""Lobby"": {
        ""type"": ""object"",
        ""properties"": {
          ""lobbyId"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""matchType"": {
            ""$ref"": ""#/components/schemas/MatchType""
          },
          ""created"": {
            ""type"": ""string"",
            ""format"": ""date-time"",
            ""nullable"": true
          },
          ""name"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""description"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""host"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""players"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/LobbyPlayer""
            },
            ""nullable"": true,
            ""readOnly"": true
          },
          ""passcode"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""restriction"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""maxPlayers"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          }
        },
        ""additionalProperties"": false
      },
      ""LobbyPlayer"": {
        ""type"": ""object"",
        ""properties"": {
          ""playerId"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          },
          ""tags"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/Tag""
            },
            ""nullable"": true,
            ""readOnly"": true
          },
          ""joined"": {
            ""type"": ""string"",
            ""format"": ""date-time"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      },
      ""LobbyQueryResponse"": {
        ""type"": ""object"",
        ""properties"": {
          ""results"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/Lobby""
            },
            ""nullable"": true,
            ""readOnly"": true
          }
        },
        ""additionalProperties"": false
      },
      ""Match"": {
        ""type"": ""object"",
        ""properties"": {
          ""matchId"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""status"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""created"": {
            ""type"": ""string"",
            ""format"": ""date-time"",
            ""nullable"": true
          },
          ""matchType"": {
            ""$ref"": ""#/components/schemas/MatchType""
          },
          ""teams"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/Team""
            },
            ""nullable"": true,
            ""readOnly"": true
          },
          ""tickets"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/Ticket""
            },
            ""nullable"": true,
            ""readOnly"": true
          }
        },
        ""additionalProperties"": false
      },
      ""MatchType"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""ContentId""
          },
          ""teams"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/TeamContentProto""
            },
            ""nullable"": true,
            ""readOnly"": true
          },
          ""waitAfterMinReachedSecs"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          },
          ""maxWaitDurationSecs"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          },
          ""matchingIntervalSecs"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          }
        },
        ""additionalProperties"": false
      },
      ""MessageRequest"": {
        ""type"": ""object"",
        ""properties"": {
          ""body"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""cid"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""pid"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""playerId"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          }
        },
        ""additionalProperties"": false
      },
      ""OnlineStatus"": {
        ""type"": ""object"",
        ""properties"": {
          ""online"": {
            ""type"": ""boolean""
          },
          ""lastOnline"": {
            ""type"": ""string"",
            ""format"": ""date-time"",
            ""nullable"": true
          },
          ""playerId"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          },
          ""status"": {
            ""$ref"": ""#/components/schemas/PresenceStatus""
          },
          ""description"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      },
      ""OnlineStatusQuery"": {
        ""type"": ""object"",
        ""properties"": {
          ""playerIds"": {
            ""type"": ""array"",
            ""items"": {
              ""type"": ""string""
            }
          },
          ""toManyRequests"": {
            ""type"": ""boolean"",
            ""readOnly"": true
          }
        },
        ""additionalProperties"": false
      },
      ""Party"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""restriction"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""leader"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          },
          ""members"": {
            ""type"": ""array"",
            ""items"": {
              ""type"": ""string""
            },
            ""nullable"": true,
            ""readOnly"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          },
          ""maxSize"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          },
          ""pendingInvites"": {
            ""type"": ""array"",
            ""items"": {
              ""type"": ""string""
            },
            ""nullable"": true,
            ""readOnly"": true
          }
        },
        ""additionalProperties"": false
      },
      ""PartyInvitation"": {
        ""type"": ""object"",
        ""properties"": {
          ""partyId"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""invitedBy"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      },
      ""PartyInvitesForPlayerResponse"": {
        ""type"": ""object"",
        ""properties"": {
          ""invitations"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/PartyInvitation""
            },
            ""nullable"": true,
            ""readOnly"": true
          }
        },
        ""additionalProperties"": false
      },
      ""PlayersStatusResponse"": {
        ""type"": ""object"",
        ""properties"": {
          ""playersStatus"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/OnlineStatus""
            }
          }
        },
        ""additionalProperties"": false
      },
      ""PresenceStatus"": {
        ""enum"": [
          ""Visible"",
          ""Invisible"",
          ""Dnd"",
          ""Away""
        ],
        ""type"": ""string""
      },
      ""ProblemDetails"": {
        ""type"": ""object"",
        ""properties"": {
          ""type"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""title"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""status"": {
            ""type"": ""integer"",
            ""format"": ""int32"",
            ""nullable"": true
          },
          ""detail"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""instance"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": { }
      },
      ""PromoteNewLeader"": {
        ""type"": ""object"",
        ""properties"": {
          ""playerId"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          }
        },
        ""additionalProperties"": false
      },
      ""PublishMessage"": {
        ""type"": ""object"",
        ""properties"": {
          ""type"": {
            ""type"": ""string""
          },
          ""topic"": {
            ""type"": ""string""
          },
          ""message"": {
            ""type"": ""string""
          },
          ""persist"": {
            ""type"": ""boolean""
          },
          ""headers"": {
            ""type"": ""object"",
            ""additionalProperties"": {
              ""type"": ""string""
            },
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      },
      ""RefreshTokenAuthRequest"": {
        ""type"": ""object"",
        ""properties"": {
          ""refreshToken"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""customerId"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""Cid""
          },
          ""realmId"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""Pid""
          }
        },
        ""additionalProperties"": false
      },
      ""RemoveFromLobby"": {
        ""type"": ""object"",
        ""properties"": {
          ""playerId"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          }
        },
        ""additionalProperties"": false
      },
      ""RemoveTags"": {
        ""type"": ""object"",
        ""properties"": {
          ""playerId"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          },
          ""tags"": {
            ""type"": ""array"",
            ""items"": {
              ""type"": ""string""
            },
            ""nullable"": true,
            ""readOnly"": true
          }
        },
        ""additionalProperties"": false
      },
      ""ServerTokenAuthRequest"": {
        ""type"": ""object"",
        ""properties"": {
          ""clientId"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""clientSecret"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""customerId"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""Cid""
          },
          ""realmId"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""Pid""
          }
        },
        ""additionalProperties"": false
      },
      ""ServerTokenResponse"": {
        ""type"": ""object"",
        ""properties"": {
          ""accessToken"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      },
      ""ServiceCall"": {
        ""type"": ""object"",
        ""properties"": {
          ""type"": {
            ""type"": ""string""
          },
          ""uri"": {
            ""type"": ""string""
          },
          ""method"": {
            ""type"": ""string""
          },
          ""body"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      },
      ""SetPresenceStatusRequest"": {
        ""type"": ""object"",
        ""properties"": {
          ""status"": {
            ""$ref"": ""#/components/schemas/PresenceStatus""
          },
          ""description"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      },
      ""StringStringKeyValuePair"": {
        ""type"": ""object"",
        ""properties"": {
          ""key"": {
            ""type"": ""string""
          },
          ""value"": {
            ""type"": ""string""
          }
        },
        ""additionalProperties"": false
      },
      ""Tag"": {
        ""type"": ""object"",
        ""properties"": {
          ""name"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""value"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      },
      ""Team"": {
        ""type"": ""object"",
        ""properties"": {
          ""name"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""players"": {
            ""type"": ""array"",
            ""items"": {
              ""type"": ""string""
            },
            ""nullable"": true,
            ""readOnly"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          }
        },
        ""additionalProperties"": false
      },
      ""TeamContentProto"": {
        ""type"": ""object"",
        ""properties"": {
          ""name"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""maxPlayers"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          },
          ""minPlayers"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          }
        },
        ""additionalProperties"": false
      },
      ""Ticket"": {
        ""type"": ""object"",
        ""properties"": {
          ""ticketId"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""status"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""created"": {
            ""type"": ""string"",
            ""format"": ""date-time"",
            ""nullable"": true
          },
          ""expires"": {
            ""type"": ""string"",
            ""format"": ""date-time"",
            ""nullable"": true
          },
          ""players"": {
            ""type"": ""array"",
            ""items"": {
              ""type"": ""string""
            },
            ""nullable"": true,
            ""readOnly"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          },
          ""matchType"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""ContentId""
          },
          ""matchId"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""stringProperties"": {
            ""type"": ""object"",
            ""additionalProperties"": {
              ""type"": ""string""
            },
            ""nullable"": true,
            ""readOnly"": true
          },
          ""numberProperties"": {
            ""type"": ""object"",
            ""additionalProperties"": {
              ""type"": ""number"",
              ""format"": ""double""
            },
            ""nullable"": true,
            ""readOnly"": true
          },
          ""team"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""priority"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          },
          ""partyId"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""watchOnlineStatus"": {
            ""type"": ""boolean""
          }
        },
        ""additionalProperties"": false
      },
      ""TicketReservationRequest"": {
        ""type"": ""object"",
        ""properties"": {
          ""players"": {
            ""type"": ""array"",
            ""items"": {
              ""type"": ""string""
            },
            ""nullable"": true,
            ""readOnly"": true,
            ""x-beamable-semantic-type"": ""GamerTag""
          },
          ""matchTypes"": {
            ""type"": ""array"",
            ""items"": {
              ""type"": ""string""
            },
            ""nullable"": true,
            ""readOnly"": true,
            ""x-beamable-semantic-type"": ""ContentId""
          },
          ""maxWaitDurationSecs"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          },
          ""team"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""watchOnlineStatus"": {
            ""type"": ""boolean""
          }
        },
        ""additionalProperties"": false
      },
      ""TicketReservationResponse"": {
        ""type"": ""object"",
        ""properties"": {
          ""tickets"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/Ticket""
            },
            ""nullable"": true,
            ""readOnly"": true
          }
        },
        ""additionalProperties"": false
      },
      ""UpdateLobby"": {
        ""type"": ""object"",
        ""properties"": {
          ""name"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""description"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""restriction"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""matchType"": {
            ""type"": ""string"",
            ""nullable"": true,
            ""x-beamable-semantic-type"": ""ContentId""
          },
          ""maxPlayers"": {
            ""type"": ""integer"",
            ""format"": ""int32"",
            ""nullable"": true
          },
          ""newHost"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      },
      ""UpdateParty"": {
        ""type"": ""object"",
        ""properties"": {
          ""restriction"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""maxSize"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          }
        },
        ""additionalProperties"": false
      }
    },
    ""securitySchemes"": {
      ""jwt"": {
        ""type"": ""http"",
        ""description"": ""Bearer authentication with a JWT in the Authorization header."",
        ""scheme"": ""bearer"",
        ""bearerFormat"": ""JWT""
      },
      ""user"": {
        ""type"": ""http"",
        ""description"": ""Bearer authentication with a player access token in the Authorization header."",
        ""scheme"": ""bearer"",
        ""bearerFormat"": ""UUID""
      },
      ""scope"": {
        ""type"": ""apiKey"",
        ""description"": ""Customer and project scope. This should contain the '<customer-id>.<project-id>'."",
        ""name"": ""X-BEAM-SCOPE"",
        ""in"": ""header""
      }
    }
  },
  ""security"": [
    {
      ""jwt"": [ ]
    },
    {
      ""user"": [ ]
    }
  ],
  ""externalDocs"": {
    ""description"": ""Beamable Documentation"",
    ""url"": ""https://help.beamable.com/CLI-Latest""
  }
}";

	#endregion

	#region content basic

	public const string ContentBasicApi = @"
{
    ""info"": {
        ""title"": ""content basic"",
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
        ""/basic/content/manifests/unarchive"": {
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
                                ""$ref"": ""#/components/schemas/ArchiveOrUnarchiveManifestsRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""developer"": []
                    }
                ]
            }
        },
        ""/basic/content/manifest/pull"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/Manifest""
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
                                ""$ref"": ""#/components/schemas/PullManifestRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""tester"": []
                    }
                ]
            }
        },
        ""/basic/content/manifest/history"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/GetManifestHistoryResponse""
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
                            ""type"": ""string"",
                            ""x-beamable-semantic-type"": ""ContentManifestId""
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
                    }
                ],
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""tester"": []
                    }
                ]
            }
        },
        ""/basic/content/binary"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/SaveBinaryResponse""
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
                                ""$ref"": ""#/components/schemas/SaveBinaryRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""developer"": []
                    }
                ]
            }
        },
        ""/basic/content/manifests/pull"": {
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
                                ""$ref"": ""#/components/schemas/PullAllManifestsRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""tester"": []
                    }
                ]
            }
        },
        ""/basic/content/content"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/ContentOrText""
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
                        ""name"": ""contentId"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string"",
                            ""x-beamable-semantic-type"": ""ContentId""
                        },
                        ""required"": true
                    },
                    {
                        ""name"": ""version"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    }
                ],
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""tester"": []
                    }
                ]
            }
        },
        ""/basic/content/localizations"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/GetLocalizationsResponse""
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
                        ""user"": [],
                        ""tester"": []
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
                ""requestBody"": {
                    ""content"": {
                        ""application/json"": {
                            ""schema"": {
                                ""$ref"": ""#/components/schemas/PutLocalizationsRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""developer"": []
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
                                ""$ref"": ""#/components/schemas/DeleteLocalizationRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""developer"": []
                    }
                ]
            }
        },
        ""/basic/content/text"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/SaveTextResponse""
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
                                ""$ref"": ""#/components/schemas/SaveTextRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""developer"": []
                    }
                ]
            }
        },
        ""/basic/content/manifest/exact"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/Manifest""
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
                        ""name"": ""uid"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""string""
                        },
                        ""required"": true
                    }
                ],
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""tester"": []
                    }
                ]
            }
        },
        ""/basic/content/manifest"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/Manifest""
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
                            ""type"": ""string"",
                            ""x-beamable-semantic-type"": ""ContentManifestId""
                        },
                        ""description"": ""ID of the content manifest"",
                        ""required"": false
                    }
                ],
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""tester"": []
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
                                    ""$ref"": ""#/components/schemas/Manifest""
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
                                ""$ref"": ""#/components/schemas/SaveManifestRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""developer"": []
                    }
                ]
            }
        },
        ""/basic/content/manifests/archive"": {
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
                                ""$ref"": ""#/components/schemas/ArchiveOrUnarchiveManifestsRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""developer"": []
                    }
                ]
            }
        },
        ""/basic/content/"": {
            ""post"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/SaveContentResponse""
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
                                ""$ref"": ""#/components/schemas/SaveContentRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""developer"": []
                    }
                ]
            }
        },
        ""/basic/content/manifest/public"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""text/csv"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/ClientManifestCsvResponse""
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
                            ""type"": ""string"",
                            ""x-beamable-semantic-type"": ""ContentManifestId""
                        },
                        ""description"": ""ID of the content manifest"",
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
        ""/basic/content/manifest/repeat"": {
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
                                ""$ref"": ""#/components/schemas/RepeatManifestRequest""
                            }
                        }
                    }
                },
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""developer"": []
                    }
                ]
            }
        },
        ""/basic/content/manifest/private"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""text/csv"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/ClientManifestCsvResponse""
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
                            ""type"": ""string"",
                            ""x-beamable-semantic-type"": ""ContentManifestId""
                        },
                        ""description"": ""ID of the content manifest"",
                        ""required"": false
                    }
                ],
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""tester"": []
                    }
                ]
            }
        },
        ""/basic/content/manifest/checksums"": {
            ""get"": {
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
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""tester"": []
                    }
                ]
            }
        },
        ""/basic/content/manifest/checksum"": {
            ""get"": {
                ""responses"": {
                    ""200"": {
                        ""description"": """",
                        ""content"": {
                            ""application/json"": {
                                ""schema"": {
                                    ""$ref"": ""#/components/schemas/ManifestChecksum""
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
                            ""type"": ""string"",
                            ""x-beamable-semantic-type"": ""ContentManifestId""
                        },
                        ""description"": ""ID of the content manifest"",
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
        ""/basic/content/manifests"": {
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
                ""security"": [
                    {
                        ""scope"": [],
                        ""user"": [],
                        ""tester"": []
                    }
                ]
            }
        }
    },
    ""components"": {
        ""schemas"": {
            ""ReferenceSuperset"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""tags"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""uri"": {
                        ""type"": ""string""
                    },
                    ""version"": {
                        ""type"": ""string""
                    },
                    ""id"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentId""
                    },
                    ""checksum"": {
                        ""type"": ""string""
                    },
                    ""type"": {
                        ""type"": ""string""
                    },
                    ""visibility"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""type"",
                    ""id"",
                    ""version"",
                    ""uri""
                ]
            },
            ""BinaryDefinition"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentId""
                    },
                    ""checksum"": {
                        ""type"": ""string""
                    },
                    ""uploadContentType"": {
                        ""type"": ""string""
                    },
                    ""tags"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""required"": [
                    ""id"",
                    ""checksum"",
                    ""uploadContentType""
                ]
            },
            ""PullManifestRequest"": {
                ""properties"": {
                    ""sourceRealmPid"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""Pid""
                    },
                    ""id"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentManifestId""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Pull Manifest Request"",
                ""type"": ""object"",
                ""required"": [
                    ""sourceRealmPid""
                ]
            },
            ""GetManifestsResponse"": {
                ""properties"": {
                    ""manifests"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/Manifest""
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
            ""SaveBinaryRequest"": {
                ""properties"": {
                    ""binary"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/BinaryDefinition""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Save Binary Request"",
                ""type"": ""object"",
                ""required"": [
                    ""binary""
                ]
            },
            ""TextReference"": {
                ""properties"": {
                    ""tags"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""uri"": {
                        ""type"": ""string""
                    },
                    ""version"": {
                        ""type"": ""string""
                    },
                    ""id"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentId""
                    },
                    ""checksum"": {
                        ""type"": ""string""
                    },
                    ""lastChanged"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""type"": {
                        ""type"": ""string"",
                        ""enum"": [
                            ""text""
                        ],
                        ""default"": ""text""
                    },
                    ""visibility"": {
                        ""type"": ""string""
                    },
                    ""created"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""text"",
                ""type"": ""object"",
                ""required"": [
                    ""type"",
                    ""id"",
                    ""version"",
                    ""uri"",
                    ""tags"",
                    ""visibility""
                ]
            },
            ""SaveBinaryResponse"": {
                ""properties"": {
                    ""binary"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/BinaryReference""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Save Binary Response"",
                ""type"": ""object"",
                ""required"": [
                    ""binary""
                ]
            },
            ""SaveTextRequest"": {
                ""properties"": {
                    ""text"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/TextDefinition""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Save Text Request"",
                ""type"": ""object"",
                ""required"": [
                    ""text""
                ]
            },
            ""TextDefinition"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentId""
                    },
                    ""checksum"": {
                        ""type"": ""string""
                    },
                    ""properties"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""string""
                        }
                    },
                    ""tags"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""required"": [
                    ""id"",
                    ""checksum"",
                    ""properties""
                ]
            },
            ""PutLocalizationsRequest"": {
                ""properties"": {
                    ""localizations"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""array"",
                            ""items"": {
                                ""$ref"": ""#/components/schemas/LocalizedValue""
                            }
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Put Localizations Request"",
                ""type"": ""object"",
                ""required"": [
                    ""localizations""
                ]
            },
            ""ContentOrText"": {
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentId""
                    },
                    ""version"": {
                        ""type"": ""string""
                    },
                    ""properties"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Content Or Text"",
                ""type"": ""object"",
                ""required"": [
                    ""id"",
                    ""version"",
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
            ""ArchiveOrUnarchiveManifestsRequest"": {
                ""properties"": {
                    ""manifestIds"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""x-beamable-semantic-type"": ""ContentManifestId"",
                            ""type"": ""string""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Archive Or Unarchive Manifests Request"",
                ""type"": ""object"",
                ""required"": [
                    ""manifestIds""
                ]
            },
            ""ContentMeta"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""data"": {
                        ""type"": ""string""
                    },
                    ""text"": {
                        ""type"": ""string""
                    },
                    ""visibility"": {
                        ""$ref"": ""#/components/schemas/ContentVisibility""
                    }
                },
                ""required"": [
                    ""visibility""
                ]
            },
            ""GetExactManifestRequest"": {
                ""properties"": {
                    ""uid"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Exact Manifest Request"",
                ""type"": ""object"",
                ""required"": [
                    ""uid""
                ]
            },
            ""BinaryReference"": {
                ""properties"": {
                    ""uploadMethod"": {
                        ""type"": ""string""
                    },
                    ""tags"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""uri"": {
                        ""type"": ""string""
                    },
                    ""version"": {
                        ""type"": ""string""
                    },
                    ""id"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentId""
                    },
                    ""checksum"": {
                        ""type"": ""string""
                    },
                    ""lastChanged"": {
                        ""type"": ""string""
                    },
                    ""uploadUri"": {
                        ""type"": ""string""
                    },
                    ""type"": {
                        ""type"": ""string"",
                        ""enum"": [
                            ""binary""
                        ],
                        ""default"": ""binary""
                    },
                    ""visibility"": {
                        ""type"": ""string""
                    },
                    ""created"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""binary"",
                ""type"": ""object"",
                ""required"": [
                    ""type"",
                    ""id"",
                    ""tags"",
                    ""version"",
                    ""uri"",
                    ""uploadUri"",
                    ""uploadMethod"",
                    ""visibility""
                ]
            },
            ""GetManifestRequest"": {
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""description"": ""ID of the content manifest"",
                        ""x-beamable-semantic-type"": ""ContentManifestId""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Manifest Request"",
                ""type"": ""object""
            },
            ""PullAllManifestsRequest"": {
                ""properties"": {
                    ""sourceRealmPid"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""Pid""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Pull All Manifests Request"",
                ""type"": ""object"",
                ""required"": [
                    ""sourceRealmPid""
                ]
            },
            ""GetManifestHistoryRequest"": {
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentManifestId""
                    },
                    ""limit"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Manifest History Request"",
                ""type"": ""object""
            },
            ""ContentDefinition"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""prefix"": {
                        ""type"": ""string""
                    },
                    ""tags"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""id"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentId""
                    },
                    ""checksum"": {
                        ""type"": ""string""
                    },
                    ""properties"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""$ref"": ""#/components/schemas/ContentMeta""
                        }
                    },
                    ""variants"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""object"",
                            ""additionalProperties"": {
                                ""$ref"": ""#/components/schemas/ContentMeta""
                            }
                        }
                    }
                },
                ""required"": [
                    ""id"",
                    ""checksum"",
                    ""properties"",
                    ""prefix""
                ]
            },
            ""ManifestChecksum"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentManifestId""
                    },
                    ""checksum"": {
                        ""type"": ""string""
                    },
                    ""createdAt"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""archived"": {
                        ""type"": ""boolean""
                    }
                },
                ""required"": [
                    ""id"",
                    ""checksum"",
                    ""createdAt""
                ]
            },
            ""SaveContentRequest"": {
                ""properties"": {
                    ""content"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ContentDefinition""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Save Content Request"",
                ""type"": ""object"",
                ""required"": [
                    ""content""
                ]
            },
            ""SaveManifestRequest"": {
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentManifestId""
                    },
                    ""references"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ReferenceSuperset""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Save Manifest Request"",
                ""type"": ""object"",
                ""required"": [
                    ""id"",
                    ""references""
                ]
            },
            ""RepeatManifestRequest"": {
                ""properties"": {
                    ""uid"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Repeat Manifest Request"",
                ""type"": ""object"",
                ""required"": [
                    ""uid""
                ]
            },
            ""ContentVisibility"": {
                ""type"": ""string"",
                ""enum"": [
                    ""public"",
                    ""private""
                ]
            },
            ""Manifest"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""archived"": {
                        ""type"": ""boolean""
                    },
                    ""references"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""oneOf"": [
                                {
                                    ""$ref"": ""#/components/schemas/ContentReference""
                                },
                                {
                                    ""$ref"": ""#/components/schemas/TextReference""
                                },
                                {
                                    ""$ref"": ""#/components/schemas/BinaryReference""
                                }
                            ]
                        }
                    },
                    ""id"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentManifestId""
                    },
                    ""checksum"": {
                        ""type"": ""string""
                    },
                    ""created"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""required"": [
                    ""id"",
                    ""references"",
                    ""checksum"",
                    ""created""
                ]
            },
            ""LocalizationQuery"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""string""
                    },
                    ""languages"": {
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
            ""GetLocalizationsResponse"": {
                ""properties"": {
                    ""localizations"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""array"",
                            ""items"": {
                                ""$ref"": ""#/components/schemas/LocalizedValue""
                            }
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Localizations Response"",
                ""type"": ""object"",
                ""required"": [
                    ""localizations""
                ]
            },
            ""GetContentRequest"": {
                ""properties"": {
                    ""contentId"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentId""
                    },
                    ""version"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Content Request"",
                ""type"": ""object"",
                ""required"": [
                    ""contentId"",
                    ""version""
                ]
            },
            ""ClientManifestCsvResponse"": {
                ""properties"": {
                    ""items"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ClientContentInfo""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Client Manifest Csv Response"",
                ""type"": ""object"",
                ""required"": [
                    ""items""
                ]
            },
            ""LocalizedValue"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""language"": {
                        ""type"": ""string""
                    },
                    ""value"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""language"",
                    ""value""
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
            ""SaveTextResponse"": {
                ""properties"": {
                    ""text"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/TextReference""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Save Text Response"",
                ""type"": ""object"",
                ""required"": [
                    ""text""
                ]
            },
            ""ManifestSummary"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""uid"": {
                        ""type"": ""string""
                    },
                    ""manifest"": {
                        ""$ref"": ""#/components/schemas/ManifestChecksum""
                    }
                },
                ""required"": [
                    ""uid"",
                    ""manifest""
                ]
            },
            ""DeleteLocalizationRequest"": {
                ""properties"": {
                    ""localizations"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/LocalizationQuery""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Delete Localization Request"",
                ""type"": ""object"",
                ""required"": [
                    ""localizations""
                ]
            },
            ""ClientContentInfo"": {
                ""x-beamable-primary-key"": ""contentId"",
                ""properties"": {
                    ""tags"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""uri"": {
                        ""type"": ""string""
                    },
                    ""version"": {
                        ""type"": ""string""
                    },
                    ""contentId"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentId""
                    },
                    ""type"": {
                        ""$ref"": ""#/components/schemas/ContentType""
                    }
                },
                ""x-beamable-csv-order"": ""type,contentId,version,uri,tags"",
                ""additionalProperties"": false,
                ""type"": ""object"",
                ""required"": [
                    ""contentId"",
                    ""version"",
                    ""uri"",
                    ""tags"",
                    ""type""
                ]
            },
            ""ContentType"": {
                ""type"": ""string"",
                ""enum"": [
                    ""content"",
                    ""text"",
                    ""binary""
                ]
            },
            ""GetManifestHistoryResponse"": {
                ""properties"": {
                    ""manifests"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ManifestSummary""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Get Manifest History Response"",
                ""type"": ""object"",
                ""required"": [
                    ""manifests""
                ]
            },
            ""SaveContentResponse"": {
                ""properties"": {
                    ""content"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""$ref"": ""#/components/schemas/ContentReference""
                        }
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Save Content Response"",
                ""type"": ""object"",
                ""required"": [
                    ""content""
                ]
            },
            ""ContentReference"": {
                ""properties"": {
                    ""tag"": {
                        ""type"": ""string""
                    },
                    ""tags"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""uri"": {
                        ""type"": ""string""
                    },
                    ""version"": {
                        ""type"": ""string""
                    },
                    ""id"": {
                        ""type"": ""string"",
                        ""x-beamable-semantic-type"": ""ContentId""
                    },
                    ""checksum"": {
                        ""type"": ""string""
                    },
                    ""lastChanged"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""type"": {
                        ""type"": ""string"",
                        ""enum"": [
                            ""content""
                        ],
                        ""default"": ""content""
                    },
                    ""visibility"": {
                        ""$ref"": ""#/components/schemas/ContentVisibility""
                    },
                    ""created"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""content"",
                ""type"": ""object"",
                ""required"": [
                    ""type"",
                    ""id"",
                    ""tags"",
                    ""version"",
                    ""uri"",
                    ""visibility"",
                    ""tag""
                ]
            }
        },
        ""securitySchemes"": {
            ""admin"": {
                ""type"": ""custom"",
                ""description"": ""Requires privileged user with an admin scope.""
            },
            ""scope"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-SCOPE"",
                ""in"": ""header"",
                ""description"": ""Customer and project scope. This should contain the '<customer-id>.<project-id>'.""
            },
            ""userRequired"": {
                ""type"": ""apiKey"",
                ""name"": ""X-DE-GAMERTAG"",
                ""in"": ""header"",
                ""description"": ""Gamer Tag of the player.""
            },
            ""developer"": {
                ""type"": ""custom"",
                ""description"": ""Requires privileged user with an developer scope.""
            },
            ""tester"": {
                ""type"": ""custom"",
                ""description"": ""Requires privileged user with an tester scope.""
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
        ""url"": ""https://help.beamable.com/CLI-Latest""
    },
    ""openapi"": ""3.0.2""
}
";

	#endregion

	#region event-players object

	public const string EventPlayersObjectApi = @"
{
  ""info"": {
    ""title"": ""event-players object"",
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
    ""/object/event-players/{objectId}/"": {
      ""get"": {
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
        ""parameters"": [
          {
            ""name"": ""objectId"",
            ""in"": ""path"",
            ""schema"": {
              ""type"": ""string""
            },
            ""description"": ""Gamertag of the player.Underlying objectId type is integer in format int64."",
            ""required"": true,
            ""x-beamable-object-id"": {
              ""type"": ""integer"",
              ""format"": ""int64""
            }
          }
        ],
        ""security"": [
          {
            ""user"": []
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
          ""description"": ""Customer and project scope. This should be in the form of '\u003Ccustomer-id\u003E.\u003Cproject-id\u003E'."",
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
    ""/object/event-players/{objectId}/claim"": {
      ""post"": {
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
        ""parameters"": [
          {
            ""name"": ""objectId"",
            ""in"": ""path"",
            ""schema"": {
              ""type"": ""string""
            },
            ""description"": ""Gamertag of the player.Underlying objectId type is integer in format int64."",
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
                ""$ref"": ""#/components/schemas/EventClaimRequest""
              }
            }
          }
        },
        ""security"": [
          {
            ""user"": []
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
          ""description"": ""Customer and project scope. This should be in the form of '\u003Ccustomer-id\u003E.\u003Cproject-id\u003E'."",
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
    ""/object/event-players/{objectId}/score"": {
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
            ""description"": ""Gamertag of the player.Underlying objectId type is integer in format int64."",
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
                ""$ref"": ""#/components/schemas/EventScoreRequest""
              }
            }
          }
        },
        ""security"": [
          {

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
          ""description"": ""Customer and project scope. This should be in the form of '\u003Ccustomer-id\u003E.\u003Cproject-id\u003E'."",
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
      ""EventInventoryRewardItem"": {
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {
          ""id"": {
            ""type"": ""string""
          },
          ""properties"": {
            ""type"": ""object"",
            ""additionalProperties"": {
              ""type"": ""string""
            }
          }
        },
        ""required"": [
          ""id""
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
      ""EventClaimResponse"": {
        ""properties"": {
          ""view"": {
            ""$ref"": ""#/components/schemas/EventPlayerStateView""
          },
          ""gameRspJson"": {
            ""type"": ""string""
          }
        },
        ""additionalProperties"": false,
        ""title"": ""Event Claim Response"",
        ""type"": ""object"",
        ""required"": [
          ""view"",
          ""gameRspJson""
        ]
      },
      ""EventPlayerView"": {
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
        ""additionalProperties"": false,
        ""title"": ""Event Player View"",
        ""type"": ""object"",
        ""required"": [
          ""running"",
          ""done""
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
      ""EventRewardState"": {
        ""type"": ""object"",
        ""additionalProperties"": false,
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
            ""type"": ""object"",
            ""additionalProperties"": {
              ""type"": ""string""
            }
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
            ""type"": ""number"",
            ""format"": ""double""
          },
          ""max"": {
            ""type"": ""number"",
            ""format"": ""double""
          },
          ""earned"": {
            ""type"": ""boolean""
          },
          ""claimed"": {
            ""type"": ""boolean""
          },
          ""pendingEntitlementRewards"": {
            ""type"": ""object"",
            ""additionalProperties"": {
              ""type"": ""string""
            }
          },
          ""obtain"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/EventRewardObtain""
            }
          }
        },
        ""required"": [
          ""min"",
          ""earned"",
          ""claimed"",
          ""pendingInventoryRewards""
        ]
      },
      ""EventScoreRequest"": {
        ""properties"": {
          ""eventId"": {
            ""type"": ""string""
          },
          ""score"": {
            ""type"": ""number"",
            ""format"": ""double""
          },
          ""increment"": {
            ""type"": ""boolean""
          },
          ""stats"": {
            ""type"": ""object"",
            ""additionalProperties"": {
              ""type"": ""string""
            }
          }
        },
        ""additionalProperties"": false,
        ""title"": ""Event Score Request"",
        ""type"": ""object"",
        ""required"": [
          ""eventId"",
          ""score""
        ]
      },
      ""EventRewardObtain"": {
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {
          ""symbol"": {
            ""type"": ""string""
          },
          ""count"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          }
        },
        ""required"": [
          ""symbol"",
          ""count""
        ]
      },
      ""EventClaimRequest"": {
        ""properties"": {
          ""eventId"": {
            ""type"": ""string""
          }
        },
        ""additionalProperties"": false,
        ""title"": ""Event Claim Request"",
        ""type"": ""object"",
        ""required"": [
          ""eventId""
        ]
      },
      ""EventInventoryRewardCurrency"": {
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {
          ""id"": {
            ""type"": ""string""
          },
          ""amount"": {
            ""type"": ""integer"",
            ""format"": ""int64""
          }
        },
        ""required"": [
          ""id"",
          ""amount""
        ]
      },
      ""EventInventoryPendingRewards"": {
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {
          ""currencies"": {
            ""type"": ""object"",
            ""additionalProperties"": {
              ""type"": ""string""
            }
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
        ""required"": [
          ""empty""
        ]
      },
      ""EventPlayerStateView"": {
        ""type"": ""object"",
        ""additionalProperties"": false,
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
            ""type"": ""integer"",
            ""format"": ""int64""
          },
          ""score"": {
            ""type"": ""number"",
            ""format"": ""double""
          },
          ""currentPhase"": {
            ""$ref"": ""#/components/schemas/EventPlayerPhaseView""
          },
          ""secondsRemaining"": {
            ""type"": ""integer"",
            ""format"": ""int64""
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
        ""required"": [
          ""id"",
          ""name"",
          ""leaderboardId"",
          ""score"",
          ""rank"",
          ""scoreRewards"",
          ""rankRewards"",
          ""running"",
          ""secondsRemaining"",
          ""allPhases""
        ]
      },
      ""EventPlayerPhaseView"": {
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {
          ""name"": {
            ""type"": ""string""
          },
          ""durationSeconds"": {
            ""type"": ""integer"",
            ""format"": ""int64""
          },
          ""rules"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/EventRule""
            }
          }
        },
        ""required"": [
          ""name"",
          ""durationSeconds"",
          ""rules""
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
      ""EventRule"": {
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {
          ""rule"": {
            ""type"": ""string""
          },
          ""value"": {
            ""type"": ""string""
          }
        },
        ""required"": [
          ""rule"",
          ""value""
        ]
      },
      ""EventPlayerGroupState"": {
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {
          ""groupScore"": {
            ""type"": ""number"",
            ""format"": ""double""
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
            ""type"": ""integer"",
            ""format"": ""int64""
          },
          ""scoreRewards"": {
            ""type"": ""array"",
            ""items"": {
              ""$ref"": ""#/components/schemas/EventRewardState""
            }
          }
        },
        ""required"": [
          ""groupScore"",
          ""groupRank"",
          ""scoreRewards"",
          ""rankRewards""
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
        ""bearerFormat"": ""Bearer \u003CAccess Token\u003E""
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
                        ""required"": true
                    },
                    {
                        ""name"": ""page"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""integer"",
                            ""format"": ""int32""
                        },
                        ""required"": true
                    },
                    {
                        ""name"": ""pagesize"",
                        ""in"": ""query"",
                        ""schema"": {
                            ""type"": ""integer"",
                            ""format"": ""int32""
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
                        ""required"": true
                    },
                    {
                        ""name"": ""token"",
                        ""in"": ""query"",
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
                ""type"": ""object"",
                ""required"": [
                    ""code"",
                    ""newPassword""
                ]
            },
            ""DeviceIdAvailableRequest"": {
                ""properties"": {
                    ""deviceId"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Device Id Available Request"",
                ""type"": ""object"",
                ""required"": [
                    ""deviceId""
                ]
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
                },
                ""required"": [
                    ""contentId"",
                    ""properties""
                ]
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
                ""type"": ""object"",
                ""required"": [
                    ""account"",
                    ""stats"",
                    ""paymentAudits""
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
                        ""type"": ""integer"",
                        ""format"": ""int64""
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
                    ""id"",
                    ""scopes"",
                    ""thirdPartyAppAssociations""
                ]
            },
            ""SearchAccountsRequest"": {
                ""properties"": {
                    ""query"": {
                        ""type"": ""string""
                    },
                    ""page"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    },
                    ""pagesize"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Search Accounts Request"",
                ""type"": ""object"",
                ""required"": [
                    ""query"",
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
                ""type"": ""object"",
                ""required"": [
                    ""email""
                ]
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
                        ""type"": ""integer"",
                        ""format"": ""int64""
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
                        ""type"": ""integer"",
                        ""format"": ""int64""
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
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""created"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""required"": [
                    ""gt"",
                    ""txid"",
                    ""providername"",
                    ""details"",
                    ""providerid"",
                    ""txstate"",
                    ""history"",
                    ""entitlements""
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
                        ""type"": ""integer"",
                        ""format"": ""int64""
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
                    ""id"",
                    ""scopes"",
                    ""thirdPartyAppAssociations"",
                    ""deviceIds""
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
                },
                ""required"": [
                    ""change""
                ]
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
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    },
                    ""claimWindow"": {
                        ""$ref"": ""#/components/schemas/EntitlementClaimWindow""
                    },
                    ""params"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""string""
                        }
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
                ""required"": [
                    ""symbol"",
                    ""action""
                ]
            },
            ""StatsResponse"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""id"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""stats"": {
                        ""type"": ""object"",
                        ""additionalProperties"": {
                            ""type"": ""string""
                        }
                    }
                },
                ""required"": [
                    ""id"",
                    ""stats""
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
                },
                ""required"": [
                    ""projectId"",
                    ""role""
                ]
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
                ""type"": ""object"",
                ""required"": [
                    ""email"",
                    ""password""
                ]
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
                ""type"": ""object"",
                ""required"": [
                    ""code"",
                    ""password""
                ]
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
                ""type"": ""object"",
                ""required"": [
                    ""accounts""
                ]
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
                        ""type"": ""integer"",
                        ""format"": ""int32""
                    },
                    ""sku"": {
                        ""type"": ""string""
                    },
                    ""price"": {
                        ""type"": ""integer"",
                        ""format"": ""int32""
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
                    ""quantity"",
                    ""name"",
                    ""reference"",
                    ""gameplace"",
                    ""sku"",
                    ""providerProductId""
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
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""originalAmount"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    }
                },
                ""required"": [
                    ""symbol"",
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
                ""type"": ""object"",
                ""required"": [
                    ""email""
                ]
            },
            ""EntitlementClaimWindow"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""open"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
                    },
                    ""close"": {
                        ""type"": ""integer"",
                        ""format"": ""int64""
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
                ""type"": ""object"",
                ""required"": [
                    ""thirdParty"",
                    ""token""
                ]
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
                ""type"": ""object"",
                ""required"": [
                    ""accounts""
                ]
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
                },
                ""required"": [
                    ""audits""
                ]
            },
            ""AccountAvailableRequest"": {
                ""properties"": {
                    ""email"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Account Available Request"",
                ""type"": ""object"",
                ""required"": [
                    ""email""
                ]
            },
            ""FindAccountRequest"": {
                ""properties"": {
                    ""query"": {
                        ""type"": ""string""
                    }
                },
                ""additionalProperties"": false,
                ""title"": ""Find Account Request"",
                ""type"": ""object"",
                ""required"": [
                    ""query""
                ]
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
        ""url"": ""https://help.beamable.com/CLI-Latest""
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
        ""url"": ""https://help.beamable.com/CLI-Latest""
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
        ""url"": ""https://help.beamable.com/CLI-Latest""
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
        ""url"": ""https://help.beamable.com/CLI-Latest""
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
        ""url"": ""https://help.beamable.com/CLI-Latest""
    },
    ""openapi"": ""3.0.2""
}";

	#endregion

	#region content

	public const string ContentObjectApi = $@"{{
	""info"": {{
    ""title"": ""content basic"",
    ""version"": ""1.0"",
    ""contact"": {{
      ""name"": ""Beamable Support"",
      ""url"": ""https://api.beamable.com"",
      ""email"": ""support@beamable.com""
    }}
  }},
  ""servers"": [
    {{
      ""url"": ""https://api.beamable.com""
    }}
  ],
  ""paths"": {{
    ""/basic/content/manifests/unarchive"": {{
      ""post"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/EmptyResponse""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""requestBody"": {{
          ""content"": {{
            ""application/json"": {{
              ""schema"": {{
                ""$ref"": ""#/components/schemas/ArchiveOrUnarchiveManifestsRequest""
              }}
            }}
          }}
        }},
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/manifest/pull"": {{
      ""post"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/Manifest""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""requestBody"": {{
          ""content"": {{
            ""application/json"": {{
              ""schema"": {{
                ""$ref"": ""#/components/schemas/PullManifestRequest""
              }}
            }}
          }}
        }},
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/manifest/history"": {{
      ""get"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/GetManifestHistoryResponse""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""parameters"": [
          {{
            ""name"": ""id"",
            ""in"": ""query"",
            ""schema"": {{
              ""type"": ""string"",
              ""x-beamable-semantic-type"": ""ContentManifestId""
            }},
            ""required"": false
          }},
          {{
            ""name"": ""limit"",
            ""in"": ""query"",
            ""schema"": {{
              ""type"": ""integer"",
              ""format"": ""int32""
            }},
            ""required"": false
          }}
        ],
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/binary"": {{
      ""post"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/SaveBinaryResponse""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""requestBody"": {{
          ""content"": {{
            ""application/json"": {{
              ""schema"": {{
                ""$ref"": ""#/components/schemas/SaveBinaryRequest""
              }}
            }}
          }}
        }},
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/manifests/pull"": {{
      ""post"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/ManifestChecksums""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""requestBody"": {{
          ""content"": {{
            ""application/json"": {{
              ""schema"": {{
                ""$ref"": ""#/components/schemas/PullAllManifestsRequest""
              }}
            }}
          }}
        }},
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/content"": {{
      ""get"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/ContentOrText""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""parameters"": [
          {{
            ""name"": ""contentId"",
            ""in"": ""query"",
            ""schema"": {{
              ""type"": ""string"",
              ""x-beamable-semantic-type"": ""ContentId""
            }},
            ""required"": true
          }},
          {{
            ""name"": ""version"",
            ""in"": ""query"",
            ""schema"": {{
              ""type"": ""string""
            }},
            ""required"": true
          }}
        ],
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/localizations"": {{
      ""get"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/GetLocalizationsResponse""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }},
      ""put"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/CommonResponse""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""requestBody"": {{
          ""content"": {{
            ""application/json"": {{
              ""schema"": {{
                ""$ref"": ""#/components/schemas/PutLocalizationsRequest""
              }}
            }}
          }}
        }},
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }},
      ""delete"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/CommonResponse""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""requestBody"": {{
          ""content"": {{
            ""application/json"": {{
              ""schema"": {{
                ""$ref"": ""#/components/schemas/DeleteLocalizationRequest""
              }}
            }}
          }}
        }},
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/text"": {{
      ""post"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/SaveTextResponse""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""requestBody"": {{
          ""content"": {{
            ""application/json"": {{
              ""schema"": {{
                ""$ref"": ""#/components/schemas/SaveTextRequest""
              }}
            }}
          }}
        }},
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/manifest/exact"": {{
      ""get"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/Manifest""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""parameters"": [
          {{
            ""name"": ""uid"",
            ""in"": ""query"",
            ""schema"": {{
              ""type"": ""string""
            }},
            ""required"": true
          }}
        ],
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/manifest"": {{
      ""get"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/Manifest""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""parameters"": [
          {{
            ""name"": ""id"",
            ""in"": ""query"",
            ""schema"": {{
              ""type"": ""string"",
              ""x-beamable-semantic-type"": ""ContentManifestId""
            }},
            ""description"": ""ID of the content manifest"",
            ""required"": false
          }}
        ],
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }},
      ""post"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/Manifest""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""requestBody"": {{
          ""content"": {{
            ""application/json"": {{
              ""schema"": {{
                ""$ref"": ""#/components/schemas/SaveManifestRequest""
              }}
            }}
          }}
        }},
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/manifests/archive"": {{
      ""post"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/EmptyResponse""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""requestBody"": {{
          ""content"": {{
            ""application/json"": {{
              ""schema"": {{
                ""$ref"": ""#/components/schemas/ArchiveOrUnarchiveManifestsRequest""
              }}
            }}
          }}
        }},
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/"": {{
      ""post"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/SaveContentResponse""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""requestBody"": {{
          ""content"": {{
            ""application/json"": {{
              ""schema"": {{
                ""$ref"": ""#/components/schemas/SaveContentRequest""
              }}
            }}
          }}
        }},
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/manifest/public"": {{
      ""get"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""text/csv"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/ClientManifestCsvResponse""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""parameters"": [
          {{
            ""name"": ""id"",
            ""in"": ""query"",
            ""schema"": {{
              ""type"": ""string"",
              ""x-beamable-semantic-type"": ""ContentManifestId""
            }},
            ""description"": ""ID of the content manifest"",
            ""required"": false
          }}
        ],
        ""security"": [
          {{
            ""scope"": []
          }}
        ]
      }}
    }},
    ""/basic/content/manifest/repeat"": {{
      ""put"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/CommonResponse""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""requestBody"": {{
          ""content"": {{
            ""application/json"": {{
              ""schema"": {{
                ""$ref"": ""#/components/schemas/RepeatManifestRequest""
              }}
            }}
          }}
        }},
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/manifest/private"": {{
      ""get"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""text/csv"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/ClientManifestCsvResponse""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""parameters"": [
          {{
            ""name"": ""id"",
            ""in"": ""query"",
            ""schema"": {{
              ""type"": ""string"",
              ""x-beamable-semantic-type"": ""ContentManifestId""
            }},
            ""description"": ""ID of the content manifest"",
            ""required"": false
          }}
        ],
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/manifest/checksums"": {{
      ""get"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/ManifestChecksums""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }},
    ""/basic/content/manifest/checksum"": {{
      ""get"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/ManifestChecksum""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""parameters"": [
          {{
            ""name"": ""id"",
            ""in"": ""query"",
            ""schema"": {{
              ""type"": ""string"",
              ""x-beamable-semantic-type"": ""ContentManifestId""
            }},
            ""description"": ""ID of the content manifest"",
            ""required"": false
          }}
        ],
        ""security"": [
          {{
            ""scope"": []
          }}
        ]
      }}
    }},
    ""/basic/content/manifests"": {{
      ""get"": {{
        ""responses"": {{
          ""200"": {{
            ""description"": """",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  ""$ref"": ""#/components/schemas/GetManifestsResponse""
                }}
              }}
            }}
          }},
          ""400"": {{
            ""description"": ""Bad Request""
          }}
        }},
        ""security"": [
          {{
            ""scope"": [],
            ""user"": []
          }}
        ]
      }}
    }}
  }},
  ""components"": {{
    ""schemas"": {{
      ""ReferenceSuperset"": {{
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {{
          ""tags"": {{
            ""type"": ""array"",
            ""items"": {{
              ""type"": ""string""
            }}
          }},
          ""uri"": {{
            ""type"": ""string""
          }},
          ""version"": {{
            ""type"": ""string""
          }},
          ""id"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentId""
          }},
          ""checksum"": {{
            ""type"": ""string""
          }},
          ""type"": {{
            ""type"": ""string""
          }},
          ""visibility"": {{
            ""type"": ""string""
          }}
        }},
        ""required"": [
          ""type"",
          ""id"",
          ""version"",
          ""uri""
        ]
      }},
      ""BinaryDefinition"": {{
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {{
          ""id"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentId""
          }},
          ""checksum"": {{
            ""type"": ""string""
          }},
          ""uploadContentType"": {{
            ""type"": ""string""
          }},
          ""tags"": {{
            ""type"": ""array"",
            ""items"": {{
              ""type"": ""string""
            }}
          }}
        }},
        ""required"": [
          ""id"",
          ""checksum"",
          ""uploadContentType""
        ]
      }},
      ""PullManifestRequest"": {{
        ""properties"": {{
          ""sourceRealmPid"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""Pid""
          }},
          ""id"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentManifestId""
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Pull Manifest Request"",
        ""type"": ""object"",
        ""required"": [
          ""sourceRealmPid""
        ]
      }},
      ""GetManifestsResponse"": {{
        ""properties"": {{
          ""manifests"": {{
            ""type"": ""array"",
            ""items"": {{
              ""$ref"": ""#/components/schemas/Manifest""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Get Manifests Response"",
        ""type"": ""object"",
        ""required"": [
          ""manifests""
        ]
      }},
      ""SaveBinaryRequest"": {{
        ""properties"": {{
          ""binary"": {{
            ""type"": ""array"",
            ""items"": {{
              ""$ref"": ""#/components/schemas/BinaryDefinition""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Save Binary Request"",
        ""type"": ""object"",
        ""required"": [
          ""binary""
        ]
      }},
      ""TextReference"": {{
        ""properties"": {{
          ""contentPrefix"": {{
            ""type"": ""string""
          }},
          ""tags"": {{
            ""type"": ""array"",
            ""items"": {{
              ""type"": ""string""
            }}
          }},
          ""uri"": {{
            ""type"": ""string""
          }},
          ""version"": {{
            ""type"": ""string""
          }},
          ""id"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentId""
          }},
          ""checksum"": {{
            ""type"": ""string""
          }},
          ""lastChanged"": {{
            ""type"": ""integer"",
            ""format"": ""int64""
          }},
          ""type"": {{
            ""type"": ""string"",
            ""enum"": [
              ""text""
            ],
            ""default"": ""text""
          }},
          ""visibility"": {{
            ""type"": ""string""
          }},
          ""created"": {{
            ""type"": ""integer"",
            ""format"": ""int64""
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""text"",
        ""type"": ""object"",
        ""required"": [
          ""type"",
          ""id"",
          ""version"",
          ""uri"",
          ""tags"",
          ""visibility"",
          ""contentPrefix""
        ]
      }},
      ""SaveBinaryResponse"": {{
        ""properties"": {{
          ""binary"": {{
            ""type"": ""array"",
            ""items"": {{
              ""$ref"": ""#/components/schemas/BinaryReference""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Save Binary Response"",
        ""type"": ""object"",
        ""required"": [
          ""binary""
        ]
      }},
      ""SaveTextRequest"": {{
        ""properties"": {{
          ""text"": {{
            ""type"": ""array"",
            ""items"": {{
              ""$ref"": ""#/components/schemas/TextDefinition""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Save Text Request"",
        ""type"": ""object"",
        ""required"": [
          ""text""
        ]
      }},
      ""TextDefinition"": {{
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {{
          ""id"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentId""
          }},
          ""checksum"": {{
            ""type"": ""string""
          }},
          ""properties"": {{
            ""type"": ""object"",
            ""additionalProperties"": {{
              ""type"": ""string""
            }}
          }},
          ""tags"": {{
            ""type"": ""array"",
            ""items"": {{
              ""type"": ""string""
            }}
          }}
        }},
        ""required"": [
          ""id"",
          ""checksum"",
          ""properties""
        ]
      }},
      ""PutLocalizationsRequest"": {{
        ""properties"": {{
          ""localizations"": {{
            ""type"": ""object"",
            ""additionalProperties"": {{
              ""type"": ""array"",
              ""items"": {{
                ""$ref"": ""#/components/schemas/LocalizedValue""
              }}
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Put Localizations Request"",
        ""type"": ""object"",
        ""required"": [
          ""localizations""
        ]
      }},
      ""ContentOrText"": {{
        ""properties"": {{
          ""id"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentId""
          }},
          ""version"": {{
            ""type"": ""string""
          }},
          ""properties"": {{
            ""type"": ""object"",
            ""additionalProperties"": {{
              ""type"": ""string""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Content Or Text"",
        ""type"": ""object"",
        ""required"": [
          ""id"",
          ""version"",
          ""properties""
        ]
      }},
      ""CommonResponse"": {{
        ""properties"": {{
          ""result"": {{
            ""type"": ""string""
          }},
          ""data"": {{
            ""type"": ""object"",
            ""additionalProperties"": {{
              ""type"": ""string""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Common Response"",
        ""type"": ""object"",
        ""required"": [
          ""result"",
          ""data""
        ]
      }},
      ""ArchiveOrUnarchiveManifestsRequest"": {{
        ""properties"": {{
          ""manifestIds"": {{
            ""type"": ""array"",
            ""items"": {{
              ""x-beamable-semantic-type"": ""ContentManifestId"",
              ""type"": ""string""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Archive Or Unarchive Manifests Request"",
        ""type"": ""object"",
        ""required"": [
          ""manifestIds""
        ]
      }},
      ""ContentMeta"": {{
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {{
          ""data"": {{
            ""type"": ""string""
          }},
          ""text"": {{
            ""type"": ""string""
          }},
          ""visibility"": {{
            ""$ref"": ""#/components/schemas/ContentVisibility""
          }}
        }},
        ""required"": [
          ""visibility""
        ]
      }},
      ""GetExactManifestRequest"": {{
        ""properties"": {{
          ""uid"": {{
            ""type"": ""string""
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Get Exact Manifest Request"",
        ""type"": ""object"",
        ""required"": [
          ""uid""
        ]
      }},
      ""BinaryReference"": {{
        ""properties"": {{
          ""uploadMethod"": {{
            ""type"": ""string""
          }},
          ""contentPrefix"": {{
            ""type"": ""string""
          }},
          ""tags"": {{
            ""type"": ""array"",
            ""items"": {{
              ""type"": ""string""
            }}
          }},
          ""uri"": {{
            ""type"": ""string""
          }},
          ""version"": {{
            ""type"": ""string""
          }},
          ""id"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentId""
          }},
          ""checksum"": {{
            ""type"": ""string""
          }},
          ""lastChanged"": {{
            ""type"": ""string""
          }},
          ""uploadUri"": {{
            ""type"": ""string""
          }},
          ""type"": {{
            ""type"": ""string"",
            ""enum"": [
              ""binary""
            ],
            ""default"": ""binary""
          }},
          ""visibility"": {{
            ""type"": ""string""
          }},
          ""created"": {{
            ""type"": ""string""
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""binary"",
        ""type"": ""object"",
        ""required"": [
          ""type"",
          ""id"",
          ""tags"",
          ""version"",
          ""uri"",
          ""uploadUri"",
          ""uploadMethod"",
          ""visibility"",
          ""contentPrefix""
        ]
      }},
      ""GetManifestRequest"": {{
        ""properties"": {{
          ""id"": {{
            ""type"": ""string"",
            ""description"": ""ID of the content manifest"",
            ""x-beamable-semantic-type"": ""ContentManifestId""
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Get Manifest Request"",
        ""type"": ""object""
      }},
      ""PullAllManifestsRequest"": {{
        ""properties"": {{
          ""sourceRealmPid"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""Pid""
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Pull All Manifests Request"",
        ""type"": ""object"",
        ""required"": [
          ""sourceRealmPid""
        ]
      }},
      ""GetManifestHistoryRequest"": {{
        ""properties"": {{
          ""id"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentManifestId""
          }},
          ""limit"": {{
            ""type"": ""integer"",
            ""format"": ""int32""
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Get Manifest History Request"",
        ""type"": ""object""
      }},
      ""ContentDefinition"": {{
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {{
          ""prefix"": {{
            ""type"": ""string""
          }},
          ""tags"": {{
            ""type"": ""array"",
            ""items"": {{
              ""type"": ""string""
            }}
          }},
          ""id"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentId""
          }},
          ""checksum"": {{
            ""type"": ""string""
          }},
          ""properties"": {{
            ""type"": ""object"",
            ""additionalProperties"": {{
              ""$ref"": ""#/components/schemas/ContentMeta""
            }}
          }},
          ""variants"": {{
            ""type"": ""array"",
            ""items"": {{
              ""type"": ""object"",
              ""additionalProperties"": {{
                ""$ref"": ""#/components/schemas/ContentMeta""
              }}
            }}
          }}
        }},
        ""required"": [
          ""id"",
          ""checksum"",
          ""properties"",
          ""prefix""
        ]
      }},
      ""ManifestChecksum"": {{
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {{
          ""id"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentManifestId""
          }},
          ""checksum"": {{
            ""type"": ""string""
          }},
          ""createdAt"": {{
            ""type"": ""integer"",
            ""format"": ""int64""
          }},
          ""archived"": {{
            ""type"": ""boolean""
          }}
        }},
        ""required"": [
          ""id"",
          ""checksum"",
          ""createdAt""
        ]
      }},
      ""SaveContentRequest"": {{
        ""properties"": {{
          ""content"": {{
            ""type"": ""array"",
            ""items"": {{
              ""$ref"": ""#/components/schemas/ContentDefinition""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Save Content Request"",
        ""type"": ""object"",
        ""required"": [
          ""content""
        ]
      }},
      ""SaveManifestRequest"": {{
        ""properties"": {{
          ""id"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentManifestId""
          }},
          ""references"": {{
            ""type"": ""array"",
            ""items"": {{
              ""$ref"": ""#/components/schemas/ReferenceSuperset""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Save Manifest Request"",
        ""type"": ""object"",
        ""required"": [
          ""id"",
          ""references""
        ]
      }},
      ""RepeatManifestRequest"": {{
        ""properties"": {{
          ""uid"": {{
            ""type"": ""string""
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Repeat Manifest Request"",
        ""type"": ""object"",
        ""required"": [
          ""uid""
        ]
      }},
      ""ContentVisibility"": {{
        ""type"": ""string"",
        ""enum"": [
          ""public"",
          ""private""
        ]
      }},
      ""Manifest"": {{
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {{
          ""archived"": {{
            ""type"": ""boolean""
          }},
          ""references"": {{
            ""type"": ""array"",
            ""items"": {{
              ""oneOf"": [
                {{
                  ""$ref"": ""#/components/schemas/ContentReference""
                }},
                {{
                  ""$ref"": ""#/components/schemas/TextReference""
                }},
                {{
                  ""$ref"": ""#/components/schemas/BinaryReference""
                }}
              ]
            }}
          }},
          ""id"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentManifestId""
          }},
          ""checksum"": {{
            ""type"": ""string""
          }},
          ""created"": {{
            ""type"": ""integer"",
            ""format"": ""int64""
          }}
        }},
        ""required"": [
          ""id"",
          ""references"",
          ""checksum"",
          ""created""
        ]
      }},
      ""LocalizationQuery"": {{
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {{
          ""id"": {{
            ""type"": ""string""
          }},
          ""languages"": {{
            ""type"": ""array"",
            ""items"": {{
              ""type"": ""string""
            }}
          }}
        }},
        ""required"": [
          ""id""
        ]
      }},
      ""EmptyResponse"": {{
        ""properties"": {{
          ""result"": {{
            ""type"": ""string""
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Empty Response"",
        ""type"": ""object"",
        ""required"": [
          ""result""
        ]
      }},
      ""GetLocalizationsResponse"": {{
        ""properties"": {{
          ""localizations"": {{
            ""type"": ""object"",
            ""additionalProperties"": {{
              ""type"": ""array"",
              ""items"": {{
                ""$ref"": ""#/components/schemas/LocalizedValue""
              }}
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Get Localizations Response"",
        ""type"": ""object"",
        ""required"": [
          ""localizations""
        ]
      }},
      ""GetContentRequest"": {{
        ""properties"": {{
          ""contentId"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentId""
          }},
          ""version"": {{
            ""type"": ""string""
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Get Content Request"",
        ""type"": ""object"",
        ""required"": [
          ""contentId"",
          ""version""
        ]
      }},
      ""ClientManifestCsvResponse"": {{
        ""properties"": {{
          ""items"": {{
            ""type"": ""array"",
            ""items"": {{
              ""$ref"": ""#/components/schemas/ClientContentInfo""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Client Manifest Csv Response"",
        ""type"": ""object"",
        ""required"": [
          ""items""
        ]
      }},
      ""LocalizedValue"": {{
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {{
          ""language"": {{
            ""type"": ""string""
          }},
          ""value"": {{
            ""type"": ""string""
          }}
        }},
        ""required"": [
          ""language"",
          ""value""
        ]
      }},
      ""ManifestChecksums"": {{
        ""properties"": {{
          ""manifests"": {{
            ""type"": ""array"",
            ""items"": {{
              ""$ref"": ""#/components/schemas/ManifestChecksum""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Manifest Checksums"",
        ""type"": ""object"",
        ""required"": [
          ""manifests""
        ]
      }},
      ""SaveTextResponse"": {{
        ""properties"": {{
          ""text"": {{
            ""type"": ""array"",
            ""items"": {{
              ""$ref"": ""#/components/schemas/TextReference""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Save Text Response"",
        ""type"": ""object"",
        ""required"": [
          ""text""
        ]
      }},
      ""ManifestSummary"": {{
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {{
          ""uid"": {{
            ""type"": ""string""
          }},
          ""manifest"": {{
            ""$ref"": ""#/components/schemas/ManifestChecksum""
          }}
        }},
        ""required"": [
          ""uid"",
          ""manifest""
        ]
      }},
      ""DeleteLocalizationRequest"": {{
        ""properties"": {{
          ""localizations"": {{
            ""type"": ""array"",
            ""items"": {{
              ""$ref"": ""#/components/schemas/LocalizationQuery""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Delete Localization Request"",
        ""type"": ""object"",
        ""required"": [
          ""localizations""
        ]
      }},
      ""ClientContentInfo"": {{
        ""x-beamable-primary-key"": ""contentId"",
        ""properties"": {{
          ""tags"": {{
            ""type"": ""array"",
            ""items"": {{
              ""type"": ""string""
            }}
          }},
          ""uri"": {{
            ""type"": ""string""
          }},
          ""version"": {{
            ""type"": ""string""
          }},
          ""contentId"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentId""
          }},
          ""type"": {{
            ""$ref"": ""#/components/schemas/ContentType""
          }}
        }},
        ""x-beamable-csv-order"": ""type,contentId,version,uri,tags"",
        ""additionalProperties"": false,
        ""type"": ""object"",
        ""required"": [
          ""contentId"",
          ""version"",
          ""uri"",
          ""tags"",
          ""type""
        ]
      }},
      ""ContentType"": {{
        ""type"": ""string"",
        ""enum"": [
          ""content"",
          ""text"",
          ""binary""
        ]
      }},
      ""GetManifestHistoryResponse"": {{
        ""properties"": {{
          ""manifests"": {{
            ""type"": ""array"",
            ""items"": {{
              ""$ref"": ""#/components/schemas/ManifestSummary""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Get Manifest History Response"",
        ""type"": ""object"",
        ""required"": [
          ""manifests""
        ]
      }},
      ""SaveContentResponse"": {{
        ""properties"": {{
          ""content"": {{
            ""type"": ""array"",
            ""items"": {{
              ""$ref"": ""#/components/schemas/ContentReference""
            }}
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""Save Content Response"",
        ""type"": ""object"",
        ""required"": [
          ""content""
        ]
      }},
      ""ContentReference"": {{
        ""properties"": {{
          ""contentPrefix"": {{
            ""type"": ""string""
          }},
          ""tag"": {{
            ""type"": ""string""
          }},
          ""tags"": {{
            ""type"": ""array"",
            ""items"": {{
              ""type"": ""string""
            }}
          }},
          ""uri"": {{
            ""type"": ""string""
          }},
          ""version"": {{
            ""type"": ""string""
          }},
          ""id"": {{
            ""type"": ""string"",
            ""x-beamable-semantic-type"": ""ContentId""
          }},
          ""checksum"": {{
            ""type"": ""string""
          }},
          ""lastChanged"": {{
            ""type"": ""integer"",
            ""format"": ""int64""
          }},
          ""type"": {{
            ""type"": ""string"",
            ""enum"": [
              ""content""
            ],
            ""default"": ""content""
          }},
          ""visibility"": {{
            ""$ref"": ""#/components/schemas/ContentVisibility""
          }},
          ""created"": {{
            ""type"": ""integer"",
            ""format"": ""int64""
          }}
        }},
        ""additionalProperties"": false,
        ""title"": ""content"",
        ""type"": ""object"",
        ""required"": [
          ""type"",
          ""id"",
          ""tags"",
          ""version"",
          ""uri"",
          ""visibility"",
          ""tag"",
          ""contentPrefix""
        ]
      }}
    }},
    ""securitySchemes"": {{
      ""userRequired"": {{
        ""type"": ""apiKey"",
        ""name"": ""X-DE-GAMERTAG"",
        ""in"": ""header"",
        ""description"": ""Gamer Tag of the player.""
      }},
      ""scope"": {{
        ""type"": ""apiKey"",
        ""name"": ""X-DE-SCOPE"",
        ""in"": ""header"",
        ""description"": ""Customer and project scope. This should contain the '<customer-id>.<project-id>'.""
      }},
      ""api"": {{
        ""type"": ""apiKey"",
        ""name"": ""X-DE-SIGNATURE"",
        ""in"": ""header"",
        ""description"": ""Signed Request authentication using project secret key.""
      }},
      ""user"": {{
        ""type"": ""http"",
        ""description"": ""Bearer authentication with an player access token in the Authorization header."",
        ""scheme"": ""bearer"",
        ""bearerFormat"": ""Bearer <Access Token>""
      }}
    }}
  }},
  ""security"": [],
  ""externalDocs"": {{
    ""description"": ""Beamable Documentation"",
    ""url"": ""https://help.beamable.com/CLI-Latest""
  }},
  ""openapi"": ""3.0.2""
}}";

	#endregion
}
