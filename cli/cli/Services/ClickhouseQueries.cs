namespace cli.Services;

public static class ClickhouseQueries
{
	public static readonly string SELECT_TEMPLATE = @"SELECT *, Cid, Pid
																FROM otel_logs
																{0}
																ORDER BY Timestamp DESC
																LIMIT {1};";

	public static readonly string WITHIN_HOURS = @"WHERE Timestamp >= now() - INTERVAL {0} HOUR";
	public static readonly string WITHIN_DAYS = @"WHERE Timestamp >= now() - INTERVAL {0} DAY";
	public static readonly string WITHIN_MINUTES = @"WHERE Timestamp >= now() - INTERVAL {0} MINUTE";
}
