using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.Serialization;

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
	public string Temporality { get; set; }

	public List<SerializableMetricPoint> Points { get; set; } = new();
}

[Serializable]
public class SerializableMetricPoint
{
	public DateTime StartTimeUtc { get; set; } //TODO need to push this back to be a DateTimeOffset
	public DateTime EndTimeUtc { get; set; }

	public MetricType MetricType { get; set; }

	public double DoubleValue { get; set; }
	public long LongValue { get; set; }

	public Dictionary<string, string> Tags { get; set; } = new();
}

public static class MetricsSerializer
{
	private static readonly ConstructorInfo? _metricCtor;
	private static readonly ConstructorInfo? _identityCtor;
	private static readonly ConstructorInfo? _aggStoreCtor;

	static MetricsSerializer()
	{
		Type? identityType = Type.GetType("OpenTelemetry.Metrics.MetricStreamIdentity, OpenTelemetry");
		_identityCtor = identityType?.GetConstructor(new[] { typeof(Instrument), typeof(MetricStreamConfiguration) });

		if (_identityCtor == null)
		{
			throw new InvalidOperationException("Constructor of type=[MetricStreamIdentity] not found");
		}

		Type? metricType = Type.GetType("OpenTelemetry.Metrics.Metric, OpenTelemetry");

		_metricCtor = metricType
			?.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
			.FirstOrDefault(c =>
			{
				var parameters = c.GetParameters();
				return parameters.Length == 5 &&
				       parameters[0].ParameterType.FullName == "OpenTelemetry.Metrics.MetricStreamIdentity" &&
				       parameters[1].ParameterType == typeof(AggregationTemporality) &&
				       parameters[2].ParameterType == typeof(int);
			});

		if (_metricCtor == null)
		{
			throw new InvalidOperationException("Constructor of type=[Metric] not found");
		}

		var asm = typeof(Metric).Assembly;
		var aggregatorStoreType = asm.GetType("OpenTelemetry.Metrics.AggregatorStore");
		_aggStoreCtor = aggregatorStoreType?.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
			.FirstOrDefault(c => c.GetParameters().Length >= 4);

		if (_aggStoreCtor == null)
		{
			throw new InvalidOperationException("Constructor of type=[AggregatorStore] not found");
		}
	}

	public static SerializableMetric SerializeMetric(Metric metric)
	{
		var metricData = new SerializableMetric()
		{
			Name	= metric.Name,
			Description = metric.Description,
			Unit = metric.Unit,
			MeterName = metric.MeterName,
			MeterVersion = metric.MeterVersion,
			Temporality = metric.Temporality.ToString()
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
		var temporalityType = ParseTemporalityType(serializedMetric.Temporality);

		var meter = new Meter(metricName, serializedMetric.MeterVersion);
		var counter = meter.CreateCounter<long>(metricName, unit, description);
		object identity = _identityCtor?.Invoke(new object[] { counter, null });

		List<MetricPoint> metricsPoints = new List<MetricPoint>();
		Type mpType = typeof(MetricPoint);

		for (int i = 0; i < serializedMetric.Points.Count; i++)
		{
			var point = new MetricPoint();

			var aggField = mpType.GetField("aggType", BindingFlags.NonPublic | BindingFlags.Instance);
			aggField?.SetValueDirect(__makeref(point), aggregationType);

			var startTimeField = mpType.GetField("startTime", BindingFlags.NonPublic | BindingFlags.Instance);
			startTimeField?.SetValueDirect(__makeref(point), serializedMetric.Points[i].StartTimeUtc);

			var endTimeField = mpType.GetField("endTime", BindingFlags.NonPublic | BindingFlags.Instance);
			endTimeField?.SetValueDirect(__makeref(point), serializedMetric.Points[i].EndTimeUtc);

			var runningValueField = mpType.GetField("runningValue", BindingFlags.NonPublic | BindingFlags.Instance);

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
				FieldInfo sumDoubleField = valueStorageType.GetField("AsDouble", BindingFlags.Public | BindingFlags.Instance);

				if (sumDoubleField == null)
				{
					throw new Exception("Could not set AsDouble field");
				}

				sumDoubleField.SetValue(runningValueInstance, serializedMetric.Points[i].DoubleValue);
			}
			else if (metricType == MetricType.LongGauge || metricType == MetricType.LongSum)
			{
				FieldInfo sumLongField = valueStorageType.GetField("AsLong", BindingFlags.Public | BindingFlags.Instance);

				if (sumLongField == null)
				{
					throw new Exception("Could not set AsDouble field");
				}

				sumLongField.SetValue(runningValueInstance, serializedMetric.Points[i].LongValue);
			}

			runningValueField.SetValue(point, runningValueInstance);

			metricsPoints.Add(point);
		}

		var fields = new object[] {
			identity,
			temporalityType,
			2000, //TODO not sure what this number means, using random one for now
			null,
			null
		};

		var metric = (Metric)_metricCtor?.Invoke(fields)!;

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

		var aggregatorStore = _aggStoreCtor?.Invoke(new object[] {
			identity,
			aggregationType,
			temporalityType,
			2000,
			null,
			null
		});

		if (aggregatorStore == null)
		{
			throw new Exception("Could not create a new AggregatorStore");
		}

		var aggregatorStoreType = aggregatorStore?.GetType();
		var metricPointsField = aggregatorStoreType?.GetField("metricPoints", BindingFlags.NonPublic | BindingFlags.Instance);

		if (metricPointsField == null)
		{
			throw new Exception("Could not get field [metricPoints] from the AggregatorStore");
		}

		var metricPointsArray = (Array)metricPointsField.GetValue(aggregatorStore);

		for (int i = 0; i < metricsPoints.Count; i++)
		{
			var point = metricsPoints[i];

			metricPointsArray.SetValue(point, i);
		}

		metricPointsField.SetValue(aggregatorStore, metricPointsArray);

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

	private static object ParseTemporalityType(string serializedTemporalityType)
	{
		Assembly otelAssembly = typeof(MetricPoint).Assembly;

		Type? temporalityEnum = otelAssembly.GetType("OpenTelemetry.Metrics.AggregationTemporality");

		if (temporalityEnum == null)
		{
			throw new InvalidOperationException("AggregationTemporality enum not found.");
		}

		return Enum.Parse(temporalityEnum, serializedTemporalityType);
	}

}
