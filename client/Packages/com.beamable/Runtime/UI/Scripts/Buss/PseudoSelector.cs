using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Beamable.UI.Buss
{
   public abstract class PseudoSelector
   {
      public abstract bool Accept(StyleBehaviour element);
   }

   public class PseudoRootSelector : PseudoSelector
   {
      public override bool Accept(StyleBehaviour element)
      {
         return element.GetRoot() == element;
      }
      public override string ToString()
      {
         return "root";
      }
   }

   public class PseudoNotSelector : PseudoSelector
   {
      public Selector NotSelector;
      public PseudoNotSelector(Selector notSelection)
      {
         NotSelector = notSelection;
      }
      public override bool Accept(StyleBehaviour element)
      {
         return !element.Matches(NotSelector);
      }

      public override string ToString()
      {
         return "not(" + NotSelector + ")";
      }
   }

   public class PseudoNthChildSelector : PseudoSelector
   {
      public string Expr { get; private set; }

      //An + B
      public int A { get; private set; }
      public int B { get; private set; }

      public PseudoNthChildSelector(string expr)
      {
         Expr = expr;
         switch (expr)
         {
            case "even":
               A = 2;
               B = 0;
               break;
            case "odd":
               A = 2;
               B = 1;
               break;
            case string s when int.TryParse(s, out var n):
               A = 0;
               B = n;
               break;
            default:
               // need to parse out A and B;

               var r = new Regex(@"^([-+]?\d*)\s*n\s*([-+]\s*\d*)?$", RegexOptions.IgnoreCase);
               var m = r.Match(expr.Trim());
               if (m.Success)
               {
                  var aStr = m.Groups[1].Captures[0].Value;
                  switch (aStr)
                  {
                     case string s when string.IsNullOrEmpty(s):
                        aStr = "1";
                        break;
                     case "-":
                        aStr = "-1";
                        break;
                  }
                  var bStr = "";
                  if (m.Groups.Count > 2 && m.Groups[2].Captures.Count > 0)
                  {
                     bStr = m.Groups[2].Captures[0].Value.Replace(" ", "");
                  }

                  var a = 0;
                  var b = 0;
                  int.TryParse(aStr, out a);
                  int.TryParse(bStr, out b);
                  A = a;
                  B = b;
                  if (B == 0)
                  {
                     Expr = $"{A}n";
                  } else if (B > 0)
                  {
                     Expr = $"{A}n+{B}";
                  }
                  else
                  {
                     Expr = $"{A}n-{Mathf.Abs(B)}";
                  }
               }
               break;

         }
      }

      public override bool Accept(StyleBehaviour element)
      {
         var i = element.transform.GetSiblingIndex() + 1;

         if (A == 0)
         {
            return i == B;
         }

         /*
          * The nth-child uses an expression form of An + B
          * where A and B are defined by the user,
          * where n is some non-negative integer
          *
          * Sequences of n are used to produce the index values that are selectable by the pseudo selector
          * That said, we can derive an equation for the index given n
          * An + B = i
          *
          * Using that, we can back-solve for n
          * n = (i - B) / A
          *
          * Remembering the original constraints, an index value can only satisfy that equation when the resulting n is an integer, and is non-negative
          */
         var n = (i - B) / (float)A;
         var isInteger = Math.Abs(n - Mathf.Floor(n)) < .00001f;

         return isInteger && n >= 0;
      }

      public override string ToString()
      {
         return "nth-child(" + Expr + ")";
      }
   }

   public class PseudoStateSelector : PseudoSelector
   {
      public string PseudoState { get; }

      public PseudoStateSelector(string pseudoState)
      {
         PseudoState = pseudoState;
      }

      public override bool Accept(StyleBehaviour element)
      {
         return element.HasPseudoState(PseudoState);
      }

      public override string ToString()
      {
         return  PseudoState;
      }
   }

   public class PseudoHoverSelector : PseudoStateSelector
   {
      public PseudoHoverSelector() : base("hover")
      {
      }
   }

   public class PseudoActiveSelector : PseudoStateSelector
   {
      public PseudoActiveSelector() : base("active")
      {
      }
   }
}
