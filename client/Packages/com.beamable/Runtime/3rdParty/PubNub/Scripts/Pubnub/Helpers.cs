﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Collections;
using System.Text;

namespace PubNubMessaging.Core
{
    public sealed class Counter
    {
        private uint current = 0;
        private object syncRoot;

        public Counter(){
            syncRoot = new Object();
            current = 0;
        }

        public uint NextValue()
        {
            lock (syncRoot) {
                this.current++;
            }
            return this.current;
        }

        public void Reset()
        {
            lock (syncRoot) {
                this.current = 0;
            }
        }
    }

    internal static class Helpers
    {
        #region "Helpers"
        internal static TimetokenMetadata CreateTimetokenMetadata (object timeTokenDataObject, string whichTT)
        {
            IDictionary<string, object> timeTokenData = (IDictionary<string, object>)timeTokenDataObject;
            TimetokenMetadata timetokenMetadata = new TimetokenMetadata (Utility.CheckKeyAndParseLong(timeTokenData, whichTT, "t"), 
                timeTokenData["r"].ToString());

            LoggingMethod.WriteToLog (string.Format ("DateTime {0}, TimetokenMetadata: {1} \nTimetoken: {2} \nRegion: {3}", 
                DateTime.Now.ToString (), whichTT, timetokenMetadata.Timetoken, timetokenMetadata.Region), 
                LoggingMethod.LevelInfo);
            
            return timetokenMetadata;
        }

        internal static void AddToSubscribeMessageList (object dictObject, ref List<SubscribeMessage> subscribeMessages)
        {
            var dict = dictObject as IDictionary<string, object>;      
            if ((dict != null) && (dict.Count > 1)) {
                string shard = (dict.ContainsKey("a")) ? dict ["a"].ToString () : "";
                string subscriptionMatch = (dict.ContainsKey ("b")) ? dict ["b"].ToString () : "";
                string channel = (dict.ContainsKey ("c")) ? dict ["c"].ToString () : "";
                object payload = (dict.ContainsKey ("d")) ? (object)dict ["d"] : null;
                string flags = (dict.ContainsKey ("f")) ? dict ["f"].ToString () : "";
                string issuingClientId = (dict.ContainsKey ("i")) ? dict ["i"].ToString () : "";
                string subscribeKey = (dict.ContainsKey ("k")) ? dict ["k"].ToString () : "";
                long sequenceNumber = Utility.CheckKeyAndParseLong (dict, "sequenceNumber", "s"); 

                TimetokenMetadata originatingTimetoken = (dict.ContainsKey ("o")) ? CreateTimetokenMetadata (dict ["o"], "Originating TT: ") : null;
                TimetokenMetadata publishMetadata = (dict.ContainsKey ("p")) ? CreateTimetokenMetadata (dict ["p"], "Publish TT: ") : null;
                object userMetadata = (dict.ContainsKey ("u")) ? (object)dict ["u"] : null;

                SubscribeMessage subscribeMessage = new SubscribeMessage (
                    shard,
                    subscriptionMatch,
                    channel,
                    payload,
                    flags,
                    issuingClientId,
                    subscribeKey,
                    sequenceNumber,
                    originatingTimetoken,
                    publishMetadata,
                    userMetadata
                );
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, AddToSubscribeMessageList: \n" +
                    "shard : {1},\n" +
                    "subscriptionMatch: {2},\n" +
                    "channel: {3},\n" +
                    "payload: {4},\n" +
                    "flags: {5},\n" +
                    "issuingClientId: {6},\n" +
                    "subscribeKey: {7},\n" +
                    "sequenceNumber: {8},\n" +
                    "originatingTimetoken tt: {9},\n" +
                    "originatingTimetoken region: {10},\n" +
                    "publishMetadata tt: {11},\n" +
                    "publishMetadata region: {12},\n" +
                    "userMetadata {13} \n",
                    DateTime.Now.ToString (), 
                    shard,
                    subscriptionMatch,
                    channel,
                    payload.ToString (),
                    flags,
                    issuingClientId,
                    subscribeKey,
                    sequenceNumber,
                    (originatingTimetoken != null) ? originatingTimetoken.Timetoken.ToString () : "",
                    (originatingTimetoken != null) ? originatingTimetoken.Region : "",
                    (publishMetadata != null) ? publishMetadata.Timetoken.ToString () : "",
                    (publishMetadata != null) ? publishMetadata.Region : "",
                    (userMetadata != null) ? userMetadata.ToString () : "null"), 
                    LoggingMethod.LevelInfo);
                

                subscribeMessages.Add (subscribeMessage);
            } 
            else {
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, AddToSubscribeMessageList: CreateListOfSubscribeMessage create " +
                    "SubscribeMessage failed. dictObject type: {1}, dict type : {2}", 
                    DateTime.Now.ToString (), dictObject.ToString (), dict.ToString ()), LoggingMethod.LevelInfo);
            }
            
        }

        internal static List<SubscribeMessage> CreateListOfSubscribeMessage (object message)
        {
            List<SubscribeMessage> subscribeMessages = new List<SubscribeMessage> ();
            if (message != null) {
                //JSONFx
                object[] messages = message as object[];

                if (messages != null) {
                    var myObjectArray = (from item in messages
                                                    select item as object).ToArray ();
                    if ((myObjectArray!= null) && (myObjectArray.Length > 0)) {
                        
                        foreach (object dictObject in myObjectArray) {
                            AddToSubscribeMessageList (dictObject, ref subscribeMessages);
                        }
                    }
                } else {
                    //MiniJSON
                    List<object> messageList = message as List<object>;
                    if ((messageList != null) && messageList.Count > 0) {
                        foreach (object dictObject in messageList) {
                            AddToSubscribeMessageList (dictObject, ref subscribeMessages);
                        }
                    }
                }
            }
            else {
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, CreateListOfSubscribeMessage: no messages ",
                    DateTime.Now.ToString ()), LoggingMethod.LevelInfo);
            }
            
            return subscribeMessages;
        }

        internal static string BuildJsonUserState (IDictionary<string, object> userStateDictionary)
        {
            StringBuilder jsonStateBuilder = new StringBuilder ();

            if (userStateDictionary != null) {
                string[] userStateKeys = userStateDictionary.Keys.ToArray<string> ();

                for (int keyIndex = 0; keyIndex < userStateKeys.Length; keyIndex++) {
                    string useStateKey = userStateKeys [keyIndex];
                    object userStateValue = userStateDictionary [useStateKey];
                    if (userStateValue == null) {
                        jsonStateBuilder.AppendFormat ("\"{0}\":{1}", useStateKey, string.Format ("\"{0}\"", "null"));
                    } else {
                        jsonStateBuilder.AppendFormat ("\"{0}\":{1}", useStateKey, (userStateValue.GetType ().ToString () == "System.String") ? string.Format ("\"{0}\"", userStateValue) : userStateValue);
                    }
                    if (keyIndex < userStateKeys.Length - 1) {
                        jsonStateBuilder.Append (",");
                    }
                }
            }

            return jsonStateBuilder.ToString ();
        }

        internal static string BuildJsonUserState (List<ChannelEntity> ce)
        {
            string retJsonUserState = "";

            StringBuilder jsonStateBuilder = new StringBuilder ();

            if (ce != null) {
                foreach (ChannelEntity c in ce) {
                    string currentJsonState = BuildJsonUserState (c.ChannelParams.UserState);
                    if (!string.IsNullOrEmpty (currentJsonState)) {
                        currentJsonState = string.Format ("\"{0}\":{{{1}}}",c.ChannelID.ChannelOrChannelGroupName, currentJsonState);
                        if (jsonStateBuilder.Length > 0) {
                            jsonStateBuilder.Append (",");
                        }
                        jsonStateBuilder.Append (currentJsonState);
                    }
                }

                if (jsonStateBuilder.Length > 0) {
                    retJsonUserState = string.Format ("{{{0}}}", jsonStateBuilder.ToString ());
                }
            }

            return retJsonUserState;
        }

        internal static string GetNamesFromChannelEntities (List<ChannelEntity> channelEntities, bool isChannelGroup){
            
            StringBuilder sb = new StringBuilder ();
            if (channelEntities != null) {
                int count = 0;
                foreach (ChannelEntity c in channelEntities) {
                    if (isChannelGroup && c.ChannelID.IsChannelGroup) {
                        if (count > 0) {
                            sb.Append (",");
                        }

                        sb.Append (c.ChannelID.ChannelOrChannelGroupName);
                        count++;
                    } else if (!isChannelGroup && !c.ChannelID.IsChannelGroup) {
                        if (count > 0) {
                            sb.Append (",");
                        }

                        sb.Append (c.ChannelID.ChannelOrChannelGroupName);
                        count++;
                    }
                }
            }
            return sb.ToString();
        }

        internal static string GetNamesFromChannelEntities (List<ChannelEntity> channelEntities){
            StringBuilder sbCh = new StringBuilder ();
            StringBuilder sbChGrp = new StringBuilder ();
            if (channelEntities != null) {
                int countCh = 0;
                int countChGrp = 0;
                foreach (ChannelEntity c in channelEntities) {
                    if (c.ChannelID.IsChannelGroup) {
                        if (countChGrp > 0) {
                            sbChGrp.Append (",");
                        }

                        sbChGrp.Append (c.ChannelID.ChannelOrChannelGroupName);
                        countChGrp++;
                    } else {
                        if (countCh > 0) {
                            sbCh.Append (",");
                        }

                        sbCh.Append (c.ChannelID.ChannelOrChannelGroupName);
                        countCh++;
                    }

                }
            }
            return string.Format ("channel(s) = {0} and channelGroups(s) = {1}", sbCh.ToString(), sbChGrp.ToString());
        }

        internal static bool UpdateOrAddUserStateOfEntity<T>(string channel, bool isChannelGroup, 
            IDictionary<string, object> userState, bool edit,
            Action<T> userCallback, Action<PubnubClientError> errorCallback, 
            PubnubErrorFilter.Level errorLevel, ref List<ChannelEntity> channelEntities)
        {
            ChannelEntity ce = CreateChannelEntity<T> (channel, false, isChannelGroup, 
                userState, userCallback, null, errorCallback, null, null);
            bool stateChanged = Subscription.Instance.UpdateOrAddUserStateOfEntity (ref ce, userState, edit);
            if (!stateChanged) {
                string message = "No change in User State";

                PubnubCallbacks.CallErrorCallback<T> (ce, message,
                    PubnubErrorCode.UserStateUnchanged, PubnubErrorSeverity.Info, errorLevel);
            } else {
                channelEntities.Add (ce);
            }
            return stateChanged;
        }

        internal static bool CheckAndAddExistingUserState<T>(string channel, string channelGroup, IDictionary<string, object> userState,
            Action<T> userCallback, Action<PubnubClientError> errorCallback, 
            PubnubErrorFilter.Level errorLevel, bool edit,
            out string returnUserState, out List<ChannelEntity> channelEntities 
        )
        {
            string[] channels = channel.Trim().Split (',');
            string[] channelGroups = channelGroup.Trim().Split (',');
            bool stateChanged = false;
            channelEntities = new List<ChannelEntity> ();

            foreach (string ch in channels) {
                if (!string.IsNullOrEmpty (ch)) {
                    bool changeState = UpdateOrAddUserStateOfEntity<T> (ch, false, userState, edit,
                        userCallback, errorCallback, errorLevel, ref channelEntities);
                    if (changeState && !stateChanged) {
                        stateChanged = true;
                    }
                }
            }

            foreach (string ch in channelGroups) {
                if (!string.IsNullOrEmpty (ch)) {
                    bool changeState = UpdateOrAddUserStateOfEntity<T> (ch, true, userState, edit,
                        userCallback, errorCallback, errorLevel, ref channelEntities);
                    if (changeState && !stateChanged) {
                        stateChanged = true;
                    }
                }
            }

            returnUserState = BuildJsonUserState(channelEntities);

            return stateChanged;
        }

        internal static ChannelEntity CreateChannelEntity<T>(string channelOrChannelGroupName2, 
            bool isAwaitingConnectCallback,
            bool isChannelGroup, IDictionary<string, object> userState,
            Action<T> userCallback, Action<T> connectCallback, 
            Action<PubnubClientError> errorCallback, Action<T> disconnectCallback, Action<T> wildcardPresenceCallback
        ){
            string channelOrChannelGroupName = channelOrChannelGroupName2.Trim ();
            LoggingMethod.WriteToLog(string.Format("DateTime {0}, CreateChannelEntity: channelOrChannelGroupName {1}, {2}", DateTime.Now.ToString(), 
                channelOrChannelGroupName.ToString(), channelOrChannelGroupName2.ToString()), LoggingMethod.LevelInfo);
            
            if (!string.IsNullOrEmpty(channelOrChannelGroupName)) {
                ChannelIdentity ci = new ChannelIdentity ();
                ci.ChannelOrChannelGroupName = channelOrChannelGroupName;
                ci.IsPresenceChannel = Utility.IsPresenceChannel (channelOrChannelGroupName);
                ci.IsChannelGroup = isChannelGroup;

                ChannelParameters cp = new ChannelParameters ();
                cp.IsAwaitingConnectCallback = isAwaitingConnectCallback;
                cp.UserState = userState;
                cp.TypeParameterType = typeof(T);

                cp.Callbacks = PubnubCallbacks.GetPubnubChannelCallback<T> (userCallback, connectCallback, errorCallback, 
                    disconnectCallback, wildcardPresenceCallback);

                ChannelEntity ce = new ChannelEntity (ci, cp);

                return ce;
            } else {
                LoggingMethod.WriteToLog(string.Format("DateTime {0}, CreateChannelEntity: channelOrChannelGroupName empty, is channel group {1}", DateTime.Now.ToString(), isChannelGroup.ToString()), LoggingMethod.LevelInfo);
            
                return null;
            }
        }

        internal static List<ChannelEntity> CreateChannelEntity<T>(string[] channelOrChannelGroupNames, bool isAwaitingConnectCallback,
            bool isChannelGroup, IDictionary<string, object> userState,
            Action<T> userCallback, Action<T> connectCallback, 
            Action<PubnubClientError> errorCallback, Action<T> disconnectCallback, Action<T> wildcardPresenceCallback
        ){
            List<ChannelEntity> channelEntities = null;
            if (channelOrChannelGroupNames != null) {
                channelEntities = new List<ChannelEntity> ();
                foreach (string ch in channelOrChannelGroupNames) {
                    ChannelEntity chEntity= CreateChannelEntity<T> (ch, isAwaitingConnectCallback, isChannelGroup, userState,
                        userCallback, connectCallback, errorCallback, disconnectCallback, wildcardPresenceCallback
                    );
                    if (chEntity != null) {
                        channelEntities.Add (chEntity);
                    }
                }
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, CreateChannelEntity 2: channelEntities={1}", DateTime.Now.ToString (), channelEntities.Count), LoggingMethod.LevelInfo);
                
            } else {
                LoggingMethod.WriteToLog(string.Format("DateTime {0}, CreateChannelEntity 2: channelOrChannelGroupNames null, is channel group {1}", DateTime.Now.ToString(), isChannelGroup.ToString()), LoggingMethod.LevelInfo);
                
            }

            return channelEntities;
        }

        internal static IEnumerable<string> GetDuplicates(string[] 
            rawChannels)
        {
            var results = from string a in rawChannels
                group a by a into g
                    where g.Count() > 1
                select g;
            foreach (var group in results)
                foreach (var item in group)
                    yield return item;
        }

        [System.Diagnostics.Conditional("SPEW_ALL")]
        [System.Diagnostics.Conditional("SPEW_NOTIFICATION")]
        internal static void LogChannelEntitiesDictionary(){
            StringBuilder sbLogs = new StringBuilder();
            foreach (var ci in Subscription.Instance.ChannelEntitiesDictionary) {
                sbLogs.AppendFormat("\nChannelEntitiesDictionary \nChannelOrChannelGroupName:{0} \nIsChannelGroup:{1} \nIsPresenceChannel:{2}\n", 
                    ci.Key.ChannelOrChannelGroupName, ci.Key.IsChannelGroup.ToString(), ci.Key.IsPresenceChannel.ToString()
                );
            }
            LoggingMethod.WriteToLog (string.Format ("DateTime {0}, LogChannelEntitiesDictionary: Count: {2} \n{1}", 
                DateTime.Now.ToString (), sbLogs.ToString(), Subscription.Instance.ChannelEntitiesDictionary.Count), LoggingMethod.LevelInfo);
        }
        
        internal static bool CreateChannelEntityAndAddToSubscribe <T>(ResponseType type, string[] rawChannels, 
            bool isChannelGroup,
            Action<T> userCallback, Action<T> connectCallback, Action<PubnubClientError> errorCallback, 
            Action<T> wildcardPresenceCallback, Action<T> disconnectCallback,
            PubnubErrorFilter.Level errorLevel, bool unsubscribeCheck, ref List<ChannelEntity> channelEntities
            )
        {
            bool bReturn = false;    
            for (int index = 0; index < rawChannels.Length; index++)
            {
                string channelName = rawChannels[index].Trim();
               
                if (channelName.Length > 0) {
                    if((type == ResponseType.PresenceV2) 
                        || (type == ResponseType.PresenceUnsubscribe)) {
                        channelName = string.Format ("{0}{1}", channelName, Utility.PresenceChannelSuffix);
                    }

                    Helpers.LogChannelEntitiesDictionary();
                    LoggingMethod.WriteToLog (string.Format ("DateTime {0}, CreateChannelEntityAndAddToSubscribe: channel={1}", DateTime.Now.ToString (), channelName), LoggingMethod.LevelInfo);
                    
                    //create channelEntity
                    ChannelEntity ce = Helpers.CreateChannelEntity (channelName, true, isChannelGroup, null, 
                        userCallback, connectCallback, errorCallback, disconnectCallback, wildcardPresenceCallback);

                    bool channelIsSubscribed = false;
                    if (Subscription.Instance.ChannelEntitiesDictionary.ContainsKey (ce.ChannelID)){
                        channelIsSubscribed = Subscription.Instance.ChannelEntitiesDictionary [ce.ChannelID].IsSubscribed;
                    }

                    if (unsubscribeCheck) {
                        if (!channelIsSubscribed) {
                            string message = string.Format ("{0}Channel Not Subscribed", (ce.ChannelID.IsPresenceChannel) ? "Presence " : "");
                            PubnubErrorCode errorType = (ce.ChannelID.IsPresenceChannel) ? PubnubErrorCode.NotPresenceSubscribed : PubnubErrorCode.NotSubscribed;
                            LoggingMethod.WriteToLog (string.Format ("DateTime {0}, CreateChannelEntityAndAddToSubscribe: channel={1} response={2}", DateTime.Now.ToString (), channelName, message), LoggingMethod.LevelInfo);
                            PubnubCallbacks.CallErrorCallback<T> (ce, message,
                                errorType, PubnubErrorSeverity.Info, errorLevel);
                        } else {
                            channelEntities.Add (ce);
                            bReturn = true;
                        }
                    } else {
                        if (channelIsSubscribed) {
                            string message = string.Format ("{0}Already subscribed", (ce.ChannelID.IsPresenceChannel) ? "Presence " : "");
                            PubnubErrorCode errorType = (ce.ChannelID.IsPresenceChannel) ? PubnubErrorCode.AlreadyPresenceSubscribed : PubnubErrorCode.AlreadySubscribed;
                            PubnubCallbacks.CallErrorCallback<T> (ce, message,
                                errorType, PubnubErrorSeverity.Info, errorLevel);
                            LoggingMethod.WriteToLog (string.Format ("DateTime {0}, CreateChannelEntityAndAddToSubscribe: channel={1} response={2}", DateTime.Now.ToString (), channelName, message), LoggingMethod.LevelInfo);
                            
                        } else {
                            channelEntities.Add (ce);
                            bReturn = true;
                        }
                    }
                } else {
                    string message = "Invalid Channel Name";
                    if (isChannelGroup) {
                        message = "Invalid Channel Group Name";
                    }

                    LoggingMethod.WriteToLog(string.Format("DateTime {0}, CreateChannelEntityAndAddToSubscribe: channel={1} response={2}", DateTime.Now.ToString(), channelName, message), 
                        LoggingMethod.LevelInfo);
                    
                }
            }
            return bReturn;
        }

        internal static bool RemoveDuplicatesCheckAlreadySubscribedAndGetChannelsCommon<T>(ResponseType type, 
            Action<T> userCallback, Action<T> connectCallback, Action<PubnubClientError> errorCallback, 
            Action<T> wildcardPresenceCallback, Action<T> disconnectCallback,
            string[] channelsOrChannelGroups, bool isChannelGroup,
            PubnubErrorFilter.Level errorLevel, bool unsubscribeCheck, ref List<ChannelEntity> channelEntities)
        {
            bool bReturn = false;
            if (channelsOrChannelGroups.Length > 0) {
                
                channelsOrChannelGroups = channelsOrChannelGroups.Where(x => !string.IsNullOrEmpty(x)).ToArray();

                if (channelsOrChannelGroups.Length != channelsOrChannelGroups.Distinct ().Count ()) {
                    channelsOrChannelGroups = channelsOrChannelGroups.Distinct ().ToArray ();
                    LoggingMethod.WriteToLog (string.Format ("DateTime {0}, RemoveDuplicatesCheckAlreadySubscribedAndGetChannelsCommon: distinct channelsOrChannelGroups len={1}, channelsOrChannelGroups = {2}", 
                        DateTime.Now.ToString (), channelsOrChannelGroups.Length, string.Join(",", channelsOrChannelGroups)), 
                        LoggingMethod.LevelInfo);
                    
                    string channel = string.Join (",", GetDuplicates (channelsOrChannelGroups).Distinct<string> ().ToArray<string> ());
                    LoggingMethod.WriteToLog (string.Format ("DateTime {0}, RemoveDuplicatesCheckAlreadySubscribedAndGetChannelsCommon: duplicates channelsOrChannelGroups {1}", 
                        DateTime.Now.ToString (), channel), 
                        LoggingMethod.LevelInfo);
                    
                    string message = string.Format ("Detected and removed duplicate channels {0}", channel); 

                    PubnubCallbacks.CallErrorCallback<T> (message, errorCallback, PubnubErrorCode.DuplicateChannel, 
                        PubnubErrorSeverity.Info, errorLevel);
                }
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, RemoveDuplicatesCheckAlreadySubscribedAndGetChannelsCommon: channelsOrChannelGroups len={1}, channelsOrChannelGroups = {2}", 
                    DateTime.Now.ToString (), channelsOrChannelGroups.Length, string.Join(",", channelsOrChannelGroups)), 
                    LoggingMethod.LevelInfo);
                
                bReturn = CreateChannelEntityAndAddToSubscribe<T> (type, channelsOrChannelGroups, isChannelGroup, 
                    userCallback, connectCallback, errorCallback, wildcardPresenceCallback, disconnectCallback, errorLevel, unsubscribeCheck, ref channelEntities);
            } else {
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, RemoveDuplicatesCheckAlreadySubscribedAndGetChannelsCommon: channelsOrChannelGroups len <=0", 
                    DateTime.Now.ToString ()), 
                    LoggingMethod.LevelInfo);
                
            }
            return bReturn;
        }

        internal static bool RemoveDuplicatesCheckAlreadySubscribedAndGetChannels<T>(ResponseType type, 
            Action<T> userCallback, Action<T> connectCallback, Action<PubnubClientError> errorCallback, 
            Action<T> wildcardPresenceCallback, Action<T> disconnectCallback,
            string[] rawChannels, string[] rawChannelGroups, 
            PubnubErrorFilter.Level errorLevel, bool unsubscribeCheck, out List<ChannelEntity> channelEntities)
        {
            bool bReturn = false;
            bool channelAdded = false;
            bool channelGroupAdded = false;
            channelEntities = new List<ChannelEntity> ();
            if (rawChannels != null && rawChannels.Length > 0) {
                channelAdded = RemoveDuplicatesCheckAlreadySubscribedAndGetChannelsCommon<T> (type, userCallback, connectCallback, errorCallback,
                    wildcardPresenceCallback, disconnectCallback, rawChannels, false, errorLevel, unsubscribeCheck, ref channelEntities
                );
            }

            if (rawChannelGroups != null && rawChannelGroups.Length > 0) {
                channelGroupAdded = RemoveDuplicatesCheckAlreadySubscribedAndGetChannelsCommon<T> (type, userCallback, connectCallback, errorCallback,
                    wildcardPresenceCallback, disconnectCallback, rawChannelGroups, true, errorLevel, unsubscribeCheck, ref channelEntities
                );
            }

            bReturn = channelAdded | channelGroupAdded;

            return bReturn;
        }

        internal static void ProcessResponseCallbacksV2<T> (ref SubscribeEnvelope resultSubscribeEnvelope, RequestState<T> asynchRequestState, 
            string cipherKey, IJsonPluggableLibrary jsonPluggableLibrary)
        {
            
            if (resultSubscribeEnvelope != null) {
                Helpers.ResponseToConnectCallback<T> (asynchRequestState, jsonPluggableLibrary);
                if (resultSubscribeEnvelope.Messages != null) {
                    ResponseToUserCallbackForSubscribeV2<T> (resultSubscribeEnvelope.Messages, asynchRequestState.ChannelEntities, 
                        cipherKey, jsonPluggableLibrary);
                }
                else {
                    LoggingMethod.WriteToLog (string.Format ("DateTime {0}, ProcessResponseCallbacksV2: resultSubscribeEnvelope.Messages null", 
                        DateTime.Now.ToString ()), LoggingMethod.LevelError);
                }
                
            } 
        }

        internal static void ProcessResponseCallbacks<T> (ref List<object> result, RequestState<T> asynchRequestState, 
            string cipherKey, IJsonPluggableLibrary jsonPluggableLibrary)
        {
            if (result != null && result.Count >= 1) {
                Helpers.ResponseToUserCallback<T> (result, asynchRequestState, cipherKey, jsonPluggableLibrary);
            } 
        }
        #endregion

        #region "Encoding and Crypto"

        internal static string JsonEncodePublishMsg (object originalMessage, string cipherKey, IJsonPluggableLibrary jsonPluggableLibrary)
        {
            string message = jsonPluggableLibrary.SerializeToJsonString (originalMessage);

            if (cipherKey.Length > 0) {
                PubnubCrypto aes = new PubnubCrypto (cipherKey);
                string encryptMessage = aes.Encrypt (message);
                message = jsonPluggableLibrary.SerializeToJsonString (encryptMessage);
            }

            return message;
        }

        internal static object DecodeMessage (PubnubCrypto aes, object element, List<ChannelEntity> channelEntities,
            IJsonPluggableLibrary jsonPluggableLibrary, 
            PubnubErrorFilter.Level errorLevel)
        {
            string decryptMessage = "";
            try {
                decryptMessage = aes.Decrypt (element.ToString ());
            }
            catch (Exception ex) {
                decryptMessage = "**DECRYPT ERROR**";
                PubnubCallbacks.CallErrorCallback<object> (ex, channelEntities, PubnubErrorCode.None, 
                    PubnubErrorSeverity.Critical, errorLevel);
            }
            object decodeMessage = (decryptMessage == "**DECRYPT ERROR**") ? decryptMessage : jsonPluggableLibrary.DeserializeToObject (decryptMessage);
            return decodeMessage;
        }

        internal static List<object> DecryptCipheredMessage (List<object> message, List<ChannelEntity> channelEntities,
            string cipherKey, IJsonPluggableLibrary jsonPluggableLibrary, 
            PubnubErrorFilter.Level errorLevel)
        {
            List<object> returnMessage = new List<object> ();

            PubnubCrypto aes = new PubnubCrypto (cipherKey);
            var myObjectArray = (from item in message
                select item as object).ToArray ();
            IEnumerable enumerable = myObjectArray [0] as IEnumerable;

            if (enumerable != null) {
                List<object> receivedMsg = new List<object> ();
                foreach (object element in enumerable) {
                    receivedMsg.Add (DecodeMessage (aes, element, channelEntities, jsonPluggableLibrary, errorLevel));
                }
                returnMessage.Add (receivedMsg);
            }
            for (int index = 1; index < myObjectArray.Length; index++) {
                returnMessage.Add (myObjectArray [index]);
            }
            return returnMessage;
        }

        internal static List<object> DecryptNonCipheredMessage (List<object> message)
        {
            List<object> returnMessage = new List<object> ();
            var myObjectArray = (from item in message
                select item as object).ToArray ();
            IEnumerable enumerable = myObjectArray [0] as IEnumerable;
            if (enumerable != null) {
                List<object> receivedMessage = new List<object> ();
                foreach (object element in enumerable) {
                    receivedMessage.Add (element);
                }
                returnMessage.Add (receivedMessage);
            }
            for (int index = 1; index < myObjectArray.Length; index++) {
                returnMessage.Add (myObjectArray [index]);
            }
            return returnMessage;
        }

        internal static List<object> DecodeDecryptLoop (List<object> message, List<ChannelEntity> channelEntities, 
            string cipherKey, IJsonPluggableLibrary jsonPluggableLibrary, 
            PubnubErrorFilter.Level errorLevel)
        {
            if (cipherKey.Length > 0) {
                return DecryptCipheredMessage (message, channelEntities, cipherKey, jsonPluggableLibrary, errorLevel);
            } else {
                return DecryptNonCipheredMessage (message);
            }
        }    

        #endregion

        #region "Other Methods"

        internal static void CheckSubscribedChannelsAndSendCallbacks<T> (List<ChannelEntity> channelEntities,
            ResponseType type, int pubnubNetworkCheckRetries, PubnubErrorFilter.Level errorLevel){
            if (channelEntities != null && channelEntities.Count > 0) {
                foreach (ChannelEntity channelEntity in channelEntities) {
                    string message = string.Format ("Unsubscribed after {0} failed retries", pubnubNetworkCheckRetries);;
                    PubnubErrorCode pnErrorCode = PubnubErrorCode.UnsubscribedAfterMaxRetries;

                    if (channelEntity.ChannelID.IsPresenceChannel) {
                        message = string.Format ("Presence Unsubscribed after {0} failed retries", pubnubNetworkCheckRetries);
                        pnErrorCode = PubnubErrorCode.PresenceUnsubscribedAfterMaxRetries;
                    }

                    //TODO: Recheck
                    PubnubClientError error = Helpers.CreatePubnubClientError<T> (message, null, channelEntities, pnErrorCode,
                        PubnubErrorSeverity.Critical);  
                    
                    PubnubCallbacks.FireErrorCallback<T> (channelEntity,
                        errorLevel, error);

                    LoggingMethod.WriteToLog (string.Format ("DateTime {0}, CheckSubscribedChannelsAndSendCallbacks: {1} Subscribe JSON network error response={2}", 
                        DateTime.Now.ToString (), (channelEntity.ChannelID.IsPresenceChannel)?"Presence":"", message), LoggingMethod.LevelInfo);
                    
                }
            }
        }
            
        public static void WrapResultBasedOnResponseType<T> (RequestState<T> pubnubRequestState, string jsonString, 
            IJsonPluggableLibrary jsonPluggableLibrary, PubnubErrorFilter.Level errorLevel, string cipherKey, ref List<object> result)
        {
            try {
                string multiChannel = Helpers.GetNamesFromChannelEntities(pubnubRequestState.ChannelEntities, false); 
                string multiChannelGroup = Helpers.GetNamesFromChannelEntities(pubnubRequestState.ChannelEntities, true);
                if (!string.IsNullOrEmpty (jsonString)) {
                    LoggingMethod.WriteToLog (string.Format ("DateTime {0}, WrapResultBasedOnResponseType: jsonString = {1}", DateTime.Now.ToString (), jsonString), LoggingMethod.LevelInfo);
                    object deSerializedResult = jsonPluggableLibrary.DeserializeToObject (jsonString);
                    List<object> result1 = ((IEnumerable)deSerializedResult).Cast<object> ().ToList ();

                    if (result1 != null && result1.Count > 0) {
                        result = result1;
                    }

                    switch (pubnubRequestState.RespType) {
                    case ResponseType.DetailedHistory:
                        result = DecodeDecryptLoop (result, pubnubRequestState.ChannelEntities, cipherKey, jsonPluggableLibrary, errorLevel);
                        result.Add (multiChannel);
                        break;
                    case ResponseType.Time:
                        Int64[] c = deSerializedResult as Int64[];
                        if ((c != null) && (c.Length > 0)) {
                            result = new List<object> ();
                            result.Add (c [0]);
                        }
                        break;
                    case ResponseType.Leave:
                        if (!string.IsNullOrEmpty(multiChannelGroup))
                        {
                            result.Add(multiChannelGroup);
                        }
                        if (!string.IsNullOrEmpty(multiChannel))
                        {
                            result.Add(multiChannel);
                        }
                        break;
                    case ResponseType.SubscribeV2:
                    case ResponseType.PresenceV2:
                        break;
                    case ResponseType.Publish:
                    case ResponseType.PushRegister:
                    case ResponseType.PushRemove:
                    case ResponseType.PushGet:
                    case ResponseType.PushUnregister:
                        result.Add (multiChannel);
                        break;
                    case ResponseType.GrantAccess:
                    case ResponseType.AuditAccess:
                    case ResponseType.RevokeAccess:
                    case ResponseType.GetUserState:
                    case ResponseType.SetUserState:
                    case ResponseType.WhereNow:
                    case ResponseType.HereNow:
                        result = DeserializeAndAddToResult (jsonString, multiChannel, jsonPluggableLibrary, true);
                        break;
                    case ResponseType.GlobalHereNow:
                        result = DeserializeAndAddToResult (jsonString, multiChannel, jsonPluggableLibrary, false);
                        break;
                    case ResponseType.ChannelGroupAdd:
                    case ResponseType.ChannelGroupRemove:
                    case ResponseType.ChannelGroupGet:
                        result = DeserializeAndAddToResult (jsonString, multiChannel, jsonPluggableLibrary, false);

                        break;
                    case ResponseType.ChannelGroupGrantAccess:
                    case ResponseType.ChannelGroupAuditAccess:
                    case ResponseType.ChannelGroupRevokeAccess:
                        result = DeserializeAndAddToResult (jsonString, "", jsonPluggableLibrary, false);
                        result.Add(multiChannelGroup);
                        break;

                    default:
                        break;
                    }
                } 
                else {
                    LoggingMethod.WriteToLog (string.Format ("DateTime {0}, WrapResultBasedOnResponseType: json string null ", DateTime.Now.ToString ()), LoggingMethod.LevelInfo);
                }
            } catch (Exception ex) {
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, WrapResultBasedOnResponseType: exception: {1} ", DateTime.Now.ToString (), ex.ToString ()), LoggingMethod.LevelError);
                ProcessWrapResultBasedOnResponseTypeException<T> (pubnubRequestState, errorLevel, ex);
            }
        }

        internal static List<object> DeserializeAndAddToResult (string jsonString, string multiChannel, 
            IJsonPluggableLibrary jsonPluggableLibrary, bool addChannel)
        {
            IDictionary<string, object> dictionary = jsonPluggableLibrary.DeserializeToDictionaryOfObject (jsonString);
            List<object> result = new List<object> ();
            result.Add (dictionary);
            if (addChannel) {
                result.Add (multiChannel);
            }
            return result;
        }

        internal static void ProcessWrapResultBasedOnResponseTypeException<T> (RequestState<T> pubnubRequestState, 
            PubnubErrorFilter.Level errorLevel, Exception ex)
        {
            if (pubnubRequestState.ChannelEntities != null) {
                if(pubnubRequestState.RespType.Equals(ResponseType.SubscribeV2) || pubnubRequestState.RespType.Equals(ResponseType.PresenceV2)
                ) {
                    PubnubCallbacks.FireErrorCallbacksForAllChannels<T> (ex, pubnubRequestState, PubnubErrorSeverity.Critical, 
                        PubnubErrorCode.None, errorLevel);
                }
                else {
                    PubnubCallbacks.FireErrorCallbacksForAllChannels<T> (ex, pubnubRequestState, PubnubErrorSeverity.Critical, 
                        PubnubErrorCode.None, errorLevel);
                }
            }
        }

        internal static void CreatePNMessageResult(SubscribeMessage subscribeMessage, out PNMessageResult messageResult)
        {
            long timetoken = (subscribeMessage.PublishTimetokenMetadata != null) ? subscribeMessage.PublishTimetokenMetadata.Timetoken : 0;
            messageResult = new PNMessageResult (
                subscribeMessage.SubscriptionMatch, 
                subscribeMessage.Channel, 
                subscribeMessage.Payload, 
                timetoken,
                subscribeMessage.UserMetadata);
        }

        internal static void AddMessageToListV2(string cipherKey, IJsonPluggableLibrary jsonPluggableLibrary, 
            SubscribeMessage subscribeMessage, ChannelEntity ce, out List<object> itemMessage)
        {
            itemMessage = new List<object> ();
            if (ce.ChannelID.IsPresenceChannel)
            {
                itemMessage.Add(subscribeMessage.Payload);
            }
            else
            {
                //decrypt the subscriber message if cipherkey is available
                if (cipherKey.Length > 0)
                {
                    object decodeMessage;
                    try
                    {
                        PubnubCrypto aes = new PubnubCrypto(cipherKey);
                        string decryptMessage = aes.Decrypt(subscribeMessage.Payload.ToString());
                        decodeMessage = (decryptMessage == "**DECRYPT ERROR**") ? decryptMessage : jsonPluggableLibrary.DeserializeToObject(decryptMessage);
                    }
                    catch (Exception decryptEx)
                    {
                        decodeMessage = subscribeMessage.Payload;
                        LoggingMethod.WriteToLog(string.Format("DateTime {0}, AddMessageToListV2: decodeMessage Exception: {1}", DateTime.Now.ToString(), decryptEx.ToString()), LoggingMethod.LevelError);
                        
                    }
                    itemMessage.Add(decodeMessage);
                }
                else
                {
                    itemMessage.Add(subscribeMessage.Payload);
                }
            }
            itemMessage.Add((subscribeMessage.PublishTimetokenMetadata != null) ? subscribeMessage.PublishTimetokenMetadata.Timetoken.ToString() : "");
            if (ce.ChannelID.IsChannelGroup) {
                itemMessage.Add (ce.ChannelID.ChannelOrChannelGroupName.Replace(Utility.PresenceChannelSuffix, ""));
            }

            itemMessage.Add(subscribeMessage.Channel.Replace(Utility.PresenceChannelSuffix, ""));
        }

        internal static void FindChannelEntityAndCallback<T> (SubscribeMessage subscribeMessage, List<ChannelEntity> channelEntities,
            string cipherKey, IJsonPluggableLibrary jsonPluggableLibrary, ChannelIdentity ci){
            ChannelEntity ce = channelEntities.Find(x => x.ChannelID.Equals(ci));
            if (ce != null) {
                LoggingMethod.WriteToLog(string.Format("DateTime {0}, FindChannelEntityAndCallback: \n ChannelEntity : {1}  cg?={2} ispres?={3}", DateTime.Now.ToString(),
                    ce.ChannelID.ChannelOrChannelGroupName, ce.ChannelID.IsChannelGroup.ToString(),
                    ce.ChannelID.IsPresenceChannel.ToString()
                ), LoggingMethod.LevelInfo);
                
                List<object> itemMessage;
                AddMessageToListV2(cipherKey, jsonPluggableLibrary, subscribeMessage, ce, out itemMessage);
                PubnubChannelCallback<T> channelCallbacks = ce.ChannelParams.Callbacks as PubnubChannelCallback<T>;

                if ((subscribeMessage.SubscriptionMatch.Contains (".*")) && Utility.IsPresenceChannel(subscribeMessage.Channel)) {
                    LoggingMethod.WriteToLog(string.Format("DateTime {0}, FindChannelEntityAndCallback: Wildcard match ChannelEntity : {1} ", DateTime.Now.ToString(),
                        ce.ChannelID.ChannelOrChannelGroupName
                    ), LoggingMethod.LevelInfo);
                    
                    PubnubCallbacks.GoToCallback<T> (itemMessage, channelCallbacks.WildcardPresenceCallback, jsonPluggableLibrary);
                } else {
                    PubnubCallbacks.GoToCallback<T> (itemMessage, channelCallbacks.SuccessCallback, jsonPluggableLibrary);
                }
            }
            #if (ENABLE_PUBNUB_LOGGING)
            else {
                StringBuilder sbChannelEntity = new StringBuilder();
                foreach( ChannelEntity channelEntity in channelEntities){
                    sbChannelEntity.AppendFormat("ChannelEntity : {0} cg?={1} ispres?={2}", channelEntity.ChannelID.ChannelOrChannelGroupName,
                        channelEntity.ChannelID.IsChannelGroup.ToString(),
                        channelEntity.ChannelID.IsPresenceChannel.ToString()
                    );
                }
                LoggingMethod.WriteToLog(string.Format("DateTime {0}, FindChannelEntityAndCallback: ChannelEntity : null ci.name {1} \nChannelEntities: \n {2}", DateTime.Now.ToString(),
                    ci.ChannelOrChannelGroupName, sbChannelEntity.ToString()), LoggingMethod.LevelInfo);
            }
            #endif

        }

        internal static void ResponseToUserCallbackForSubscribeV2<T> (List<SubscribeMessage> subscribeMessages, List<ChannelEntity> channelEntities,
            string cipherKey, IJsonPluggableLibrary jsonPluggableLibrary)
        {
            LoggingMethod.WriteToLog(string.Format("DateTime {0}, In ResponseToUserCallbackForSubscribeV2", DateTime.Now.ToString()
                ), LoggingMethod.LevelInfo);
             
            foreach (SubscribeMessage subscribeMessage in subscribeMessages){
                LoggingMethod.WriteToLog (string.Format ("DateTime {0}, ResponseToUserCallbackForSubscribeV2:\n SubscribeMessage:\n" +
                    "shard : {1},\n" +
                    "subscriptionMatch: {2},\n" +
                    "channel: {3},\n" +
                    "payload: {4},\n" +
                    "flags: {5},\n" +
                    "issuingClientId: {6},\n" +
                    "subscribeKey: {7},\n" +
                    "sequenceNumber: {8},\n" +
                    "originatingTimetoken tt: {9},\n" +
                    "originatingTimetoken region: {10},\n" +
                    "publishMetadata tt: {11},\n" +
                    "publishMetadata region: {12},\n" +
                    "userMetadata {13} \n",
                    DateTime.Now.ToString (), 
                    subscribeMessage.Shard,
                    subscribeMessage.SubscriptionMatch,
                    subscribeMessage.Channel,
                    subscribeMessage.Payload.ToString(),
                    subscribeMessage.Flags,
                    subscribeMessage.IssuingClientId,
                    subscribeMessage.SubscribeKey,
                    subscribeMessage.SequenceNumber,
                    (subscribeMessage.OriginatingTimetoken != null) ? subscribeMessage.OriginatingTimetoken.Timetoken.ToString() : "",
                    (subscribeMessage.OriginatingTimetoken != null) ? subscribeMessage.OriginatingTimetoken.Region : "",
                    (subscribeMessage.PublishTimetokenMetadata != null) ? subscribeMessage.PublishTimetokenMetadata.Timetoken.ToString() : "",
                    (subscribeMessage.PublishTimetokenMetadata  != null) ? subscribeMessage.PublishTimetokenMetadata.Region : "",
                    (subscribeMessage.UserMetadata != null) ? subscribeMessage.UserMetadata.ToString() : "null"),
                    LoggingMethod.LevelInfo);
                
                bool isPresenceChannel = Utility.IsPresenceChannel(subscribeMessage.Channel);
                if (string.IsNullOrEmpty(subscribeMessage.SubscriptionMatch) || subscribeMessage.Channel.Equals (subscribeMessage.SubscriptionMatch)) {
                    //channel

                    ChannelIdentity ci = new ChannelIdentity (subscribeMessage.Channel, false, isPresenceChannel);
                    FindChannelEntityAndCallback<T> (subscribeMessage, channelEntities, cipherKey, jsonPluggableLibrary, ci);
                } else if (subscribeMessage.SubscriptionMatch.Contains (".*") && isPresenceChannel){
                    //wildcard presence channel

                    ChannelIdentity ci = new ChannelIdentity (subscribeMessage.SubscriptionMatch, false, false);
                    FindChannelEntityAndCallback<T> (subscribeMessage, channelEntities, cipherKey, jsonPluggableLibrary, ci);
                } else if (subscribeMessage.SubscriptionMatch.Contains (".*")){
                    //wildcard channel
                    ChannelIdentity ci = new ChannelIdentity (subscribeMessage.SubscriptionMatch, false, isPresenceChannel);
                    FindChannelEntityAndCallback<T> (subscribeMessage, channelEntities, cipherKey, jsonPluggableLibrary, ci);

                } else {
                    ChannelIdentity ci = new ChannelIdentity(subscribeMessage.SubscriptionMatch, true, isPresenceChannel);
                    FindChannelEntityAndCallback<T> (subscribeMessage, channelEntities, cipherKey, jsonPluggableLibrary, ci);

                    //ce will be the cg and subscriptionMatch will have the cg name
                }

            }
        }
            
        internal static void ResponseToUserCallback<T> (List<object> result, RequestState<T> asynchRequestState, string cipherKey, 
            IJsonPluggableLibrary jsonPluggableLibrary)
        {
            switch (asynchRequestState.RespType) {
                case ResponseType.Leave:
                //No response to callback
                    break;
                case ResponseType.Publish:
                case ResponseType.DetailedHistory:
                case ResponseType.HereNow:
                case ResponseType.GlobalHereNow:
                case ResponseType.WhereNow:
                case ResponseType.GrantAccess:
                case ResponseType.AuditAccess:
                case ResponseType.RevokeAccess:
                case ResponseType.GetUserState:
                case ResponseType.SetUserState:
                case ResponseType.Time:
                case ResponseType.PushRegister:
                case ResponseType.PushRemove:
                case ResponseType.PushGet:
                case ResponseType.PushUnregister:
                case ResponseType.ChannelGroupAdd:
                case ResponseType.ChannelGroupRemove:
                case ResponseType.ChannelGroupGet:
                case ResponseType.ChannelGroupGrantAccess:
                case ResponseType.ChannelGroupAuditAccess:
                case ResponseType.ChannelGroupRevokeAccess:
                
                    PubnubCallbacks.SendCallbacks<T>(jsonPluggableLibrary, asynchRequestState, result, CallbackType.Success, false);
                    break;
                default:
                    break;
            }
        }

        internal static void ResponseToConnectCallback<T> (RequestState<T> asynchRequestState,
             IJsonPluggableLibrary jsonPluggableLibrary)
        {
            //Check callback exists and make sure previous timetoken = 0
            if (asynchRequestState.ChannelEntities != null) {
                bool updateIsAwaitingConnectCallback = false;
                for(int i=0; i< asynchRequestState.ChannelEntities.Count; i++){
                    ChannelEntity channelEntity = asynchRequestState.ChannelEntities[i];
                    LoggingMethod.WriteToLog (string.Format ("DateTime {0}, Check ResponseToConnectCallback {1}", 
                        DateTime.Now.ToString (), channelEntity.ChannelID.ChannelOrChannelGroupName), LoggingMethod.LevelInfo);
                    
                    if (channelEntity.ChannelParams.IsAwaitingConnectCallback) {
                        updateIsAwaitingConnectCallback = true;
                        LoggingMethod.WriteToLog (string.Format ("DateTime {0}, ResponseToConnectCallback IsAwaitingConnectCallback {1}", 
                            DateTime.Now.ToString (), channelEntity.ChannelID.ChannelOrChannelGroupName), LoggingMethod.LevelInfo);
                    
                        switch (asynchRequestState.RespType) {
                        case ResponseType.SubscribeV2:
                            var connectResult = Helpers.CreateJsonResponse ("Connected", channelEntity.ChannelID.ChannelOrChannelGroupName, jsonPluggableLibrary);
                            PubnubCallbacks.SendCallbacks<T> (jsonPluggableLibrary, channelEntity, connectResult, 
                                CallbackType.Connect, false);

                            break;
                        case ResponseType.PresenceV2:
                            var connectResult2 = Helpers.CreateJsonResponse ("Presence Connected", 
                                                 channelEntity.ChannelID.ChannelOrChannelGroupName.Replace (Utility.PresenceChannelSuffix, ""), jsonPluggableLibrary);
                            PubnubCallbacks.SendCallbacks<T> (jsonPluggableLibrary, channelEntity, connectResult2, 
                                CallbackType.Connect, false);
                            break;
                        default:
                            break;
                        }
                    }
                }
                if (updateIsAwaitingConnectCallback) {
                    Subscription.Instance.UpdateIsAwaitingConnectCallbacksOfEntity (asynchRequestState.ChannelEntities, false);
                }
            }

        }

        internal static List<object> CreateJsonResponse(string message, string channel, IJsonPluggableLibrary jsonPluggableLibrary){
            string jsonString = "";
            List<object> connectResult = new List<object> ();
            jsonString = string.Format ("[1, \"{0}\"]", message);
            connectResult = jsonPluggableLibrary.DeserializeToListOfObject (jsonString);
            connectResult.Add (channel);

            return connectResult;
        }

        internal static PubnubErrorCode GetTimeOutErrorCode (ResponseType responseType)
        {
            switch(responseType){
            case ResponseType.AuditAccess:
            case ResponseType.GrantAccess:
            case ResponseType.RevokeAccess:
            case ResponseType.ChannelGroupGrantAccess:
            case ResponseType.ChannelGroupAuditAccess:
            case ResponseType.ChannelGroupRevokeAccess:
                return PubnubErrorCode.PAMAccessOperationTimeout;
            case ResponseType.DetailedHistory:
            case ResponseType.History:
                return PubnubErrorCode.DetailedHistoryOperationTimeout;
            case ResponseType.GetUserState:
                return PubnubErrorCode.GetUserStateTimeout;
            case ResponseType.GlobalHereNow:
                return PubnubErrorCode.GlobalHereNowOperationTimeout;
            case ResponseType.SetUserState:
                return PubnubErrorCode.SetUserStateTimeout;
            case ResponseType.HereNow:
                return PubnubErrorCode.HereNowOperationTimeout;
            case ResponseType.Publish:
                return PubnubErrorCode.PublishOperationTimeout;
            case ResponseType.Time:
                return PubnubErrorCode.TimeOperationTimeout;
            case ResponseType.WhereNow:
                return PubnubErrorCode.WhereNowOperationTimeout;
            default:
                /* 
                 * ResponseType.Presence:
                 * ResponseType.PresenceUnsubscribe:
                 * ResponseType.Leave:
                 * ResponseType.Subscribe:
                 * ResponseType.Unsubscribe:
                 * ResponseType.Heartbeat:
                 * ResponseType.PresenceHeartbeat:
                 * ResponseType.ChannelGroupAdd
                 * ResponseType.ChannelGroupRemove
                 * ResponseType.ChannelGroupGet
                 */
                return PubnubErrorCode.OperationTimeout;
            }
        }

        internal static PubnubClientError CreatePubnubClientError<T>(Exception ex, 
            RequestState<T> requestState, List<ChannelEntity> channelEntities, PubnubErrorCode errorCode, PubnubErrorSeverity severity){

            if (errorCode.Equals (PubnubErrorCode.None)) {
                errorCode = PubnubErrorCodeHelper.GetErrorType (ex);
            }

            int statusCode = (int)errorCode;
            string errorDescription = PubnubErrorCodeDescription.GetStatusCodeDescription (errorCode);
            PubnubWebRequest request = null;
            PubnubWebResponse response = null;

            if (requestState != null) {
                request = requestState.Request;
                response = requestState.Response;
                if (channelEntities == null && requestState.ChannelEntities != null) {
                    channelEntities = requestState.ChannelEntities;
                }
            }

            PubnubClientError error = new PubnubClientError (statusCode, severity, true, ex.Message, ex, 
                PubnubMessageSource.Client, request, response, errorDescription, 
                channelEntities);

            LoggingMethod.WriteToLog (string.Format ("DateTime {0}, PubnubClientError = {1}", DateTime.Now.ToString (), error.ToString ()), LoggingMethod.LevelInfo);
            return error;
        }

        internal static PubnubClientError CreatePubnubClientError<T>(string message, 
            RequestState<T> requestState, PubnubErrorCode errorCode, PubnubErrorSeverity severity, string channels, string channelGroups){

            int statusCode = (int)errorCode;
            string errorDescription = PubnubErrorCodeDescription.GetStatusCodeDescription (errorCode);

            PubnubWebRequest request = null;
            PubnubWebResponse response = null;

            if (requestState != null) {
                request = requestState.Request;
                response = requestState.Response;
            }

            PubnubClientError error = new PubnubClientError (statusCode, severity, message, PubnubMessageSource.Client, 
                request, response, errorDescription, 
                channels, channelGroups
            );

            LoggingMethod.WriteToLog (string.Format ("DateTime {0}, PubnubClientError 2 = {1}", DateTime.Now.ToString (), error.ToString ()), LoggingMethod.LevelInfo);
            return error;
        }

        internal static PubnubClientError CreatePubnubClientError<T>(Exception ex, 
            RequestState<T> requestState, PubnubErrorCode errorCode, PubnubErrorSeverity severity, string channels, string channelGroups){

            int statusCode = (int)errorCode;
            string errorDescription = PubnubErrorCodeDescription.GetStatusCodeDescription (errorCode);

            PubnubWebRequest request = null;
            PubnubWebResponse response = null;

            if (requestState != null) {
                request = requestState.Request;
                response = requestState.Response;
            }

            PubnubClientError error = new PubnubClientError (statusCode, severity, ex.Message, PubnubMessageSource.Client, 
                request, response, errorDescription, 
                channels, channelGroups
            );

            LoggingMethod.WriteToLog (string.Format ("DateTime {0}, PubnubClientError 3 = {1}", DateTime.Now.ToString (), error.ToString ()), LoggingMethod.LevelInfo);
            return error;
        }

        internal static PubnubClientError CreatePubnubClientError<T>(string message, 
            RequestState<T> requestState, List<ChannelEntity> channelEntities, PubnubErrorCode errorCode, PubnubErrorSeverity severity){

            int statusCode = (int)errorCode;
            string errorDescription = PubnubErrorCodeDescription.GetStatusCodeDescription (errorCode);

            PubnubWebRequest request = null;
            PubnubWebResponse response = null;

            if (requestState != null) {
                request = requestState.Request;
                response = requestState.Response;
            }

            PubnubClientError error = new PubnubClientError (statusCode, severity, message, PubnubMessageSource.Client, 
                request, response, errorDescription, 
                channelEntities
            );

            LoggingMethod.WriteToLog (string.Format ("DateTime {0}, PubnubClientError 4 = {1}", DateTime.Now.ToString (), error.ToString ()), LoggingMethod.LevelInfo);
            return error;
        }

        internal static PubnubClientError CreatePubnubClientError<T>(WebException ex, 
            RequestState<T> requestState, string channel, PubnubErrorSeverity severity){

            PubnubErrorCode errorCode = PubnubErrorCodeHelper.GetErrorType (ex.Status, ex.Message);
            return CreatePubnubClientError<T> (ex, requestState, null,  errorCode, severity);
        }

        #endregion

    }
}
