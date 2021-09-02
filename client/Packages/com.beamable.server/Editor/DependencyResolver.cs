using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Server.Editor;
using Beamable.Platform.SDK;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using Beamable.Content;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Debug = UnityEngine.Debug;

namespace Beamable.Server
{

    public class AssemblyDefinitionInfo
    {
        public string Name;
        public string[] References = new string[]{};
        public string[] DllReferences = new string[] { };
        public string Location;
    }

    public class AssemblyDefinitionInfoGroup
    {
        public AssemblyDefinitionInfoCollection ToCopy;
        public AssemblyDefinitionInfoCollection Stubbed;
        public AssemblyDefinitionInfoCollection Invalid;

        public HashSet<string> DllReferences = new HashSet<string>();

    }

    public class AssemblyDefinitionNotFoundException : Exception
    {
        public AssemblyDefinitionNotFoundException(string assemblyName) : base($"Cannot find unity assembly {assemblyName}"){}
    }

    public class DllReferenceNotFoundException : Exception
    {
        public DllReferenceNotFoundException(string dllReference) : base($"Cannot find dll reference {dllReference}"){}
    }

    public class AssemblyDefinitionInfoCollection : IEnumerable<AssemblyDefinitionInfo>
    {
        private Dictionary<string, AssemblyDefinitionInfo> _assemblies = new Dictionary<string, AssemblyDefinitionInfo>();

        public AssemblyDefinitionInfoCollection(IEnumerable<AssemblyDefinitionInfo> assemblies)
        {
            _assemblies = assemblies.ToDictionary(a => a.Name);
        }

        public AssemblyDefinitionInfo Find(string assemblyName)
        {
            if (!_assemblies.TryGetValue(assemblyName, out var unityAssembly))
            {
                throw new AssemblyDefinitionNotFoundException(assemblyName);
            }

            return unityAssembly;
        }

        public AssemblyDefinitionInfo Find(Type type)
        {
            return Find(type.Assembly);
        }

        public AssemblyDefinitionInfo Find<T>()
        {
            return Find(typeof(T).Assembly);
        }

        public bool Contains(Type t)
        {
            try
            {
                Find(t);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public AssemblyDefinitionInfo Find(Assembly assembly)
        {
            // make sure this assembly exists in the Unity assembly definition list.
            var hasMoreThanOneModule = assembly.Modules.Count() > 1;
            if (hasMoreThanOneModule)
            {
                throw new Exception("Cannot handle multi-module assemblies");
            }

            var moduleName = assembly.Modules.FirstOrDefault().Name.Replace(".dll", "");
            if (!_assemblies.TryGetValue(moduleName, out var unityAssembly))
            {
                throw new Exception($"Cannot handle non unity assemblies yet. moduleName=[{moduleName}]");
            }

            return unityAssembly;
        }

        public IEnumerator<AssemblyDefinitionInfo> GetEnumerator()
        {
            return _assemblies.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class MicroserviceFileDependencyComparer: IEqualityComparer<MicroserviceFileDependency>
    {
        public bool Equals(MicroserviceFileDependency x, MicroserviceFileDependency y)
        {
            return string.Equals(x.Agnostic.SourcePath, y.Agnostic.SourcePath);
        }

        public int GetHashCode(MicroserviceFileDependency obj)
        {
            return obj.Agnostic.SourcePath.GetHashCode();
        }
    }
    public class MicroserviceFileDependency
    {
        public Type Type;
        public IHasSourcePath Agnostic;

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Type.Equals(obj);
        }


    }

    public class MicroserviceAssemblyDependency
    {
        public Assembly Assembly;
        public string DeveloperMachineLocation => Assembly.Location;
        public string DockerBuildLocation => $"/src/{MicroserviceDescriptor.ASSEMBLY_FOLDER_NAME}{Assembly.Location}"; // lib/path
    }

    public class MicroserviceDependencies
    {
        public List<MicroserviceFileDependency> FilesToCopy;
        public AssemblyDefinitionInfoGroup Assemblies;
        public List<PluginImporter> DllsToCopy;
    }

    public class DependencyResolver : MonoBehaviour
    {


        public static HashSet<Type> GetReferencedTypes(Type type)
        {
            var results = new HashSet<Type>();
            if (type == null) return results;

            void Add(Type t)
            {
                if (t == null) return;

                results.Add(t);
                if (t.IsGenericType)
                {
                    foreach (var g in t.GenericTypeArguments)
                    {
                        results.Add(g);
                    }
                }
            }

            // get all methods
            Add(type.BaseType);

            var agnosticAttribute = type.GetCustomAttribute<AgnosticAttribute>();
            if (agnosticAttribute != null && agnosticAttribute.SupportTypes != null)
            {
                foreach (var supportType in agnosticAttribute.SupportTypes)
                {
                    Add(supportType);
                }
            }

            foreach (var method in type.GetMethods())
            {
                // TODO: look at the method body itself for type references... https://github.com/jbevain/mono.reflection/blob/master/Mono.Reflection/MethodBodyReader.cs

                Add(method.ReturnType);

                foreach (var parameter in method.GetParameters())
                {
                    Add(parameter.ParameterType);
                }
            }

            // get all fields
            foreach (var field in type.GetFields())
            {
                Add(field.FieldType);
            }

            // get all properties
            foreach (var property in type.GetProperties())
            {
                Add(property.PropertyType);
            }

            // TODO get all generic types

            return new HashSet<Type>(results.Where(t => t != null));
        }

        private static bool IsUnityEngineType(Type t)
        {
            var ns = t.Namespace ?? "";
            var isUnity = ns.StartsWith("UnityEngine") || ns.StartsWith("UnityEditor");
            return isUnity;
        }

        private static bool IsSystemType(Type t)
        {
            var ns = t.Namespace ?? "";
            return ns.StartsWith("System");
        }

        private static bool IsBeamableType(Type t)
        {
            var ns = t.Namespace ?? "";
            if (typeof(Microservice).IsAssignableFrom(t))
            {
                return false; // TODO: XXX hacky and gross, but we *DO* want the Microservice type to get considered...
            }

            return ns.StartsWith("Beamable.Common") || ns.StartsWith("Beamable.Server");
        }

        private static bool IsStubbedType(Type t)
        {
            var stubbed = new Type[]
            {
                //typeof(JsonSerializable.ISerializable),
                typeof(ArrayDict),
                typeof(JsonSerializable.IStreamSerializer),
                typeof(ContentObject),
                typeof(IContentRef),

                typeof(ContentDelegate)
            };

            // we stub out any generic references to ContentRef and ContentLink, because those themselves are stubbed.

            if (t.IsGenericType)
            {
                var gt = t.GetGenericTypeDefinition();
                if (typeof(ContentRef<>).IsAssignableFrom(gt) || typeof(ContentLink<>).IsAssignableFrom(gt))
                {
                    return true;
                }
            }

            return stubbed.Any(s => s == t);
        }

        private static bool IsSourceCodeType(Type t, out IHasSourcePath attribute)
        {
            attribute = t.GetCustomAttribute<AgnosticAttribute>(false);
            if (attribute == null)
            {
                attribute = t.GetCustomAttribute<ContentTypeAttribute>(false);
            }

            return attribute != null;
        }

        public static bool IsMicroserviceRoot(Type t)
        {
            return typeof(Microservice).IsAssignableFrom(t);
        }

        public static string GetTypeName(Type t)
        {
            return t.FullName ?? (t.Namespace + "." + t.Name);
        }


        public static AssemblyDefinitionInfoCollection ScanAssemblyDefinitions()
        {
            var output = new List<AssemblyDefinitionInfo>();

            // TODO: Check that AssemblyDefinitionAsset is consistent on Unity 2019+
            var assemblyDefGuids = AssetDatabase.FindAssets($"t:{nameof(AssemblyDefinitionAsset)}");
            foreach (var assemblyDefGuid in assemblyDefGuids)
            {
                var assemblyDefPath = AssetDatabase.GUIDToAssetPath(assemblyDefGuid);
                var assemblyDef = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assemblyDefPath);

                var jsonData = Json.Deserialize(assemblyDef.text) as ArrayDict;

                var assemblyDefInfo = new AssemblyDefinitionInfo();
                assemblyDefInfo.Location = assemblyDefPath;

                if (jsonData.TryGetValue("name", out var nameObject) && nameObject is string name)
                {
                    assemblyDefInfo.Name = name;
                    output.Add(assemblyDefInfo);
                }

                if (jsonData.TryGetValue("references", out var referencesObject) &&
                    referencesObject is IEnumerable<object> references)
                {
                    assemblyDefInfo.References = references
                        .Cast<string>()
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToArray();
                }

                if (jsonData.TryGetValue("precompiledReferences", out var referencesDllObject) &&
                    referencesDllObject is IEnumerable<object> dllReferences)
                {
                    assemblyDefInfo.DllReferences = dllReferences
                        .Cast<string>()
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToArray();
                }
            }

            return new AssemblyDefinitionInfoCollection(output);
        }

        private static bool IsInvalid(AssemblyDefinitionInfoCollection assemblies, AssemblyDefinitionInfo assembly)
        {
            var startsWithUnity = assembly.Name.StartsWith("Unity");
            var startsWithUnityBeamable = assembly.Name.StartsWith("Unity.Beamable");

            // disallow unity assemblies from being loaded...
            var isUnityAssembly = (startsWithUnity && !startsWithUnityBeamable);

            return isUnityAssembly;
        }

        private static bool IsStubbed(AssemblyDefinitionInfoCollection assemblies,
            string assemblyName)
        {
            // TODO: maybe don't rebuild this every check?
            var rejectedAssemblies = new HashSet<AssemblyDefinitionInfo>
            {
                assemblies.Find<MicroserviceDescriptor>(), // Server.Editor
                assemblies.Find<MicroserviceClient>(), // Server.Runtime
                assemblies.Find<Microservice>(), // Server.SharedRuntime
                assemblies.Find<PromiseBase>(), // Common
                assemblies.Find<PlatformRequester>(), // Beamable.Platform
                assemblies.Find<ArrayDict>(), // SmallerJson
                assemblies.Find<API>(), // Beamable
            };

            if (assemblyName.Equals("Unity.Addressables"))
            {
                return true; // this assembly is okay... We couldn't search for it above because the user may not even have it installed :/
            }

            return rejectedAssemblies.FirstOrDefault(a => a.Name.Equals(assemblyName)) != null;
        }

        private static List<MicroserviceFileDependency> GatherAllContentTypes(MicroserviceDescriptor descriptor)
        {
            // the job here is to get all Agnostic types that are marked with "alwaysInclude"

            var output = new List<MicroserviceFileDependency>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var isTestAssembly = assembly.FullName.Contains("Test");
                var isEditorAssembly = assembly.FullName.Contains("Editor");
                if (isTestAssembly || isEditorAssembly) continue;
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    var contentAttr = type.GetCustomAttribute<ContentTypeAttribute>();
                    if (contentAttr == null) continue;

                    output.Add(new MicroserviceFileDependency
                    {
                        Agnostic = contentAttr,
                        Type = type
                    });
                }
            }

            return output;
        }

        private static List<PluginImporter> GatherDllDependencies(MicroserviceDescriptor descriptor, AssemblyDefinitionInfoGroup knownAssemblies)
        {
            var importers = PluginImporter.GetImporters(BuildTarget.NoTarget);

            var dllImporters = knownAssemblies.DllReferences.Select(dllReference =>
            {
                var importer = importers.FirstOrDefault(i =>
                {
                    var isMatch = i.assetPath.EndsWith(dllReference);
                    return isMatch;
                });
                if (importer == null)
                {
                    throw new DllReferenceNotFoundException(dllReference);
                }
                return importer;
            }).ToList();
            return dllImporters;
        }

        private static AssemblyDefinitionInfoGroup GatherAssemblyDependencies(MicroserviceDescriptor descriptor)
        {
            /*
            * We can crawl the assembly definition itself...
            */

            // reject the assembly that represents this microservice, because that will be recompiled separately.
            var unityAssemblies = ScanAssemblyDefinitions();
            var selfUnityAssembly = unityAssemblies.Find(descriptor.Type.Assembly);

            // crawl deps of unity assembly...
            var allRequiredUnityAssemblies = new HashSet<AssemblyDefinitionInfo>();
            var stubbedAssemblies = new HashSet<AssemblyDefinitionInfo>();
            var invalidAssemblies = new HashSet<AssemblyDefinitionInfo>();
            var unityAssembliesToExpand = new Queue<AssemblyDefinitionInfo>();
            var totalDllReferences = new HashSet<string>();
            unityAssembliesToExpand.Enqueue(selfUnityAssembly);

            while (unityAssembliesToExpand.Count > 0)
            {
                var curr = unityAssembliesToExpand.Dequeue();
                if (curr == null) continue;
                if (IsStubbed(unityAssemblies, curr.Name))
                {
                    stubbedAssemblies.Add(curr);
                    continue;
                }

                if (IsInvalid(unityAssemblies, curr))
                {
                    invalidAssemblies.Add(curr);
                    continue;
                }

                if (!allRequiredUnityAssemblies.Contains(curr))
                {
                    allRequiredUnityAssemblies.Add(curr);

                    foreach (var dllReference in curr.DllReferences)
                    {
                        totalDllReferences.Add(dllReference);
                    }

                    foreach (var referenceName in curr.References)
                    {
                        try
                        {
                            var referencedAssembly = unityAssemblies.Find(referenceName);
                            unityAssembliesToExpand.Enqueue(referencedAssembly);
                        }
                        catch (AssemblyDefinitionNotFoundException) when (IsStubbed(unityAssemblies, referenceName))
                        {
                            Debug.LogWarning($"Skipping {referenceName} because it is a stubbed package. You should still install the package for general safety.");
                        }
                    }
                }
            }
            return new AssemblyDefinitionInfoGroup
            {
                ToCopy = new AssemblyDefinitionInfoCollection(allRequiredUnityAssemblies),
                Stubbed = new AssemblyDefinitionInfoCollection(stubbedAssemblies),
                Invalid = new AssemblyDefinitionInfoCollection(invalidAssemblies),
                DllReferences = totalDllReferences
            };
        }

        private static List<MicroserviceFileDependency> GatherSingleFileDependencies(MicroserviceDescriptor descriptor, AssemblyDefinitionInfoGroup knownAssemblies)
        {
            Queue<Type> toExpand = new Queue<Type>();
            HashSet<string> seen = new HashSet<string>();
            Dictionary<string, string> trace = new Dictionary<string, string>();

            var fileDependencies = new HashSet<MicroserviceFileDependency>();

            var contentTypes = new List<MicroserviceFileDependency>();
            if (MicroserviceConfiguration.Instance.AutoReferenceContent)
            {
                contentTypes.AddRange(GatherAllContentTypes(descriptor));
            }

            toExpand.Enqueue(descriptor.Type);

            foreach (var contentType in contentTypes)
            {
                toExpand.Enqueue(contentType.Type);
                fileDependencies.Add(contentType);
            }

            seen.Add(descriptor.Type.FullName);
            while (toExpand.Count > 0)
            {
                var curr = toExpand.Dequeue();
                var currName = GetTypeName(curr);
                seen.Add(currName);

                // run any sort of white list?

                // filter the types that are unityEngine specific...
                if (IsUnityEngineType(curr))
                {
                    // TODO: Need to further white-list this, because not all Unity types will be stubbed on server.
                    //Debug.Log($"Found Unity Type {currName}");
                    //PrintTrace(currName);
                    continue; // don't go nuts chasing unity types..
                }

                if (IsSystemType(curr))
                {
                    //Debug.Log($"Found System Type {currName}");
                    continue; // don't go nuts chasing system types..
                }

                if (IsBeamableType(curr))
                {
                    continue;
                }

                if (IsStubbedType(curr))
                {
                    //Debug.Log($"Found STUB TYPE {currName}");
                    continue;
                }
                var isAssemblyStubbed = knownAssemblies.Stubbed.Contains(curr);
                var isAssemblyKnown = knownAssemblies.ToCopy.Contains(curr);
                var isValidFileDependency = isAssemblyKnown || isAssemblyStubbed;

                if (IsSourceCodeType(curr, out var agnosticAttribute))
                {
                    // This is good, we can copy this code
                    if (!isValidFileDependency)
                    {
                        Debug.LogWarning($"WARNING: The type will be pulled into your microservice through an Agnostic attribute. Beamable suggests you put this type into a shared assembly definition instead. type=[{curr}]");
                        fileDependencies.Add(new MicroserviceFileDependency
                        {
                            Type = curr,
                            Agnostic = agnosticAttribute
                        });
                    }
                }
                else if (!IsMicroserviceRoot(curr))
                {
                    // check that this type exists in the known assemblies...

                    if (!isValidFileDependency)
                    {
                        Debug.LogError($"Unknown type referenced. Expect Failure. {currName} {curr.Assembly.Location}");
                    }
                }


                var references = GetReferencedTypes(curr);
                foreach (var reference in references)
                {
                    var referenceName = GetTypeName(reference);
                    if (reference == null || seen.Contains(referenceName))
                    {
                        continue; // we've already seen this type, so march on
                    }

                    seen.Add(referenceName);
                    trace.Add(referenceName, currName);
                    toExpand.Enqueue(reference);
                }

            }


            var unstubbedDistinctFileDependencies = fileDependencies
                .Where(d => !knownAssemblies.ToCopy.Contains(d.Type) && !knownAssemblies.Stubbed.Contains(d.Type))
                .Distinct(new MicroserviceFileDependencyComparer())
                .ToList();

            return unstubbedDistinctFileDependencies;
        }


        public static MicroserviceDependencies GetDependencies(MicroserviceDescriptor descriptor)
        {
            var assemblyRequirements = GatherAssemblyDependencies(descriptor);
            var dlls = GatherDllDependencies(descriptor, assemblyRequirements);
            var infos = GatherSingleFileDependencies(descriptor, assemblyRequirements);
            return new MicroserviceDependencies
            {
                FilesToCopy = infos,
                Assemblies = assemblyRequirements,
                DllsToCopy = dlls
            };
        }


    }

}