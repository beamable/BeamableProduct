// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using LoxSmoke.DocXml;
//
// namespace microservice.Common
// {
//    public delegate IDocCommentsProvider DocCommentProviderFactory(string xmlPath);
//    public interface IDocCommentsProvider
//    {
//       MethodComments GetComments(MethodInfo info);
//    }
//
//    public class DefaultDocCommentsProvider : IDocCommentsProvider
//    {
//       public DocXmlReader Reader;
//       public MethodComments GetComments(MethodInfo info)
//       {
//          return Reader.GetMethodComments(info);
//       }
//    }
//
//    public static class XmlDocsHelper
//    {
//       private const string BASE_IMAGE_DOCS_PATH = "baseImageDocs.xml";
//       private const string MICROSERVICE_DOCS_PATH = "serviceDocs.xml";
//       private static Dictionary<Assembly, IDocCommentsProvider> _assemblyToDocs = new Dictionary<Assembly, IDocCommentsProvider>();
//
//       public static DocCommentProviderFactory ProviderFactory { get; set; } = (path) => null;
//       public static DocCommentProviderFactory FileIOProvider = (path) => new DefaultDocCommentsProvider
//       {
//          Reader = new DocXmlReader(path)
//       };
//
//       public static void ProvideXmlForBaseImage(Type type) => ProvideXmlForAssembly(type, BASE_IMAGE_DOCS_PATH);
//       public static void ProvideXmlForService(Type type) => ProvideXmlForAssembly(type, MICROSERVICE_DOCS_PATH);
//       public static void ProvideXmlForAssembly(Type type, string xmlPath) => ProvideXmlForAssembly(type.Assembly, xmlPath);
//       public static void ProvideXmlForAssembly(Assembly asm, string xmlPath)
//       {
//          _assemblyToDocs[asm] = ProviderFactory(xmlPath);
//       }
//
//       public static bool TryGetComments(MethodInfo info, out MethodComments comments)
//       {
//          comments = null;
//          if (_assemblyToDocs.TryGetValue(info.Module.Assembly, out var reader))
//          {
//             comments = reader.GetComments(info);
//          }
//
//          return comments != null;
//       }
//    }
// }
