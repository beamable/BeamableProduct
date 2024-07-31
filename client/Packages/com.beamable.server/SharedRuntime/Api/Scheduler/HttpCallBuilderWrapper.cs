// this file was copied from nuget package Beamable.Server.Common@0.0.0-PREVIEW.NIGHTLY-202407221259
// https://www.nuget.org/packages/Beamable.Server.Common/0.0.0-PREVIEW.NIGHTLY-202407221259

using Beamable.Common.Api;
using Beamable.Common.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Server
{

	public class HttpCallBuilderWrapper
	{
		private readonly SchedulerBuilder _schedulerBuilder;
		private readonly Action<HttpAction> _setAction;

		private HttpCallBuilder _builder;

		public HttpCallBuilderWrapper(SchedulerBuilder schedulerBuilder, Action<HttpAction> setAction)
		{
			_schedulerBuilder = schedulerBuilder;
			_setAction = setAction;
			_builder = new HttpCallBuilder();
		}



		public ISchedulerBuilderTrigger Run<T>(Method method,
											   string uri,
											   T body,
											   Dictionary<string, string> headers = null)
		{
			var action = _builder.Run(method, uri, body, headers);
			_setAction(action);
			return _schedulerBuilder;
		}

		public ISchedulerBuilderTrigger Run(Method method,
											string uri,
											string contentType = "application/json",
											Dictionary<string, string> headers = null)
		{
			var action = _builder.Run(method, uri, contentType, headers);
			_setAction(action);
			return _schedulerBuilder;
		}

		public ISchedulerBuilderTrigger Run(Method method,
											string uri,
											string body,
											string contentType = "application/json",
											Dictionary<string, string> headers = null)
		{
			var action = _builder.Run(method, uri, body, contentType, headers);
			_setAction(action);
			return _schedulerBuilder;
		}


	}
}
