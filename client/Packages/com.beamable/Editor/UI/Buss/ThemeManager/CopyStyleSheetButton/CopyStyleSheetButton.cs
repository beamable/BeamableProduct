using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public class CopyStyleSheetButton : BeamableBasicVisualElement
	{
		private VisualElement _copyStyleSheetButton;
		private BussStyleListVisualElement _styleList;

		public CopyStyleSheetButton() :
			base($"{BUSS_THEME_MANAGER_PATH}/CopyStyleSheetButton/CopyStyleSheetButton.uss") { }

		public void Setup(BussStyleListVisualElement styleList)
		{
			_styleList = styleList;

			Init();
		}

		public override void Init()
		{
			base.Init();

			_copyStyleSheetButton = new VisualElement {name = "copyStyleSheetButton"};
			_copyStyleSheetButton.AddToClassList("button");
			_copyStyleSheetButton.Add(new Label(DUPLICATE_STYLESHEET_BUTTON_LABEL));

			_copyStyleSheetButton.UnregisterCallback<MouseDownEvent>(OnClick);
			_copyStyleSheetButton.RegisterCallback<MouseDownEvent>(OnClick);

			_copyStyleSheetButton.SetInactive(false);

			Root.Add(_copyStyleSheetButton);
		}

		private void OnClick(MouseDownEvent evt = null)
		{
			List<BussStyleSheet> readonlyStyles =
				_styleList.StyleSheets.Where(styleSheet => styleSheet.IsReadOnly).ToList();
			OpenMenu(readonlyStyles);
		}

		private void OpenMenu(IEnumerable<BussStyleSheet> bussStyleSheets)
		{
			GenericMenu context = new GenericMenu();
			context.AddItem(new GUIContent(DUPLICATE_STYLESHEET_OPTIONS_HEADER), false, () => { });
			context.AddSeparator(string.Empty);
			foreach (BussStyleSheet styleSheet in bussStyleSheets)
			{
				context.AddItem(new GUIContent(styleSheet.name), false, () =>
				{
					NewStyleSheetWindow window = NewStyleSheetWindow.ShowWindow();
					if (window != null)
					{
						window.Init(styleSheet.Styles);
					}
				});
			}

			context.ShowAsContext();
		}
	}
}
