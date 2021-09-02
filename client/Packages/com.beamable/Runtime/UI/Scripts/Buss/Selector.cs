using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace Beamable.UI.Buss
{
   public static class SelectorParser
   {
      enum SelectorPartType
      {
         CLASS, ELEMENT, PSEUDO, ID
      }

      public static Selector Parse(string selectorString)
      {
         var subSelections = selectorString.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
         var entireSelector = new Selector();

         Selector selector = entireSelector;
         for (var sIndex = 0; sIndex < subSelections.Length; sIndex += 1)
         {
            if (sIndex != 0)
            {
               var lastSelector = selector;
               selector = new Selector();
               lastSelector.ChildSelector = selector;
            }

            var subSelector = subSelections[sIndex];

            var type = SelectorPartType.ELEMENT;
            var buffer = "";

            var breakCharacters = new char[] {'.', ':', '(', ')', '#'};
            for (var i = 0; i <= subSelector.Length; i++)
            {
               char c = ' ';
               if (i == subSelector.Length || breakCharacters.Contains(c = subSelector[i]))
               {

                  // break last build
                  var isExprStart = c == '(';
                  var nextIsClass = c == '.';
                  var nextIsId = c == '#';
                  var nextIsPseudo = c == ':';
                  if (isExprStart)
                  {
                     // fill the buffer with whatever, until the next )
                     var parenCount = 0;
                     while ((c != ')' || parenCount > 0) && i < subSelector.Length)
                     {
                        if (c == '(')
                        {
                           parenCount ++;
                        }

                        if (c == ')')
                        {
                           parenCount --;
                        }
                        c = subSelector[i];
                        buffer += c;
                        i++;
                     }
                  }

                  switch (type)
                  {
                     case SelectorPartType.PSEUDO:

                        switch (buffer)
                        {
                           case "hover":
                              selector.PseudoConstraints.Add(new PseudoHoverSelector());
                              break;
//                           case "disabled":
//                              selector.PseudoConstraints.Add(new PseudoDisabledSelector());
//                              break;
                           case "active":
                              selector.PseudoConstraints.Add(new PseudoActiveSelector());
                              break;
                           case "root":
                              selector.PseudoConstraints.Add(new PseudoRootSelector());
                              break;
                           case string s when s.StartsWith("not(") && s.EndsWith(")") :
                              var innerSelectorString =
                                 s.Substring("not(".Length, s.Length - ("not(".Length + ")".Length));
                              var innerSelector = Parse(innerSelectorString);
                              selector.PseudoConstraints.Add(new PseudoNotSelector(innerSelector));
                              break;
                           case string s when s.StartsWith("nth-child(") && s.EndsWith(")"):
                              var inner = s.Substring("nth-child(".Length,
                                 s.Length - ("nth-child(".Length + ")".Length));
                              selector.PseudoConstraints.Add(new PseudoNthChildSelector(inner));
                              break;

                           default:
                              throw new Exception("unknown pseudo selector. " + buffer);
                        }


                        break;
                     case SelectorPartType.CLASS:
                        selector.ClassConstraints.Add(buffer);
                        break;
                     case SelectorPartType.ID:
                        selector.IdConstraint = buffer;
                        break;
                     case SelectorPartType.ELEMENT:
                        if (StyleBehaviour.IsElementTypeName(buffer.ToLower().Trim(), out var elementType))
                        {
                           selector.ElementTypeConstraint = elementType;
                        }
                        break;
                  }

                  // start new classBuilder
                  buffer = "";
                  if (nextIsClass)
                  {
                     type = SelectorPartType.CLASS;
                  } else if (nextIsId)
                  {
                     type = SelectorPartType.ID;
                  }
                  else
                  {
                     type = SelectorPartType.PSEUDO;
                  }
               }
               else
               {
                  buffer += c;
               }
            }

         }

         return entireSelector;
      }
   }

   [System.Serializable]
   public class Selector : ISerializationCallbackReceiver
   {
      [NonSerialized]
      public Selector ChildSelector;

      [NonSerialized]
      public List<string> ClassConstraints = new List<string>();

      [NonSerialized]
      public string ElementTypeConstraint;

      [NonSerialized]
      public List<PseudoSelector> PseudoConstraints = new List<PseudoSelector>();

      [NonSerialized]
      public string IdConstraint;

      [NonSerialized]
      public StyleBehaviour InlineConstraint;

      [SerializeField]
      private string _selfString;

      public bool IsInline => InlineConstraint != null;

      public void Commit()
      {
         _selfString = ToString();
      }

      public int WeightFrom(StyleBehaviour node)
      {
         // if the selector applies directly, then that is a really high weight.
         var baseWeight = Weight();
         var distance = node.MatchSelectorDistance(this);
         if (distance == 0)
         {
            return baseWeight;
         }

         return ((100 - distance) * 1000) + baseWeight;
      }

      public int Weight()
      {
         /* https://www.w3schools.com/css/css_specificity.asp
          *
          * The weight of a selector is derived from its specificity...
          *
          * div = x
          * .primary = y
          *
          * div.primary = x+y
          *
          * TODO: make the weight calculation match the css spec exactly... currently its just "good enough"
          */

         var weight = ClassConstraints.Count * 5;
         weight += (PseudoConstraints.Count * 7);
         if (!string.IsNullOrEmpty(IdConstraint))
         {
            weight += 100;
         }
         if (ElementTypeConstraint != null)
         {
            weight += 3;
         }


         var childWeight = ChildSelector?.Weight() * 2 ?? 0;
         return weight + childWeight;
      }

      public bool Accept(StyleBehaviour element)
      {
         if (!string.IsNullOrEmpty(IdConstraint))
         {
            if (!element.Id.Equals(IdConstraint))
            {
               return false;
            }
         }

         if (InlineConstraint != null)
         {
            if (element != InlineConstraint)
            {
               return false;
            }
         }

         if (!string.IsNullOrEmpty(ElementTypeConstraint))
         {
            if (element == null) return false;

            if (!string.Equals(element.TypeString, ElementTypeConstraint))
            {
               return false; // the element is not of the specified type.
            }
         }

         if (ClassConstraints != null)
         {
            var classNames = element.GetClassNames();
            foreach (var clazz in ClassConstraints)
            {
               if (string.IsNullOrEmpty(element.ClassString) || !classNames.Contains(clazz.Trim().ToLower()))
               {
                  return false;
               }
            }
         }

         if (PseudoConstraints != null)
         {
            foreach (var constraint in PseudoConstraints)
            {
               if (!constraint.Accept(element))
               {
                  return false;
               }
            }
         }

         return true;
      }

      public IEnumerable<Selector> Ascend()
      {
         var stack = new Stack<Selector>();

         var current = this;
         while (current != null)
         {
            stack.Push(current);
            current = current.ChildSelector;
         }

         while (stack.Count > 0)
         {
            var bottom = stack.Pop();
            yield return bottom;
         }
      }

      public override string ToString()
      {
         var self = "";

         if (InlineConstraint != null)
         {
            self += "inline";
         }

         if (ElementTypeConstraint != null)
         {
            self += ElementTypeConstraint;
         }

         if (!string.IsNullOrEmpty(IdConstraint))
         {
            self += "#" + IdConstraint;
         }

         if (ClassConstraints.Count > 0)
         {
            self += "." + string.Join(".", ClassConstraints);
         }

         if (PseudoConstraints.Count > 0)
         {
            self += ":" + string.Join(":", PseudoConstraints);
         }

         return ChildSelector == null ? self : (self + " " + ChildSelector.ToString());
      }


      public void OnBeforeSerialize()
      {
         Commit();
      }

      public void OnAfterDeserialize()
      {
         var self = SelectorParser.Parse(_selfString) ?? new Selector();
         ChildSelector = self.ChildSelector;
         ElementTypeConstraint = self.ElementTypeConstraint;
         ClassConstraints = self.ClassConstraints;
         PseudoConstraints = self.PseudoConstraints;
         IdConstraint = self.IdConstraint;
      }
   }
}