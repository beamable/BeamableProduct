using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using Beamable.Server;

namespace Beamable.Server.Editor.CodeGen
{
   public class ProgramCodeGenerator
   {
      public MicroserviceDescriptor Descriptor { get; }

      CodeCompileUnit targetUnit;
      CodeTypeDeclaration targetClass;


      public ProgramCodeGenerator(MicroserviceDescriptor descriptor)
      {
         Descriptor = descriptor;

         Descriptor = descriptor;
         targetUnit = new CodeCompileUnit();
         CodeNamespace samples = new CodeNamespace("Beamable.Server");

         samples.Imports.Add(new CodeNamespaceImport(descriptor.Type.Namespace));
         samples.Imports.Add(new CodeNamespaceImport("Beamable.Common"));
         targetClass = new CodeTypeDeclaration("Program");
         targetClass.IsClass = true;
         targetClass.TypeAttributes = TypeAttributes.Public;

         var mainmethod = new CodeEntryPointMethod();


         var invokeExpr = new CodeMethodInvokeExpression(
            new CodeMethodReferenceExpression(
               new CodeTypeReferenceExpression("global::Beamable.Server.MicroserviceBootstrapper"), // XXX: This is super brittle...
               "Start",
               new CodeTypeReference[]
               {
                  new CodeTypeReference(descriptor.Type),
               }),
            new CodeExpression[]
            {
            });
         mainmethod.Statements.Add(invokeExpr);

         targetClass.Members.Add(mainmethod);

         samples.Types.Add(targetClass);
         targetUnit.Namespaces.Add(samples);
      }

      public void GenerateCSharpCode(string fileName)
      {
         CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
         CodeGeneratorOptions options = new CodeGeneratorOptions();
         options.BracingStyle = "C";
         using (StreamWriter sourceWriter = new StreamWriter(fileName))
         {
            provider.GenerateCodeFromCompileUnit(
               targetUnit, sourceWriter, options);
         }
      }

   }
}