using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;

namespace Beamable.Editor.Toolbox.Models
{
   public class ToolboxQuery : DefaultQuery
   {
      public bool HasOrientationConstraint;
      public WidgetOrientationSupport OrientationConstraint;

      public bool HasTagConstraint;
      public WidgetTags TagConstraint;


      protected static Dictionary<string, DefaultQueryParser.ApplyParseRule<ToolboxQuery>> EditorRules;
      protected static List<DefaultQueryParser.SerializeRule<ToolboxQuery>> EditorSerializeRules;
      static ToolboxQuery()
      {
         EditorRules = new Dictionary<string, DefaultQueryParser.ApplyParseRule<ToolboxQuery>>
         {
            {"id", DefaultQueryParser.ApplyIdParse},
            {"tag", ApplyTagRule},
            {"layout", ApplyOrientationRule}
         };

         EditorSerializeRules = new List<DefaultQueryParser.SerializeRule<ToolboxQuery>>
         {
            SerializeOrientationRule, SerializeTypeConstraintsRule, SerializeIdRule
         };
      }


      public ToolboxQuery(){

      }
      public ToolboxQuery(ToolboxQuery other)
      {
         if (other == null) return;

         TagConstraint = other.TagConstraint;
         IdContainsConstraint = other.IdContainsConstraint;
         OrientationConstraint = other.OrientationConstraint;
         HasTagConstraint = other.HasTagConstraint;
         HasOrientationConstraint = other.HasOrientationConstraint;
      }

      public bool Accepts(Widget widget)
      {
         return AcceptIdContains(widget.Name.ToLower()) && AcceptsTag(widget.Tags) && AcceptsOrientation(widget.OrientationSupport);
      }

      public bool AcceptsTag(WidgetTags tags)
      {
         if (!HasTagConstraint) return true;
         return tags.ContainsAllFlags(TagConstraint);
      }

      public bool AcceptsOrientation(WidgetOrientationSupport orientationSupport)
      {
         if (!HasOrientationConstraint) return true;
         return orientationSupport.ContainsAllFlags(OrientationConstraint);
      }

      public bool FilterIncludes(WidgetOrientationSupport orientationSupport)
      {
         if (!HasOrientationConstraint) return false;
         return OrientationConstraint.ContainsAnyFlag(orientationSupport);
      }

      public bool FilterIncludes(WidgetTags tags)
      {
         if (!HasTagConstraint) return false;
         return TagConstraint.ContainsAnyFlag(tags);
      }

      private static bool SerializeTypeConstraintsRule(ToolboxQuery query, out string str)
      {
         str = "";
         if (query.HasTagConstraint)
         {
            str = $"tag:{query.TagConstraint.Serialize()}";
            return true;
         }
         return false;
      }
      private static bool SerializeOrientationRule(ToolboxQuery query, out string str)
      {
         str = "";
         if (query.HasOrientationConstraint)
         {
            str = $"layout:{query.OrientationConstraint.Serialize()}";
            return true;
         }
         return false;
      }


      private static void ApplyTagRule(string raw, ToolboxQuery query)
      {
         query.HasTagConstraint = false;
         if (WidgetTagExtensions.TryParse(raw, out var tagConstraint))
         {
            query.HasTagConstraint = true;
            query.TagConstraint = tagConstraint;
         }
      }

      private static void ApplyOrientationRule(string raw, ToolboxQuery query)
      {
         query.HasOrientationConstraint = false;
         if (WidgetOrientationSupportExtensions.TryParse(raw, out var orientationConstraint))
         {
            query.HasOrientationConstraint = true;
            query.OrientationConstraint = orientationConstraint;
         }
      }

      private string ToString(string existing)
      {
         return DefaultQueryParser.ToString(existing, this, EditorSerializeRules, EditorRules);
      }

      public override string ToString()
      {
         return ToString(null);
      }

      public static ToolboxQuery Parse(string text)
      {
         return DefaultQueryParser.Parse(text, EditorRules);
      }

      private bool Equals(ToolboxQuery other)
      {
         if (other == null) return false;
         return other.HasOrientationConstraint == HasOrientationConstraint
                && other.OrientationConstraint == OrientationConstraint
                && other.IdContainsConstraint == IdContainsConstraint
                && other.HasTagConstraint == HasTagConstraint
                && other.TagConstraint == TagConstraint;
      }

      public override bool Equals(object obj)
      {
         return Equals(obj as ToolboxQuery);
      }

      public override int GetHashCode()
      {
         unchecked
         {
            var hashCode = HasOrientationConstraint.GetHashCode();
            hashCode = (hashCode * 397) ^ (int) OrientationConstraint;
            hashCode = (hashCode * 397) ^ HasTagConstraint.GetHashCode();
            hashCode = (hashCode * 397) ^ (int) TagConstraint;
            return hashCode;
         }
      }
   }
}