using Beamable.Common.Dependencies;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.UI;
using Beamable.Editor.UI.Components;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Beamable.Editor.Tests.Toolbox
{
	public class ToolboxTextboxTests : EditorTest
	{
		protected override void Configure(IDependencyBuilder builder)
		{
			builder.ReplaceSingleton<IToolboxViewService, MockToolboxViewService>();
		}

	}
}
