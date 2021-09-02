using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.UI.Buss.Wizard.Components;
using Beamable.Editor.UI.Buss.Wizard.Model;
using Beamable.Editor.UI.Buss.Components;
using Beamable.UI.Buss;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss.Wizard
{

   public class WizardContext
   {
      private readonly Button _nextButton;
      private readonly Action _onSetEnabled;

      public WizardContext(Button nextButton, Action onSetEnabled)
      {
         _nextButton = nextButton;
         _onSetEnabled = onSetEnabled;
      }

      public void SetNextEnabled(bool enabled)
      {
         _nextButton.SetEnabled(enabled);
         _onSetEnabled?.Invoke();
      }
   }

   public class BussWizardWindow : EditorWindow
   {
// Not fit for end users - chrisa
#if BEAMABLE_DEVELOPER
      [MenuItem(
         BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_THEME_MANAGER + "/" +
         BeamableConstants.OPEN + " " +
         BeamableConstants.BUSS_WIZARD,
         priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_3)]
   #endif
      public static BussWizardWindow Init()
      {
         var inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");

         var wnd = CreateInstance<BussWizardWindow>();
         wnd.titleContent = new GUIContent("Beamable Theme Wizard");
         //var wnd = GetWindow<BussWizardWindow>(ContentConstants.BUSS_WIZARD, true);
         wnd.ShowUtility();
         return wnd;
      }


      private const string BUSS_WIZARD_PACKAGE_PATH = "Packages/com.beamable/Editor/UI/Buss/Wizard";

      private VisualElement _windowRoot;
      private VisualElement _contentRoot;

      private WizardModel _model;
      private List<WizardStepVisualElement> _steps;
      private int _currentStep = 0;
      private WizardStepVisualElement _currentStepElement => _steps[_currentStep];

      private List<Label> _tocElements;
      private VisualElement _tocElement, _stepContainerElement;
      private Button _nextButton, _prevButton;


      public void OnEnable()
      {
         VisualElement root = this.GetRootVisualContainer();
         var uiAsset =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{BUSS_WIZARD_PACKAGE_PATH}/bussWizardWindow.uxml");
         _windowRoot = uiAsset.CloneTree();
         _windowRoot.AddStyleSheet($"{BUSS_WIZARD_PACKAGE_PATH}/bussWizardWindow.uss");
         _windowRoot.name = nameof(_windowRoot);

         root.Add(_windowRoot);

         _tocElement = root.Q<VisualElement>("toc");
         _stepContainerElement = root.Q<VisualElement>("step");

         _nextButton = root.Q<Button>("btn-next");
//         _prevButton = root.Q<Button>("btn-back");

         _nextButton.clickable.clicked += SelectNextStep;
//         _prevButton.clickable.clicked += SelectPrevStep;

         /*
          * The wizard needs to produce a style sheet, and automatically assign it to the BussConfiguration as a fallback.
          * It needs
          */
         _model = new WizardModel();
         _steps = CreateStepElements();

         var ctx = BuildContext();
         _tocElements = new List<Label>();
         for (var i = 0 ; i < _steps.Count; i ++)
         {
            var index = i;
            var step = _steps[i];
            step.Refresh(ctx);

            // TODO build out a ToC of steps.
            // and display them in a big giant list.

            var tocLabel = new Label($"{(i+1)}. {step.StepName}");
            tocLabel.RegisterCallback<MouseUpEvent>(evt =>
            {
               if (!step.Enabled) return;
               SelectStep(index);
            });
            _tocElements.Add(tocLabel);
            _tocElement.Add(tocLabel);
         }
         SelectStep(0);
      }

      List<WizardStepVisualElement> CreateStepElements()
      {
         return new List<WizardStepVisualElement>
         {
            new StyleSheetStep(_model),
            new ColorStep( _model.PrimaryColor,
               "Primary Color",
               "What is the main color of your game?",
               "Every Beamable Module will use this color as the primary color. You should pick a color that matches the genre and feel of your game.",
               _model),
            new ColorStep( _model.SecondaryColor,
               "Secondary Color",
               "What color compliments your game?",
               "This color will be used as the secondary color for all Beamable Modules. You should pick a value that looks good with the primary color",
               _model),
         };
      }

      void SelectStep(int index)
      {
         if (index == _steps.Count - 1)
         {
            _nextButton.text = "Complete Theme";
         }
         else
         {
            _nextButton.text = "Next";
         }

         _tocElements[_currentStep].RemoveFromClassList("active");
         _currentStep = index;
         _tocElements[_currentStep].AddToClassList("active");

         _stepContainerElement.Clear();

         _stepContainerElement.Add(_currentStepElement);

         var ctx = BuildContext();
         SetTocEnables();
         _currentStepElement.Activate(ctx);

      }

      void SetTocEnables()
      {
         for (var i = 0 ; i < _tocElements.Count; i ++)
         {
            _tocElements[i].SetEnabled( _steps[i].Enabled );
         }
      }

      void SelectNextStep()
      {
         if (_currentStep == _steps.Count - 1)
         {
            Complete();
         }
         else
         {
            SelectStep(_currentStep + 1);
         }
      }

      WizardContext BuildContext()
      {
         return new WizardContext(_nextButton, () =>
         {
            SetTocEnables();
         });
      }

      void Complete()
      {

         _model.Complete();

         Close();
      }

   }
}