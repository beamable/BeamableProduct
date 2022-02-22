using System;
using System.Collections.Generic;

namespace Beamable.Common.Api.CloudData
{
	[Serializable]
	public class GetCloudDataManifestResponse
	{
		public string result;
		public List<CloudMetaData> meta;
	}

	[Serializable]
	public class CloudMetaData
	{
		public long sid;
		public long version;
		public string @ref;
		public string uri;
		public CohortEntry cohort;

		public bool IsDefault => string.IsNullOrEmpty(cohort?.trial) && string.IsNullOrEmpty(cohort?.cohort);
	}

	[Serializable]
	public class CohortEntry
	{
		public string trial;
		public string cohort;
	}

	/// <summary>
	/// This type defines the %Client main entry point for the %A/B %Testing feature.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/abtesting-feature">A/B Testing</a> feature documentation
	/// - See Beamable.API script reference
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	public class CloudDataApi : ICloudDataApi
	{
		public IUserContext Ctx { get; }
		public IBeamableRequester Requester { get; }

		public CloudDataApi(IUserContext ctx, IBeamableRequester requester)
		{
			Ctx = ctx;
			Requester = requester;
		}

		public Promise<GetCloudDataManifestResponse> GetGameManifest()
		{
			return Requester.Request<GetCloudDataManifestResponse>(
			   Method.GET,
			   "/basic/cloud/meta"
			);
		}

		public Promise<GetCloudDataManifestResponse> GetPlayerManifest()
		{
			return Requester.Request<GetCloudDataManifestResponse>(
			   Method.GET,
			   $"/basic/cloud/meta/player/all"
			);
		}
	}
}
