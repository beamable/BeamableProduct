using System.Collections.Generic;
using Beamable.Common;

namespace Beamable.Experimental.Api.Chat
{
   using Promise = Promise<Unit>;

   public class PubNubChatProvider : ChatProvider
   {
      public override Promise<Room> CreatePrivateRoom(List<long> gamerTags)
      {
         var gamerTagsPlusMe = new List<long> {Platform.User.id};
         gamerTagsPlusMe.AddRange(gamerTags);

         var roomName = CreateRoomNameFromGamerTags(gamerTagsPlusMe);
         return Platform.Chat.CreateRoom(roomName, true, gamerTagsPlusMe).Map<Room>(roomInfo =>
         {
            var room = new PubNubRoom(roomInfo);
            AddRoom(room);
            return room;
         });
      }

      protected override Promise Connect()
      {
         // The game will already be connected to PubNub by the time that this chat provider is initialized.
         return Promise.Successful(Promise.Unit);
      }

      protected override Promise<List<Room>> FetchMyRooms()
      {
         return Platform.Chat.GetMyRooms()
            .Map(roomInfos =>
            {
               var rooms = new List<Room>();
               foreach (var roomInfo in roomInfos)
               {
                  rooms.Add(new PubNubRoom(roomInfo));
               }

               return rooms;
            });
      }
   }
}