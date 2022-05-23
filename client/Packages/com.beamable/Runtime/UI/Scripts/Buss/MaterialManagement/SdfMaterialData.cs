namespace Beamable.UI.Sdf
{
	public struct SdfMaterialData
	{
		public int baseMaterialID;
		//public int mainTextureID;
		public int secondaryTextureID;
		public SdfImage.SdfMode imageMode;
		public SdfShadowMode shadowMode;
		public SdfBackgroundMode backgroundMode;
		public bool isBackgroundTexMain;

		public override bool Equals(object other)
		{
			return other is SdfMaterialData d
				   && d.baseMaterialID == baseMaterialID
				   //&& d.mainTextureID == mainTextureID
				   && d.secondaryTextureID == secondaryTextureID
				   && d.imageMode == imageMode
				   && d.shadowMode == shadowMode
				   && d.backgroundMode == backgroundMode
			       && d.isBackgroundTexMain == isBackgroundTexMain;
		}

		public override int GetHashCode()
		{
			unchecked // Allow arithmetic overflow, numbers will just "wrap around"
			{
				int hashcode = 1430287;
				hashcode = hashcode * 7302013 ^ baseMaterialID.GetHashCode();
				//hashcode = hashcode * 7302013 ^ mainTextureID.GetHashCode();
				hashcode = hashcode * 7302013 ^ secondaryTextureID.GetHashCode();
				hashcode = hashcode * 7302013 ^ imageMode.GetHashCode();
				hashcode = hashcode * 7302013 ^ shadowMode.GetHashCode();
				hashcode = hashcode * 7302013 ^ backgroundMode.GetHashCode();
				hashcode = hashcode * 7302013 ^ isBackgroundTexMain.GetHashCode();
				return hashcode;
			}
		}
	}

	public enum SdfShadowMode
	{
		Default,
		Inner
	}

	public enum SdfBackgroundMode
	{
		Default,
		Outline,
		Full
	}
}
