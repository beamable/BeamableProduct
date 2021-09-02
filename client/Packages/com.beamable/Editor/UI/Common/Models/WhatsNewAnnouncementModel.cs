using System;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.UI.Buss;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
namespace Beamable.Editor.Toolbox.Models
{
    public class WhatsNewAnnouncementModel : AnnouncementModelBase
    {
        public string IgnoreActionText = "Ignore";
        public string WhatsNewActionText = "What's new?";
        
        public Action OnIgnore;
        public Action OnWhatsNew;
        
        public override BeamableVisualElement CreateVisualElement()
        {
            return new WhatsNewAnnouncementVisualElement()
            {
                WhatsNewAnnouncementModel = this
            };
        }
    }
}