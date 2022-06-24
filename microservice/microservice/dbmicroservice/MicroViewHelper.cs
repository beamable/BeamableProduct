using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Beamable.Server.Editor;

namespace Beamable.Server;

public static class MicroViewHelper
{
	public static List<ServiceMethod> BuildViewRoutes(MicroserviceAttribute microserviceAttribute, Type microserviceType, MicroViewManifest manifest)
	{
		var routes = manifest.Views.Select(view =>
		{
			var callableMethod = view.ViewDescriptor.Type.GetMethod(nameof(MicroView.GetHtml), BindingFlags.Instance | BindingFlags.Public);

			var method = new ServiceMethod
			{
				ShowInSwagger = false,
				Path = view.Path,

				// TODO: we are leaving some functionality on the table by not being able to create this via DI; the view _could_ be taking advantage of microservice functions
				InstanceFactory = _ => Activator.CreateInstance(view.ViewDescriptor.Type),

				// TODO: what scopes should this require? Sure it should require _something_
				RequiredScopes = new HashSet<string>(),

				RequireAuthenticatedUser = true,

				// views do not consume any parameters. (yet)
				ParameterInfos = new List<ParameterInfo>(),
				Deserializers = new List<ParameterDeserializer>(),
				ParameterNames = new List<string>(),
				ParameterDeserializers = new Dictionary<string, ParameterDeserializer>(),

				Method = callableMethod,

				ResponseSerializer = new DefaultResponseSerializer(false),

				Executor = (target, args) =>
				{
					// it doesn't matter what is given, we can control this...
					var imageName = microserviceAttribute.MicroserviceName.ToLower();
					return Task.FromResult(callableMethod?.Invoke(target, new object[]
					{
						imageName, microserviceType, view.ViewDescriptor
					}));
				}

			};

			return method;
		}).ToList();

		return routes;
	}

	public static MicroViewManifest Scan(MicroserviceAttribute serviceAttribute, Type microserviceType)
	{
		var allTypes = microserviceType.Assembly.GetTypes();
		var matchingTypes = allTypes.Where(t =>
		{
			var isSubClass = t.IsAssignableTo(typeof(MicroView));
			return isSubClass;
		});

		var entries = matchingTypes.Select(type =>
			{
				var attr = type.GetCustomAttribute<MicroViewAttribute>();
				if (attr == null) return null;
				return new MicroViewEntry
				{
					Name = attr.ViewName,
					AppName = ViewDescriptor.GetAppName(serviceAttribute.MicroserviceName, attr.ViewName),
					Slot = attr.UIPath,
					Path = $"admin/views/{attr.UIPath}/{attr.ViewName}",
					ViewDescriptor = new ViewDescriptor
					{
						ViewName = attr.ViewName,
						Slot = attr.UIPath,
						SourcePath = attr.SourcePath,
						AppName = ViewDescriptor.GetAppName(serviceAttribute.MicroserviceName, attr.ViewName),
						Type = type
					}
				};

			}).Where(x => x != null)
			.ToList();
		return new MicroViewManifest
		{
			Views = entries
		};
	}
}