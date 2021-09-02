using System;
using Beamable.Editor.UI.Buss.Model;
using Beamable.UI.Buss;
using Beamable.Editor.UI.Buss.Extensions;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;

#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss.Components
{
  public class PropertySearchVisualElement : BeamableVisualElement
  {
    public StyleObject StyleObject { get; }
    public const string COMMON = BeamableComponentsConstants.UI_PACKAGE_PATH + "/Buss/Components/propertySearchVisualElement";

    public Action<OptionalPropertyFieldWrapper> OnSelectedProperty = x => { };

    private ScrollView _propertyContainer;
    private TextField _searchField;
    private string filter = "";

    public PropertySearchVisualElement(StyleObject styleObject) : base(COMMON)
    {
      StyleObject = styleObject;
    }

    public override void Refresh()
    {
      base.Refresh();

      _searchField = Root.Q<TextField>();
      _propertyContainer = Root.Q<ScrollView>("property-container");

      _searchField.RegisterValueChangedCallback(evt =>
      {
        filter = evt.newValue;
        RefreshList();
      });
      _searchField.BeamableFocus();

      RefreshList();
      // need to iteratoe over all possible buss properties

    }


    void RefreshList()
    {
      _propertyContainer.Clear();
      foreach (var prop in StyleObject.GetUnusedProperties())
      {
        var propName = prop.GetName();
        var passesFilter = propName.ToLower().Contains(filter.Trim().ToLower());
        if (!passesFilter) continue;

        var button = new Button(() =>
        {
          OnSelectedProperty?.Invoke(prop);
        });
        button.text = prop.GetName();
        _propertyContainer.Add(button);
      }
    }


  }
}