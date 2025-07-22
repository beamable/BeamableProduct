using OpenTelemetry.Metrics;
using System.Reflection;

namespace beamable.otel.exporter.Serialization;

[Serializable]
public class SerializableMetric
{
	public string Name { get; set; }
	public string Description { get; set; }
	public string Unit { get; set; }
	public string MeterName { get; set; }
	public string MeterVersion { get; set; }
	public string AggregationType { get; set; } = default!;

	public List<SerializableMetricPoint> Points { get; set; } = new();
}

[Serializable]
public class SerializableMetricPoint
{
	public DateTime StartTimeUtc { get; set; }
	public DateTime EndTimeUtc { get; set; }

	public MetricType MetricType { get; set; }

	public double DoubleValue { get; set; }
	public long LongValue { get; set; }

	public Dictionary<string, string> Tags { get; set; } = new();
}

public static class MetricsSerializer
{
	private static readonly ConstructorInfo? _metricCtor;

	static MetricsSerializer()
	{
		var constructors = typeof(Metric)
			.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);

		foreach (var con in constructors)
		{
			//TODO change this for an assertion to make sure the constructor that we got is the right one
			if (con.GetParameters().Length == 5) //This is to make sure we get the correct constructor
			{
				_metricCtor = con;
			}
		}

		if (_metricCtor == null)
			throw new InvalidOperationException("Constructor of type=[Metric] not found");
	}

	public static SerializableMetric SerializeMetric(Metric metric)
	{
		var metricData = new SerializableMetric()
		{
			Name	= metric.Name,
			Description = metric.Description,
			Unit = metric.Unit,
			MeterName = metric.MeterName,
			MeterVersion = metric.MeterVersion
		};

		var points = metric.GetMetricPoints();

		foreach (MetricPoint point in points)
		{
			var tags = new Dictionary<string, string>();
			foreach (KeyValuePair<string,object?> tag in point.Tags)
			{
				tags.Add(tag.Key, tag.Value?.ToString() ?? "");
			}

			var pointData = new SerializableMetricPoint()
			{
				StartTimeUtc = point.StartTime.ToUniversalTime().UtcDateTime,
				EndTimeUtc = point.EndTime.ToUniversalTime().UtcDateTime,
				MetricType = metric.MetricType,
				Tags = tags
			};

			if (metric.MetricType == MetricType.DoubleGauge || metric.MetricType == MetricType.DoubleSum)
			{
				pointData.DoubleValue = point.GetSumDouble();
			}else if (metric.MetricType == MetricType.LongGauge || metric.MetricType == MetricType.LongSum)
			{
				pointData.LongValue = point.GetSumLong();
			}else if (metric.MetricType == MetricType.Histogram)
			{
				pointData.DoubleValue = point.GetHistogramSum();
			}

			metricData.Points.Add(pointData);
		}

		MetricPoint firstPoint = new MetricPoint();

		foreach (MetricPoint point in points)
		{
			firstPoint = point;
			break;
		}

		var field = typeof(MetricPoint).GetField("aggType", BindingFlags.NonPublic | BindingFlags.Instance);
		if (field != null)
		{
			metricData.AggregationType = field.GetValue(firstPoint)?.ToString() ?? "";
		}

		return metricData;
	}

	public static Metric DeserializeMetric(SerializableMetric serializedMetric)
	{
		var metricName = serializedMetric.Name;
		var description = serializedMetric.Description;
		var unit = serializedMetric.Unit;
		var aggregationType = ParseAggregationType(serializedMetric.AggregationType);


		Span<MetricPoint> pointsSpan = new MetricPoint[serializedMetric.Points.Count];
		Type mpType = typeof(MetricPoint);

		for (int i = 0; i < serializedMetric.Points.Count; i++)
		{
			ref MetricPoint point = ref pointsSpan[i];
			point = new MetricPoint();

			var aggField = mpType.GetField("aggType", BindingFlags.NonPublic | BindingFlags.Instance);
			aggField?.SetValueDirect(__makeref(point), aggregationType);

			var startTimeField = mpType.GetField("startTime", BindingFlags.NonPublic | BindingFlags.Instance);
			startTimeField?.SetValueDirect(__makeref(point), serializedMetric.Points[i].StartTimeUtc);

			var endTimeField = mpType.GetField("endTime", BindingFlags.NonPublic | BindingFlags.Instance);
			endTimeField?.SetValueDirect(__makeref(point), serializedMetric.Points[i].EndTimeUtc);

			FieldInfo? runningValueField = typeof(MetricPoint).GetField("runningValue", BindingFlags.NonPublic | BindingFlags.Instance);
			if (runningValueField == null)
			{
				throw new Exception("Cannot find runningValue field");
			}

			Type valueStorageType = runningValueField.FieldType;

			object? runningValueInstance = Activator.CreateInstance(valueStorageType);

			if (runningValueInstance == null)
			{
				throw new Exception("Could not create MetricPointValueStorage");
			}

			var metricType = serializedMetric.Points[i].MetricType;
			if (metricType == MetricType.DoubleGauge || metricType == MetricType.DoubleSum || metricType == MetricType.Histogram)
			{
				FieldInfo sumDoubleField = valueStorageType.GetField("AsDouble", BindingFlags.NonPublic | BindingFlags.Instance);

				if (sumDoubleField == null)
				{
					throw new Exception("Could not set AsDouble field");
				}

				sumDoubleField.SetValue(runningValueInstance, serializedMetric.Points[i].DoubleValue);
			}
			else if (metricType == MetricType.LongGauge || metricType == MetricType.LongSum)
			{
				FieldInfo sumLongField = valueStorageType.GetField("AsLong", BindingFlags.NonPublic | BindingFlags.Instance);

				if (sumLongField == null)
				{
					throw new Exception("Could not set AsDouble field");
				}

				sumLongField.SetValue(runningValueInstance, serializedMetric.Points[i].LongValue);
			}
		}

		var fields = new object[]
		{
			metricName,
			description,
			unit,
			aggregationType,
			null //pointsSpan
		};

		var metric = (Metric)_metricCtor.Invoke(fields);

		var meterNameProp = metric.GetType().GetProperty("MeterName", BindingFlags.NonPublic | BindingFlags.Instance);
		if (meterNameProp != null && meterNameProp.CanWrite)
		{
			meterNameProp.SetValue(metric, serializedMetric.MeterName);
		}

		var meterVersionProp = metric.GetType().GetProperty("MeterVersion", BindingFlags.NonPublic | BindingFlags.Instance);
		if (meterVersionProp != null && meterVersionProp.CanWrite)
		{
			meterVersionProp.SetValue(metric, serializedMetric.MeterVersion);
		}

		return metric;
	}

	private static object ParseAggregationType(string serializedAggType)
	{
		Assembly otelAssembly = typeof(MetricPoint).Assembly;

		Type? aggregationTypeEnum = otelAssembly.GetType("OpenTelemetry.Metrics.AggregationType");

		if (aggregationTypeEnum == null)
		{
			throw new InvalidOperationException("AggregationType enum not found.");
		}

		return Enum.Parse(aggregationTypeEnum, serializedAggType);
	}

}
