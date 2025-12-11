using System;

namespace Beamable
{
	
	public enum BeamSemanticType
	{
		Cid,
		Pid,
		AccountId,
		GamerTag,
		ContentManifestId,
		ContentId,
		StatsType,
		ServiceName,
	}
	
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Class | AttributeTargets.Struct)]
	public class BeamSemanticTypeAttribute : Attribute
	{
		public string SemanticType { get; }

		public BeamSemanticTypeAttribute(BeamSemanticType semanticType)
		{
			SemanticType = semanticType.ToString();
		}
		
		public BeamSemanticTypeAttribute(string customSemanticType)
		{
			SemanticType = customSemanticType;
		}
	}
}
