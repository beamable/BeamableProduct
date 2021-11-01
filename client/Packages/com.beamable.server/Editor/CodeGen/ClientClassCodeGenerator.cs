using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ClientClassCodeGenerator
{
    private static string GETTER_SETTER_BODY = " { get; set; }//";

    public static void GenerateClientClass(CodeNamespace ns, Type sourceType)
    {
        CodeTypeDeclaration genClass = new CodeTypeDeclaration(sourceType.Name);
        genClass.Comments.Add(new CodeCommentStatement($"<summary> A generated mocked dependency class for <see cref=\"{sourceType.FullName}\"/> </summary", true));

        ns.Types.Add(genClass);

        List<Type> unknownTypes = new List<Type>();

        unknownTypes.AddRange(GenerateClientClassFields(genClass, sourceType));
        unknownTypes.AddRange(GenerateClientClassProperties(genClass, sourceType));
        unknownTypes.AddRange(GenerateClientClassMethods(genClass, sourceType));

        GenerateClientClassConstructors(genClass, sourceType);

        if (unknownTypes.Count > 0)
        {
            for (int i = 0; i < unknownTypes.Count; i++)
            {
                if (!IsTypeExistInNamespace(ns, unknownTypes[i]))
                    GenerateClientClass(ns, unknownTypes[i]);
            }
        }

        unknownTypes.Clear();
    }

    public static bool IsTypeExistInNamespace(CodeNamespace ns, Type type)
    {
        return IsTypeExistInNamespace(ns, type.Name);
    }

    public static bool IsTypeExistInNamespace(CodeNamespace ns, string type)
    {
        foreach (CodeTypeDeclaration tt in ns.Types)
        {
            if (string.Equals(tt.Name, type))
                return true;
        }

        return false;
    }

    static List<Type> GenerateClientClassFields(CodeTypeDeclaration genClass, Type sourceType)
    {
        List<Type> unknownTypes = new List<Type>();

        foreach (var fieldData in sourceType.GetFields())
        {
            CodeMemberField field = new CodeMemberField();
            field.Attributes = MemberAttributes.Public | MemberAttributes.Final;

            if (Type.GetType(fieldData.FieldType.ToString()) != null)
                field.Type = new CodeTypeReference(fieldData.FieldType);
            else
            {
                field.Type = new CodeTypeReference(fieldData.FieldType.Name);
                unknownTypes.Add(fieldData.FieldType);
            }

            field.Name = fieldData.Name;

            genClass.Members.Add(field);
        }

        return unknownTypes;
    }

    static List<Type> GenerateClientClassProperties(CodeTypeDeclaration genClass, Type sourceType)
    {
        List<Type> unknownTypes = new List<Type>();

        foreach (var propertyData in sourceType.GetProperties())
        {
            CodeMemberField property = new CodeMemberField();
            property.Attributes = MemberAttributes.Public | MemberAttributes.Final;

            if (Type.GetType(propertyData.PropertyType.ToString()) != null)
                property.Type = new CodeTypeReference(propertyData.PropertyType);
            else
            {
                property.Type = new CodeTypeReference(propertyData.PropertyType.Name);
                unknownTypes.Add(propertyData.PropertyType);
            }

            property.Name = propertyData.Name;

            property.Name += GETTER_SETTER_BODY;
            genClass.Members.Add(property);
        }

        return unknownTypes;
    }


    static void GenerateClientClassConstructors(CodeTypeDeclaration genClass, Type sourceType)
    {
        bool hasEmptyConstructor = false;
        int constructors = 0;

        foreach (var constructorData in sourceType.GetConstructors())
        {
            CodeConstructor constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public | MemberAttributes.Final;


            var constructorParam = constructorData.GetParameters();

            foreach (var singleConstructorParam in constructorParam)
            {
                constructor.Parameters.Add(new CodeParameterDeclarationExpression(singleConstructorParam.ParameterType.Name, singleConstructorParam.Name));

                // assignConstructorParam
                foreach (CodeTypeMember member in genClass.Members)
                {
                    string tmpName = member.Name.Replace(GETTER_SETTER_BODY, "");
                    if (string.Equals(tmpName.ToLower(), singleConstructorParam.Name.ToLower()))
                    {
                        if (member is CodeMemberField)
                        {
                            CodeMemberField field = (CodeMemberField)member;
                            CodeTypeReference compareVal = new CodeTypeReference(singleConstructorParam.ParameterType);

                            if (field.Type.BaseType == compareVal.BaseType)
                            {
                                CodeFieldReferenceExpression reference = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), tmpName);
                                constructor.Statements.Add(new CodeAssignStatement(reference, new CodeArgumentReferenceExpression(singleConstructorParam.Name)));
                                break;
                            }
                        }
                    }
                }
            }

            if (constructorParam.Length == 0)
                hasEmptyConstructor = true;

            if (constructor.Statements.Count > 0)
                genClass.Members.Add(constructor);

            constructors++;
        }

        if (!hasEmptyConstructor && constructors > 0)
        {
            CodeConstructor emptyConstructor = new CodeConstructor();
            emptyConstructor.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            genClass.Members.Add(emptyConstructor);
        }
    }

    static List<Type> GenerateClientClassMethods(CodeTypeDeclaration genClass, Type sourceType)
    {
        List<Type> unknownTypes = new List<Type>();

            List<string> processed = new List<string>();

            foreach (MethodInfo method in sourceType.GetMethods())
            {
                if (method.DeclaringType == typeof(object))
                    continue;

                if (method.GetBaseDefinition().DeclaringType != method.DeclaringType) // not override methods
                    continue;

                if ((method.Attributes & MethodAttributes.SpecialName) != 0)
                    continue;

                if (processed.Contains(method.Name))
                    continue;

                processed.Add(method.Name);

                CodeMemberMethod genMethod = new CodeMemberMethod();
                genMethod.Name = method.Name;
                genMethod.Attributes = MemberAttributes.Public;

                if (method.IsStatic)
                    genMethod.Attributes |= MemberAttributes.Static | MemberAttributes.Final;

                CodeMethodReturnStatement returnStatement = null;

                if (Type.GetType(method.ReturnType.ToString()) != null)
                {
                    genMethod.ReturnType = new CodeTypeReference(method.ReturnType);
                    returnStatement = new CodeMethodReturnStatement(new CodeObjectCreateExpression(method.ReturnType));
                }
                else
                {
                    genMethod.ReturnType = new CodeTypeReference(method.ReturnType.Name);
                    returnStatement = new CodeMethodReturnStatement(new CodeObjectCreateExpression(method.ReturnType.Name));
                    unknownTypes.Add(method.ReturnType);
                }

                if (method.ReturnType == typeof(void))
                    returnStatement = null;
                else if (method.ReturnType.IsArray || method.ReturnType.IsGenericType)
                    returnStatement = new CodeMethodReturnStatement(new CodePrimitiveExpression(null));

                foreach (ParameterInfo param in method.GetParameters())
                {
                    FieldDirection dir = FieldDirection.In;
                    Type paramType;
                    if (param.ParameterType.IsByRef)
                    {
                        paramType = param.ParameterType.GetElementType();
                        if (param.IsOut)
                        {
                            dir = FieldDirection.Out;

                            if (paramType.IsPrimitive)
                                genMethod.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(param.Name), new CodeObjectCreateExpression(paramType)));
                            else
                                genMethod.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(param.Name), new CodePrimitiveExpression(null)));
                        }
                        else
                        {
                            dir = FieldDirection.Ref;
                        }
                    }
                    else
                    {
                        paramType = param.ParameterType;
                    }

                    if (Type.GetType(paramType.ToString()) != null)
                    {
                        genMethod.Parameters.Add( new CodeParameterDeclarationExpression(paramType, param.Name) { Direction = dir });
                    }
                    else
                    {
                        genMethod.Parameters.Add(new CodeParameterDeclarationExpression(paramType.Name, param.Name) { Direction = dir });
                        unknownTypes.Add(paramType);
                    }
                }

                if (returnStatement != null)
                    genMethod.Statements.Add(returnStatement);

                genClass.Members.Add(genMethod);
            }

        return unknownTypes;
    }
}