namespace cli.Services;

public static class ClickhouseQueries
{
	public static readonly string SELECT_TEMPLATE = @"SELECT * FROM otel_logs {0} ORDER BY Timestamp {1} LIMIT {2};";
	public static readonly string AND_OPERATION = "AND";
	public static readonly string WHERE_OPERATION = "WHERE";

	public static readonly string WITHIN_HOURS = @"Timestamp >= now() - INTERVAL {0} HOUR";
	public static readonly string WITHIN_DAYS = @"Timestamp >= now() - INTERVAL {0} DAY";
	public static readonly string WITHIN_MINUTES = @"Timestamp >= now() - INTERVAL {0} MINUTE";
	public static readonly string SERVICE_NAME_PARTIAL_MATCH = @"ServiceName LIKE '%{0}%'";
	public static readonly string SERVICE_NAME_FULL_MATCH = @"ServiceName == '{0}'";
	public static readonly string LOG_LEVEL_MATCH = @"SeverityText == '{0}'";
	public static readonly string BODY_PARTIAL_MATCH = @"Body LIKE '%{0}%'";
	public static readonly string BODY_FULL_MATCH = @"Body == '{0}'";

}
