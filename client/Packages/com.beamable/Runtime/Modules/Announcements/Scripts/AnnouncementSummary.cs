using TMPro;
using UnityEngine;

namespace Beamable.Announcements
{
   public class AnnouncementSummary : MonoBehaviour
   {
#pragma warning disable CS0649
      [SerializeField] private TextMeshProUGUI txtTitle;
      [SerializeField] private TextMeshProUGUI txtBody;
#pragma warning restore CS0649

      public void Setup(string title, string body)
      {
         txtTitle.text = title;
         txtBody.text = body;
      }
   }
}