using Beamable.Editor.BeamableAssistant.Components;
using Beamable.Editor.BeamableAssistant.Models;
using Beamable.Editor.Content.Models;
using Beamable.Editor.Content;
using UnityEngine;
using Beamable.Editor.UI.Buss.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.UI.Components;
using Common.Runtime.BeamHints;
using Modules.Content;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.BeamableAssistant.Components
{

   public class ActionBarVisualElement : BeamableAssistantComponent
   {
      public new class UxmlFactory : UxmlFactory<ActionBarVisualElement, UxmlTraits>
      {
      }

      public new class UxmlTraits : VisualElement.UxmlTraits
      {
         UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
            {name = "custom-text", defaultValue = "nada"};

         public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
         {
            get { yield break; }
         }

         public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
         {
            base.Init(ve, bag, cc);
            var self = ve as ActionBarVisualElement;
         }
      }
      
      public BeamHintsDataModel Model { get; internal set; }

      private SearchBarVisualElement _searchBar;
      private Button _createNewButton, _validateButton, _publishButton, _publishDropdownButton, _downloadButton;
      private Button _tagButton, _typeButton, _statusButton, _refreshButton, _docsButton;
      private bool _mouseOverPublishDropdown;

      public ActionBarVisualElement() : base(nameof(ActionBarVisualElement))
      {
      }

      public override void Refresh()
      {
         base.Refresh();

         //Buttons (Left To Right in UX)

         _searchBar = Root.Q<SearchBarVisualElement>();
         // Model.OnQueryUpdated += (query, force) =>
         // {
         //    var existing = force
         //       ? null
         //       : _searchBar.Value;
         //
         //    var filterString = query?.ToString(existing) ?? "";
         //    _searchBar.SetValueWithoutNotify(filterString);
         // };
         _searchBar.OnSearchChanged += SearchBar_OnSearchChanged;
      }

      private void SearchBar_OnSearchChanged(string obj)
      {
         var query = EditorContentQuery.Parse(obj);
         // Model.SetFilter(query);
      }
   }
}
