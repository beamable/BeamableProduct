// using System.Collections;
// using Beamable.Common.Api.Announcements;
// using Beamable.Common.Api.Notifications;
// using Beamable.Common.Player;
// using Beamable.Player;
// using Beamable.Tests.Runtime.Player.Notifications;
// using NUnit.Framework;
// using UnityEngine.TestTools;
//
// namespace Beamable.Tests.Runtime.Player.Announcements
// {
//    public class RefreshTests : BeamableTest
//    {
//       private IAnnouncementsApi Api;
//       private INotificationService Notifications;
//       private ISdkEventService SdkEventService;
//
//       [UnityTest]
//       public IEnumerator Test()
//       {
//
//          var announcements = new PlayerAnnouncements(MockPlatform, Api, Notifications, SdkEventService);
//
//          yield return announcements.Refresh().ToYielder();
//       }
//
//       protected override void OnSetupBeamable()
//       {
//          base.OnSetupBeamable();
//
//          Api = new MockAnnouncementsApi();
//          Notifications = new MockNotificationService();
//          SdkEventService = new SdkEventService();
//       }
//    }
// }
