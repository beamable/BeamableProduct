using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using RuntimeConfigData = Beamable.Config.ConfigData;
using ConfigDatabaseProvider = Beamable.Config.ConfigDatabaseProvider;
using Beamable.Editor.Util;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Accounts
{
	public partial class AccountWindow
	{
		private const string BEAM_PROD_ENDPOINT = "https://api.beamable.com";
		private const string BEAM_STAGE_ENDPOINT = "https://staging.api.beamable.com";
		private const string BEAM_DEV_ENDPOINT = "https://dev.api.beamable.com";
		
		public enum Env
		{
			Unset,
			Beamable, 
			Staging,
			Dev,
			Custom
		}

		public static Dictionary<Env, string> envMap = new Dictionary<Env, string>
		{
			[Env.Beamable] = "Beamable",
			[Env.Staging] = "Beamable Staging",
			[Env.Dev] = "Beamable Dev",
			[Env.Custom] = "Private Cloud",
		};

		public static Dictionary<string, Env> reverseEnvMap = envMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

		public static string[] envChoices = new string[]
		{
			envMap[Env.Beamable],
			#if BEAMABLE_DEVELOPER
			envMap[Env.Staging],
			envMap[Env.Dev],
			#endif
			envMap[Env.Custom],
		};

		public string GetHostString()
		{
			switch (env)
			{
				case Env.Beamable: return BEAM_PROD_ENDPOINT;
				case Env.Staging: return BEAM_STAGE_ENDPOINT;
				case Env.Dev: return BEAM_DEV_ENDPOINT;
				case Env.Custom: return host;
				default: throw new InvalidOperationException("unknown env");
			}
		}

		public string GetPortalUriString()
		{
			switch (env)
			{
				case Env.Dev: return Constants.BEAM_DEV_PORTAL_URI;
				case Env.Staging: return Constants.BEAM_STAGE_PORTAL_URI;
				default: return Constants.BEAM_PROD_PORTAL_URI;
			}
		}
		
		// public static Dictionary<string, Env> envChoiceMap = envChoices.Select(choice => )
		
		public string cidOrAlias; 
		public string email; 
		public string password;

		public Env env;
		public string host = "https://api.beamable.com";
		
		private Promise _loginPromise;

		// the project's previous realm context, captured before logging in so it can be restored afterwards
		private string _previousCid;
		private string _previousPid;
		private string _previousBuildPid;

		[NonSerialized]
		private RuntimeConfigData _configDefaults;
		private RuntimeConfigData ConfigDefaults
		{
			get
			{
				if (_configDefaults == null)
				{
					_configDefaults = ConfigDatabaseProvider.GetConfigData() ?? new RuntimeConfigData();
				}
				return _configDefaults;
			}
		}
		private GUIStyle _headerStyle;
		private GUIStyle _titleStyle;
		private GUIStyle _textboxStyle;
		private GUIStyle _textboxPlaceholderStyle;
		private GUIStyle _placeholderStyle;

		public void Draw_SignIn()
		{
			EditorGUILayout.BeginVertical(new GUIStyle
			{
				padding = new RectOffset(12, 12, 12, 12)
			});
			

			BeamGUI.DrawLogoBanner();
			
			EditorGUILayout.Space(12);

			
			EditorGUILayout.Space(6);

			{ // draw form
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space(1, true);
				EditorGUILayout.BeginVertical(new GUIStyle
				{
					// padding = new RectOffset(24, 24, 0, 0)
				}, GUILayout.Width(280));

				EditorGUILayout.LabelField("Welcome to Beamable. Please sign into your account.", _headerStyle, GUILayout.ExpandWidth(true));

				EditorGUILayout.Space(12);

				
				Rect rect;

				GUI.enabled = _loginPromise == null;
				
				{ // env/host
					EditorGUILayout.LabelField("Please select your Beamable Environment", _titleStyle);
					
					if (env == Env.Unset)
					{
						env = Env.Beamable;
						
						var previousHost = FirstNonEmpty(cli.latestConfig?.host, ConfigDefaults.host);
						if (previousHost != null)
						{
							host = previousHost;
							switch (host)
							{
								case BEAM_PROD_ENDPOINT:
									env = Env.Beamable;
									break;
								#if BEAMABLE_DEVELOPER
								case BEAM_STAGE_ENDPOINT:
									env = Env.Staging;
									break;
								case BEAM_DEV_ENDPOINT:
									env = Env.Dev;
									break;
								#endif
								default:
									env = Env.Custom;
									break;
							}
						}
					}
					
					
					if (env == Env.Custom)
					{
						rect = GUILayoutUtility.GetRect(GUIContent.none, _textboxStyle);
						host = BeamGUI.PlaceholderTextField(rect, host, "https://custom.beamable.com", _textboxStyle, _placeholderStyle);
						var clickedCustomEnv = BeamGUI.SoftRightLinkButton(new GUIContent("Use Standard Beamable"));
						if (clickedCustomEnv)
						{
							env = Env.Beamable;
						}
					}
					else
					{

						var envIndex = Array.IndexOf(envChoices, envMap[env]);
						var next = EditorGUILayout.Popup(envIndex, envChoices, _textboxStyle);
						env = reverseEnvMap[envChoices[next]];
						
						// env = (Env)EditorGUILayout.EnumPopup(env, _textboxStyle);
						EditorGUILayout.Space(26);

					}
					

				}
				
				EditorGUILayout.Space(18);

				
				{ // organization 
					EditorGUILayout.LabelField("Please enter your organization's cid or alias.", _titleStyle);

					if (cidOrAlias == null)
					{
						cidOrAlias = FirstNonEmpty(cli.latestAlias, ConfigDefaults.alias, ConfigDefaults.cid, cli.latestConfig?.cid);
					}
					
					rect = GUILayoutUtility.GetRect(GUIContent.none, _textboxStyle);
					cidOrAlias = BeamGUI.PlaceholderTextField(rect, cidOrAlias, "Enter Organization Alias", _textboxStyle, _placeholderStyle);

					// EditorGUILayout.Space(2);

					var clickedCreateOrg = BeamGUI.SoftRightLinkButton(new GUIContent("Create a new organization"));
					if (clickedCreateOrg)
					{
						EditorApplication.delayCall += () =>
						{
							var url = $"{GetPortalUriString()}/signup/registration?utm_source=unity-sdk";
							Application.OpenURL(url);
						};
					}
				}
				
				EditorGUILayout.Space(12);

				
				{ // email/password
					
					EditorGUILayout.LabelField("Please enter your account credentials.", _titleStyle);


					if (email == null && cli.latestAccount?.email?.Length > 0)
					{
						email = cli.latestAccount.email;
					}
					
					rect = GUILayoutUtility.GetRect(GUIContent.none, _textboxStyle);
					email = BeamGUI.PlaceholderTextField(rect, email, "Enter Email", _textboxStyle, _placeholderStyle);

					EditorGUILayout.Space(4);

					rect = GUILayoutUtility.GetRect(GUIContent.none, _textboxStyle);
					password = BeamGUI.PlaceholderPasswordField(rect, password, "Enter Password", _textboxStyle,
					                                            _placeholderStyle);
					// EditorGUILayout.Space(1);
					var clickedForgotPassword = BeamGUI.SoftRightLinkButton(new GUIContent("Forgot password"));
					if (clickedForgotPassword)
					{
						EditorApplication.delayCall += () =>
						{
							var url = $"{GetPortalUriString()}/forgot-password?utm_source=unity-sdk";
							Application.OpenURL(url);
						};
					}
				}
				
				EditorGUILayout.Space(24);
				
				{ // accept button

					var isAliasValid = cidOrAlias?.Length > 0;
					var isEmailValid = email?.Length > 0;
					var iasPasswordValid = password?.Length > 0;
					var isValidHost = GetHostString()?.Length > 0;

					var isButtonEnabled = isAliasValid && isEmailValid && iasPasswordValid && isValidHost;
					isButtonEnabled &= _loginPromise == null;
					
					GUI.enabled = isButtonEnabled;
					
					
					
					var wasClicked = BeamGUI.PrimaryButton(new GUIContent("Login"), allowEnterKeyToClick: true);
					GUI.enabled = true;

					if (cli.latestLoginError?.Length > 0)
					{
						EditorGUILayout.SelectableLabel(cli.latestLoginError, _errorStyle);
					}
					
					if (_loginPromise != null)
					{
						EditorGUILayout.Space(12);
						BeamGUI.LoadingSpinnerWithState("Logging in...");
					}
					
					
					if (wasClicked)
					{
						EditorApplication.delayCall += () =>
						{
							_previousCid = FirstNonEmpty(cli.Cid, ConfigDefaults.cid);
							_previousPid = cli.Pid;
							_previousBuildPid = ConfigDefaults.pid;
							_didApplyPreviousSelection = false;

							_loginPromise = context.Login(GetHostString(), cidOrAlias, email, password);
							_loginPromise.Then(_ =>
							{
								if (cli.latestGames?.VisibleGames.Length > 1) // if there is only one game, there is no reason to make a selection
								{
									needsGameSelection = true;
								}
								else if (TryResolvePreviousRealm(out string _, out var previousRealmPid) && previousRealmPid != cli.Pid)
								{
									// restore the realm the project was using before the login
									var __ = context.SwitchRealm(previousRealmPid);
								}
								_loginPromise = null;
							});
							_loginPromise.Error(ex =>
							{
								Debug.LogError(ex);
								_loginPromise = null;
							});
						};
					}
				}



				EditorGUILayout.EndVertical();
				EditorGUILayout.Space(1, true);

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.EndVertical();

		}

		static string FirstNonEmpty(params string[] values)
		{
			foreach (var value in values)
			{
				if (!string.IsNullOrEmpty(value))
				{
					return value;
				}
			}

			return null;
		}
	}
}
