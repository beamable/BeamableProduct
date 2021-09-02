using System;

namespace Beamable.UI.Buss.Properties
{
   [AttributeUsage(AttributeTargets.Class)]
   public class BussPropertyAttribute : System.Attribute
   {
      public string Name { get; }

      public BussPropertyAttribute(string name)
      {
         Name = name;
      }
   }

   [AttributeUsage(AttributeTargets.Field)]
   public class BussPropertyFieldAttribute : System.Attribute
   {
      public string Name { get; }

      public BussPropertyFieldAttribute(string name)
      {
         Name = name;
      }
   }
}