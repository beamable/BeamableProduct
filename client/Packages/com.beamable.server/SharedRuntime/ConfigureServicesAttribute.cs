using System;

namespace Beamable.Server
{
   [AttributeUsage(AttributeTargets.Method)]
   public class ConfigureServicesAttribute : Attribute
   {
      public int ExecutionOrder;

      public ConfigureServicesAttribute(int executionOrder = 0)
      {
         ExecutionOrder = executionOrder;
      }
      
   }
}