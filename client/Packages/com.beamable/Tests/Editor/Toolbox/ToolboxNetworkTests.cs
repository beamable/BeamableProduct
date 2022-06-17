using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Beamable.Common.Dependencies;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.UI;
using Beamable.Editor.UI.Components;
using Beamable.Platform.Tests;
using Beamable.Editor.Realms;

#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Tests
{
    public class ToolboxNetworkTests
    {
		private IDependencyProviderScope provider;

		// A Test behaves as an ordinary method
		[Test]
        public void TestNetwork()
        {
			var requester = new MockPlatformAPI();

			//BeamEditor line 234
			//RealmsServices constructor
			//mock network
			//requester.MockRequest();
			//
			IDependencyBuilder builder = new DependencyBuilder();
			builder.AddSingleton<Beamable.Api.IPlatformRequester, MockPlatformAPI>(requester);
			builder.AddSingleton<RealmsService>();

			provider = builder.Build();

			var realmsService = provider.GetService<RealmsService>();
			
			realmsService.GetCustomerData();

			/*requester
				.MockRequest<GetCustomerResponseDTO>(Beamable.Common.Api.Method.GET)
				.WithResponse(new GetCustomerResponseDTO){
				
			}*/
		}
    }
}
