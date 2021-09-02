using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Common.Content
{

   public class ContentQuery : DefaultQuery
   {
      public static readonly ContentQuery Unit = new ContentQuery();

      public HashSet<Type> TypeConstraints;
      public HashSet<string> TagConstraints;

      public ContentQuery()
      {

      }

      public ContentQuery(ContentQuery other)
      {
         if (other == null) return;

         TypeConstraints = other.TypeConstraints != null
            ? new HashSet<Type>(other.TypeConstraints.ToArray())
            : null;
         TagConstraints = other.TagConstraints != null
            ? new HashSet<string>(other.TagConstraints.ToArray())
            : null;
         IdContainsConstraint = other.IdContainsConstraint;
      }

      protected static void ApplyTypeParse(string raw, ContentQuery query)
      {
         try
         {
            var typeNames = raw.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            var types = new HashSet<Type>();
            foreach (var typeName in typeNames)
            {
               try
               {
                  var type = ContentRegistry.NameToType(typeName);
                  types.Add(type);
               }
               catch (Exception ex)
               {
                  BeamableLogger.LogException(ex);
               }
            }
            query.TypeConstraints = new HashSet<Type>(types);

         }
         catch (Exception)
         {
            // don't do anything.
            //query.TypeConstraint = typeof(int); // something to block filtering from working.
         }
      }


      protected static void ApplyTagParse(string raw, ContentQuery query)
      {
         var tags = raw.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
         query.TagConstraints = new HashSet<string>(tags);
      }

      protected static readonly Dictionary<string, DefaultQueryParser.ApplyParseRule<ContentQuery>> StandardRules = new Dictionary<string, DefaultQueryParser.ApplyParseRule<ContentQuery>>
      {
         {"t", ApplyTypeParse},
         {"id", DefaultQueryParser.ApplyIdParse},
         {"tag", ApplyTagParse},
      };

      protected static readonly List<DefaultQueryParser.SerializeRule<ContentQuery>> StandardSerializeRules = new List<DefaultQueryParser.SerializeRule<ContentQuery>>
      {
         SerializeTagRule, SerializeTypeRule, SerializeIdRule
      };

      public static ContentQuery Parse(string text)
      {
         return DefaultQueryParser.Parse(text, StandardRules);
      }

      public virtual bool Accept(IContentObject content)
      {
         if (content == null) return false;

         return AcceptTag(content) && AcceptIdContains(content) && AcceptType(content.GetType());
      }

      public bool AcceptTag(IContentObject content)
      {
         if (TagConstraints == null) return true;
         if (content == null)
         {
            return TagConstraints.Count == 0;
         }

         return AcceptTags(new HashSet<string>(content.Tags));
      }

      public bool AcceptTags(HashSet<string> tags)
      {
         if (TagConstraints == null) return true;
         if (tags == null) return TagConstraints.Count == 0;

         foreach (var tag in TagConstraints)
         {
            if (!tags.Contains(tag))
            {
               return false;
            }
         }

         return true;
      }

      public bool AcceptType<TContent>(bool allowInherit=true) where TContent : IContentObject, new()
      {
         return AcceptType(typeof(TContent), allowInherit);
      }

      public bool AcceptType(Type type, bool allowInherit=true)
      {
         if (TypeConstraints == null || TypeConstraints.Count == 0) return true;

         if (type == null) return false;

         if (allowInherit)
         {
            return TypeConstraints.Any(t => t.IsAssignableFrom(type));
         }
         else
         {
            return TypeConstraints.Contains(type);
         }
      }

      public bool AcceptIdContains(IContentObject content)
      {
         return AcceptIdContains(content?.Id);
      }

      protected static bool SerializeTagRule(ContentQuery query, out string str)
      {
         str = "";
         if (query.TagConstraints == null)
         {
            return false;
         }
         str = $"tag:{string.Join(" ", query.TagConstraints)}";
         return true;
      }

      protected static bool SerializeTypeRule(ContentQuery query, out string str)
      {
         str = "";
         if (query.TypeConstraints == null)
         {
            return false;
         }
         str = $"t:{string.Join(" ", query.TypeConstraints.Select(ContentRegistry.TypeToName))}";
         return true;
      }



      public bool EqualsContentQuery(ContentQuery other)
      {
         if (other == null) return false;

         var tagsEqual = other.TagConstraints == null || TagConstraints == null
            ? (other.TagConstraints == null && TagConstraints == null)
            : (other.TagConstraints.SetEquals(TagConstraints));

         var typesEqual = other.TypeConstraints == null || TypeConstraints == null
            ? (other.TypeConstraints == null && TypeConstraints == null)
            : other.TypeConstraints.SetEquals(TypeConstraints);

         var idEqual = (other.IdContainsConstraint?.Equals(IdContainsConstraint) ?? IdContainsConstraint == null);
         return tagsEqual &&
                idEqual &&
                typesEqual;
      }

      public override bool Equals(object obj)
      {
         return EqualsContentQuery(obj as ContentQuery);
      }

      public override int GetHashCode()
      {
         unchecked
         {
            var hashCode = (TypeConstraints != null ? TypeConstraints.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (TagConstraints != null ? TagConstraints.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (IdContainsConstraint != null ? IdContainsConstraint.GetHashCode() : 0);
            return hashCode;
         }
      }

      public string ToString(string existing)
      {
         return DefaultQueryParser.ToString(existing, this, StandardSerializeRules, StandardRules);
      }

      public override string ToString()
      {
         return ToString(null);
      }
   }
}