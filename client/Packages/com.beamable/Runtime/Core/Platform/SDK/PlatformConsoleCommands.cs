using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.ConsoleCommands;
using Beamable.Coroutines;
using Beamable.Api.Analytics;
using Beamable.Common.Api.Groups;
using Beamable.Common.Api.Mail;
using Beamable.Service;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Scripting;

namespace Beamable.Api
{
    [BeamableConsoleCommandProvider]
    public class PlatformConsoleCommands
    {
        private BeamableConsole Console => ServiceManager.Resolve<BeamableConsole>();
        private CoroutineService CoroutineService => ServiceManager.Resolve<CoroutineService>();

        [Preserve]
        public PlatformConsoleCommands()
        {
        }

        [BeamableConsoleCommand("IDFA", "print advertising identifier", "IDFA")]
        private string PrintAdvertisingIdentifier(string[] args)
        {
            Application.RequestAdvertisingIdentifierAsync((id, trackingEnabled, error) =>
                Console.Log($"AdId = {id}\nTrackingEnabled={trackingEnabled}\nError = {error}"));

            return String.Empty;
        }

        [BeamableConsoleCommand("RESET", "Clear the access token and start with a fresh account", "RESET")]
        protected string ResetAccount(params string[] args)
        {
            var platform = ServiceManager.Resolve<PlatformService>();
            platform.ClearDeviceUsers();
            Console.Log(ForceRestart());
            return "Attempting access token reset...";
        }

        [BeamableConsoleCommand(new [] { "FORCE-RESTART", "FR"}, "Restart the game as if it had just been launched", "FORCE-RESTART")]
        public static string ForceRestart(params string[] args)
        {
            ServiceManager.OnTeardown();
            return "Game Restarted.";
        }

        /// <summary>
        /// Send a local notification test at some delay.
        /// </summary>
        [BeamableConsoleCommand(new [] {"LOCALNOTE", "LN"}, "Send a local notification. Default delay is 10 seconds.", "LOCALNOTE [<delay> [<title> [<body>]]]")]
        private string LocalNotificationCommand(params string[] args)
        {
            var title = "Test Notification Message Title";
            var message =
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";
            var delay = 10;
            if (args.Length >= 1)
            {
                int.TryParse(args[0], out delay);
            }
            if (args.Length >= 2)
            {
                title = args[1];
            }
            if (args.Length >= 3)
            {
                message = args[2];
            }
            var customData = new Dictionary<string, string> {{"evt", "test"}, {"foo", "123"}};
            var service = ServiceManager.Resolve<PlatformService>();

            string channel = "test";

            service.Notification.CreateNotificationChannel(channel, "Test", "Test notifications of regular importance.");
            service.Notification.ScheduleLocalNotification(channel, "DBCONSOLE", 0, title, message,
                TimeSpan.FromSeconds(delay), false, customData);
            return string.Format("Scheduled notification for {0} seconds in the future.", delay);
        }

        [BeamableConsoleCommand("GCCOLLECT", "Do a GC Collect and Unload Unused Assets", "GCCOLLECT")]
        private static string GCCollect(params string[] args)
        {
            Profiler.BeginSample("Memory collect test");
            Profiler.BeginSample("GC.Collect");
            System.GC.Collect();
            Profiler.EndSample();
            Profiler.BeginSample("Resources.UnloadUnusedAssets");
            Resources.UnloadUnusedAssets();
            Profiler.EndSample();
            Profiler.EndSample();
            return "";
        }

        [BeamableConsoleCommand(new [] {"TIMESCALE", "TS"}, "Sets the current timescale", "TIMESCALE <value> | variable")]
        private string Timescale(params string[] args)
        {
            if (args.Length < 1)
            {
                return Console.Help("TIMESCALE");
            }

            float timescale = 1;
            CoroutineService.StopCoroutine("VariableTimescale");
            if (args[0] == "variable")
            {
                CoroutineService.StartCoroutine("VariableTimescale");
                return "variable timescale";
            }
            else if (float.TryParse(args[0], out timescale))
            {
                Time.timeScale = timescale;
                return "setting timescale to " + timescale;
            }

            return "unknown timescale";
        }

        private IEnumerator VariableTimescale()
        {
            while (true)
            {
                Time.timeScale = (float) Mathf.Sqrt(UnityEngine.Random.Range(0f, 20.0f));
                yield return null;
            }
        }


        [BeamableConsoleCommand("SUBSCRIBER_DETAILS", "Query subscriber details", "SUBSCRIBER_DETAILS")]
        public string SubscriberDetails(string[] args)
        {
            ServiceManager.Resolve<PlatformService>().PubnubNotificationService.GetSubscriberDetails().Then(rsp => {
                Console.Log(
                    rsp.authenticationKey + " " +
                    rsp.customChannelPrefix + " " +
                    rsp.gameGlobalNotificationChannel + " " +
                    rsp.gameNotificationChannel + " " +
                    rsp.playerChannel + " " +
                    rsp.playerForRealmChannel + " " +
                    rsp.subscribeKey
                );
            }).Error(err => {
                Console.Log("Failed: " + err.ToString());
            });
            return "";
        }

        [BeamableConsoleCommand("DBID", "Show current player DBID", "DBID")]
        private string ShowDBID(params string[] args)
        {
            return ServiceManager.Resolve<PlatformService>().User.id.ToString();
        }

        [BeamableConsoleCommand("HEARTBEAT", "Get heartbeat of a user", "HEARTBEAT <dbid>")]
        string GetHeartbeat(params string[] args)
        {
            if (args.Length != 1)
            {
                return "Requires dbid";
            }
            var dbid = long.Parse(args[0]);
            ServiceManager.Resolve<PlatformService>().Session.GetHeartbeat(dbid)
                .Then(rsp => { Console.Log(rsp.ToString()); })
                .Error(err => { Console.Log(String.Format("Error:", err)); });

            return "Querying...";
        }

        /**
         * Login to a previously registered account with the given username and password.
         */
        [BeamableConsoleCommand("LOGIN_ACCOUNT", "Log in to the DBID designated by the given username and password", "LOGIN_ACCOUNT <email> <password>")]
        string LoginAccount(params string[] args)
        {
            if (args.Length < 2)
            {
                return "Requires both an email and a password.";
            }
            var email = args[0];
            var password = args[1];
            ServiceManager.Resolve<PlatformService>().Auth.Login(email, password).Then(rsp =>
            {
                ServiceManager.Resolve<PlatformService>().SaveToken(rsp);
                ServiceManager.Resolve<PlatformService>().ReloadUser();
                Console.Log(String.Format("Successfully logged in as {0}.", email));
            }).Error(err =>
            {
                if (err is PlatformRequesterException code && code.Error.error == "UnableToMergeError")
                {
                    ServiceManager.Resolve<PlatformService>().Auth.Login(email, password, mergeGamerTagToAccount: false)
                        .Then(rsp =>
                        {
                            ServiceManager.Resolve<PlatformService>().SaveToken(rsp);
                            ServiceManager.Resolve<PlatformService>().ReloadUser();
                            Console.Log(String.Format("Successfully SWITCHED to {0}. Resetting", email));
                            Console.Execute("RESET");
                        }).Error(err2 =>
                        {
                            Console.Log(String.Format("There was an error trying to log in as user: {0} - {1}", email, err2));
                        });
                }
                else
                {
                    Console.Log(String.Format("There was an error trying to log in as user: {0} - {1}", email, err));
                }
            });
            return "Logging in as user: " + email;
        }

        /**
         * Get the counts of the mailbox
         */
        [BeamableConsoleCommand("MAIL_GET", "Get mailbox messages", "MAIL_GET <category>")]
        string GetMail(params string[] args)
        {
            if (args.Length < 1)
            {
                return "Requires category";
            }

            ServiceManager.Resolve<PlatformService>().Mail.GetMail(args[0]).Then(rsp =>
            {
                for (int i=0; i<rsp.result.Count; i++) {
                    var next = rsp.result[i];
                    Console.Log("[" + next.id + "]");
                    Console.Log("FROM: " + next.senderGamerTag);
                    Console.Log(next.subject);
                    Console.Log("(" + next.rewards.items.Count + " items)");
                    Console.Log("(" + next.rewards.currencies.Count + " currencies)");
                    Console.Log(next.body);
                    Console.Log("");
                }
                Console.Log("DONE");
            }).Error(err =>
            {
                Console.Log(String.Format("Error:", err));
            });
            return "Querying...";
        }

        [BeamableConsoleCommand("MAIL_SEND", "Send a message via the mail system.", "MAIL_SEND <receiver> <body>")]
        string SendMail(params string[] args)
        {
            var platform = ServiceManager.Resolve<PlatformService>();
            if (args.Length < 2)
            {
                return "Requires receiver and body";
            }

            var receiver = long.Parse(args[0]);
            var body = args[1];
            var request = new MailSendRequest();
            request.Add(new MailSendEntry
            {
                senderGamerTag = platform.UserId,
                receiverGamerTag = receiver,
                category = "test",
                subject = "test message",
                body = body
            });
            platform.Mail.SendMail(request).Then(rsp =>
            {
                Console.Log(JsonUtility.ToJson(rsp));
            }).Error(err =>
            {
                Console.Log($"Error: {err}");
            });
            return "Mail sent!";
        }

        /**
         * Update a mail in the mailbox
         */
        [BeamableConsoleCommand("MAIL_UPDATE", "Update a mail", "MAIL_UPDATE <id> <state> <acceptAttachments>")]
        string UpdateMail(params string[] args)
        {
            if (args.Length < 2)
            {
                return "Requires mailId and state";
            }

            string mailId = args[0];
            string stateStr = args[1];
            MailState state = (MailState)Enum.Parse(typeof(MailState), stateStr);
            bool acceptAttachments = args.Length >= 3;

            MailUpdateRequest updates = new MailUpdateRequest();
            updates.Add(long.Parse(mailId), state, acceptAttachments, "");
            ServiceManager.Resolve<PlatformService>().Mail.Update(updates).Then(rsp =>
            {
                Console.Log(JsonUtility.ToJson(rsp));
            }).Error(err =>
            {
                Console.Log(String.Format("Error:", err));
            });
            return "Updating...";
        }

        /**
         * Registers the current DBID to the given username and password.
         */
        [BeamableConsoleCommand("REGISTER_ACCOUNT", "Registers this DBID with the given username and password", "REGISTER_ACCOUNT <email> <password>")]
        string RegisterAccount(params string[] args)
        {
            if (args.Length < 2)
            {
                return "Requires both an email and a password.";
            }
            var email = args[0];
            var password = args[1];
            ServiceManager.Resolve<PlatformService>().Auth.RegisterDBCredentials(email, password)
                .Then(rsp => { Console.Log(String.Format("Successfully registered user {0}", email)); })
                .Error(err => { Console.Log(err.ToString()); });

            return "Registering user: " + email;
        }

        [BeamableConsoleCommand("TOKEN", "Show current access token", "TOKEN")]
        private static string ShowToken(params string[] args)
        {
            return ServiceManager.Resolve<PlatformService>().AccessToken.Token;
        }

        [BeamableConsoleCommand("EXPIRE_TOKEN", "Expires the current access token to trigger the refresh flow", "EXPIRE_TOKEN")]
        public string ExpireAccessToken(params string[] args)
        {
            var platform = ServiceManager.Resolve<PlatformService>();
            platform.AccessToken.ExpireAccessToken();
            ServiceManager.OnTeardown();
            return "Access Token is now expired. Restarting.";
        }

        [BeamableConsoleCommand("CORRUPT_TOKEN", "Corrupts the current access token to trigger the refresh flow", "CORRUPT_TOKEN")]
        public string CorruptAccessToken(params string[] args)
        {
            var platform = ServiceManager.Resolve<PlatformService>();
            platform.AccessToken.CorruptAccessToken();
            return "Access Token has been corrupted.";
        }

        [BeamableConsoleCommand("TEST-ANALYTICS", "Run 1000 events to test batching/load", "TEST-ANALYTICS")]
        public string TestAnalytics(params string[] args)
        {
            var evt = new SampleCustomEvent("lorem ipsum dolar set amet", "T-T-T-Test the base!");

            ServiceManager.Resolve<PlatformService>().Analytics.TrackEvent(evt);
            for (var i = 0; i < 1000; ++i)
            {
                ServiceManager.Resolve<PlatformService>().Analytics.TrackEvent(evt);
            }
            return "Analytics Sent";
        }

        [BeamableConsoleCommand("IAP_BUY", "Invokes the real money transaction flow to purchase the given item_symbol.", "IAP_BUY <listing> <sku>")]
        string IAPBuy(params string[] args)
        {
            if (args.Length != 2)
            {
                return "Requires: <listing> <sku>";
            }

            ServiceManager.Resolve<PlatformService>().BeamablePurchaser.StartPurchase(args[0], args[1])
                .Then((txn) => { Console.Log("Purchase Complete: " + txn.Txid); })
                .Error((err) => { Console.Log("Purchase Failed: " + err.ToString()); });

            return "Purchasing item: " + args[0];
        }

        [BeamableConsoleCommand("IAP_PENDING", "Displays pending transactions", "IAP_PENDING")]
        string IAPPending(params string[] args)
        {
            return PlayerPrefs.GetString("pending_purchases");
        }

        [BeamableConsoleCommand("IAP_UNFULFILLED", "Display unfulfilled purchases", "IAP_UNFULFILLED")]
        string IAPUnfulfilled(params string[] args)
        {
            return PlayerPrefs.GetString("unfulfilled_transactions");
        }

        /**
         * Get the group info of a user
         */
        [BeamableConsoleCommand("GROUP_USER", "Query a user for group info", "GROUP_USER <dbid>")]
        string GetGroupUser(params string[] args)
        {
            long gamerTag;
            if (args.Length < 1) {
                gamerTag = ServiceManager.Resolve<PlatformService>().User.id;
            } else {
                gamerTag = long.Parse(args[0]);
            }
            ServiceManager.Resolve<PlatformService>().Groups.GetUser(gamerTag)
                .Then(rsp => { Console.Log(JsonUtility.ToJson(rsp)); })
                .Error(err => { Console.Log(String.Format("Error:", err)); });
            return "Querying...";
        }

        [BeamableConsoleCommand("GROUP_LEAVE", "Leave the current group", "GROUP_LEAVE")]
        string GroupLeave(params string[] args)
        {
            long gamerTag = ServiceManager.Resolve<PlatformService>().User.id;
            ServiceManager.Resolve<PlatformService>().Groups.GetUser(gamerTag)
                .FlatMap<GroupMembershipResponse>(userRsp => {
                    long group = 0;
                    if (userRsp.member.guild.Count > 0) {
                        group = userRsp.member.guild[0].id;
                    }
                    return ServiceManager.Resolve<PlatformService>().Groups.LeaveGroup(group);
                })
                .Error(err => { Console.Log(String.Format("Error:", err)); });
            return "Querying...";
        }



        /**
         * View stats for a user
         */
        [BeamableConsoleCommand("GET_STATS", "Get stats for some user", "GET_STATS <domain> <access> <type> <id>")]
        string GetStats(params string[] args)
        {
            if (args.Length != 4)
            {
                return "Requires: <DOMAIN> <ACCESS> <TYPE> <ID>";
            }

            var platform = ServiceManager.Resolve<PlatformService>();
            platform.Stats.GetStats(args[0], args[1], args[2], long.Parse(args[3]))
                .Then(rsp =>
                {
                    foreach (var next in rsp)
                    {
                        Console.Log(String.Format("{0} = {1}", next.Key, next.Value));
                    }
                    Console.Log("Done");
                })
                .Error(err => { Console.Log(String.Format("Error:", err)); });
            return "Querying...";
        }

        /**
         * Set stat for a user
         */
        [BeamableConsoleCommand("SET_STAT", "Sets client stat for self", "SET_STAT <access> <key> <value>")]
        string SetStat(params string[] args)
        {
            if (args.Length != 3)
            {
                return "Requires: <ACCESS> <KEY> <VALUE>";
            }

            var platform = ServiceManager.Resolve<PlatformService>();
            Dictionary<string, string> stats = new Dictionary<string, string>();
            stats.Add(args[1], args[2]);
            platform.Stats.SetStats(args[0], stats)
                .Then(rsp => Console.Log("Done"))
                .Error(err => Console.Log(String.Format("Error:", err)) );
            return "Querying...";
        }

        [BeamableConsoleCommand("SET_TIME", "Sets the override time. If no time is specified, then there will be no override", "SET_TIME <time>")]
        string SetTime(params string[] args)
        {
            var platform = ServiceManager.Resolve<PlatformService>();
            if (args.Length == 0)
            {
                platform.TimeOverride = null;
                return "Clearing Override Time";
            }

            try
            {
                platform.TimeOverride = args[0];
                return String.Format("Setting Override: {0}", platform.TimeOverride);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return "Invalid Time";
            }
        }

    }
}
