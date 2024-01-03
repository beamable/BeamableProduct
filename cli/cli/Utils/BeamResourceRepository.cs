using Errata;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace cli.Utils;

public sealed class BeamResourceRepository : ISourceRepository
{
	private readonly Dictionary<string, Source> _lookup;
	private readonly Assembly _assembly;

	public BeamResourceRepository(Assembly assembly)
	{
		_lookup = new Dictionary<string, Source>(StringComparer.OrdinalIgnoreCase);
		_assembly = assembly;
	}
	static Stream LoadResourceStream(Assembly assembly, string resourceName)
	{
		if (assembly is null)
		{
			throw new ArgumentNullException(nameof(assembly));
		}

		if (resourceName is null)
		{
			throw new ArgumentNullException(nameof(resourceName));
		}

		resourceName = resourceName.Replace("/", ".");
		return assembly.GetManifestResourceStream(resourceName);
	}
	public bool TryGet(string id, [NotNullWhen(true)] out Source source)
	{
		if (_lookup.TryGetValue(id, out source))
		{
			return true;
		}

		if (File.Exists(id))
		{
			source = new Source(id, File.ReadAllText(id).Replace("\r\n","\n"));
			_lookup[id] = source;
			return true;
		}

		using var stream = LoadResourceStream(_assembly, id);
		using var reader = new StreamReader(stream);
		source = new Source(id, reader.ReadToEnd().Replace("\r\n", "\n"));
		_lookup[id] = source;

		return true;
	}
}
