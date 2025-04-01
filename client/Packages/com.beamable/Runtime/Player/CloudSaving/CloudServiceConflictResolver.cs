using System.Collections.Generic;

namespace Beamable.Player.CloudSaving
{
	public class CloudServiceConflictResolver : IConflictResolver
	{
		private readonly ICloudSavingService _cloudSavingService;

		public CloudServiceConflictResolver(ICloudSavingService cloudSavingService)
		{
			_cloudSavingService = cloudSavingService;
		}

		public IReadOnlyList<DataConflictDetail> Conflicts => _cloudSavingService.GetDataConflictDetails();

		public void Resolve(DataConflictDetail conflictDetail, ConflictResolveType resolveType) =>
			_cloudSavingService.ResolveDataConflict(conflictDetail, resolveType);
	}
}
