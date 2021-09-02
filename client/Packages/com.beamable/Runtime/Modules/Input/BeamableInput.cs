namespace Beamable.InputManagerIntegration
{
   public static class BeamableInput
   {
      public static bool IsActionTriggered(InputActionArg arg)
      {
         return arg?.IsTriggered() ?? false;
      }
   }
}