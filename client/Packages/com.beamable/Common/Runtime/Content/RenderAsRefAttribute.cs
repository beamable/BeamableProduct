using System;
using UnityEngine;

namespace Beamable.Common.Content
{
   [AttributeUsage(AttributeTargets.Field)]
   public class RenderAsRefAttribute : PropertyAttribute
   {
      public string ContentType { get; }

      public RenderAsRefAttribute(string contentType, int order=1)
      {
         ContentType = contentType;
         base.order = order;
      }
   }

   [AttributeUsage(AttributeTargets.Field)]
   public class RenderAsRef2Attribute : PropertyAttribute
   {
      public string ContentType { get; }

      public RenderAsRef2Attribute(string contentType, int order=1)
      {
         ContentType = contentType;
         base.order = order;
      }
   }
}