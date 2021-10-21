using System;

namespace Beamable.Common
{
    public enum BeamableReflectionSystems
    {
        None = 0,
        
        Content = 1 << 0,
        Microservice = 1 << 1,
        ConsoleCommands = 1 << 2,
        
        
        All = Content | Microservice | ConsoleCommands, 
        
    }
    
    [AttributeUsage(AttributeTargets.Assembly)]
    public class IgnoreFromBeamableAssemblySweepAttribute : Attribute
    {
        public BeamableReflectionSystems LogComplianceFailureAsError { get; }

        public IgnoreFromBeamableAssemblySweepAttribute(BeamableReflectionSystems complianceAsError = BeamableReflectionSystems.All)
        {
            LogComplianceFailureAsError = complianceAsError;
        }
    }
}