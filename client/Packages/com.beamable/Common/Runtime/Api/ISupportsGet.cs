namespace Beamable.Common.Api
{
	/// <summary>
	/// This type defines getting fresh data from an %Api data source (e.g. %Service).
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See Beamable.Api script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public interface ISupportsGet<TData>
	{
		Promise<TData> GetCurrent(string scope = "");
	}

	/// <summary>
	/// This type defines getting fresh data from an %Api data source (e.g. %Service).
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See Beamable.Api script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public interface ISupportGetLatest<out TData>
	{
		TData GetLatest(string scope = "");
	}

	/// <summary>
	/// This type defines getting %Api data source.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See Beamable.Api script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class BeamableGetApiResource<ScopedRsp>
	{
		public Promise<ScopedRsp> RequestData(IBeamableRequester requester,
											  IUserContext ctx,
											  string serviceName,
											  string scope)
		{
			return RequestData(requester, CreateRefreshUrl(ctx, serviceName, scope));
		}

		public Promise<ScopedRsp> RequestData(IBeamableRequester requester, string url)
		{
			return requester.Request<ScopedRsp>(Method.GET, url);
		}

		public string CreateRefreshUrl(IUserContext ctx, string serviceName, string scope)
		{
			var queryArgs = "";
			if (!string.IsNullOrEmpty(scope))
			{
				queryArgs = $"?scope={scope}";
			}

			return $"/object/{serviceName}/{ctx.UserId}{queryArgs}";
		}
	}
}
