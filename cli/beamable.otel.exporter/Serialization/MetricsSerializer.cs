using OpenTelemetry;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace beamable.otel.exporter.Serialization;

[Serializable]
public class MetricsBatch
{
	public List<SerializableMetric> AllMetrics { get; set; }
	public Dictionary<string, string> ResourceAttributes { get; set; }
	public int SchemaVersion { get; set; }
}

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
	public MetricType MetricType { get; set; }
	public bool IsLongValue { get; set; }

	public List<SerializableMetricPoint> Points { get; set; } = new();
}

[Serializable]
public class SerializableMetricPoint
{
	public DateTime StartTimeUtc { get; set; }
	public TimeSpan StartTimeOffset { get; set; }
	public DateTime EndTimeUtc { get; set; }
	public TimeSpan EndTimeOffset { get; set; }

	public double DoubleValue { get; set; }
	public long LongValue { get; set; }

	public Dictionary<string, string> Tags { get; set; } = new();
}

public static class MetricsSerializer
{
	// These variables are copies of the default values existing in the Otel source code, we don't use this for our metrics, so it's here just to avoid errors
	static readonly double[] DefaultHistogramBoundsLongSeconds = new double[] { 0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 30, 60, 120, 300 };
	private const int DefaultExponentialHistogramMaxBuckets = 160;
	private const int DefaultExponentialHistogramMaxScale = 20;


	private static readonly ConstructorInfo? _metricCtor;
	private static readonly ConstructorInfo? _identityCtor;
	private static readonly ConstructorInfo? _aggStoreCtor;
	private static readonly ConstructorInfo? _tagsCollCtor;
	private static readonly ConstructorInfo? _metricPointCtor;

	private static readonly Type _aggregatorStoreType;

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
		_aggregatorStoreType = asm.GetType("OpenTelemetry.Metrics.AggregatorStore");
		_aggStoreCtor = _aggregatorStoreType?.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
			.FirstOrDefault(c => c.GetParameters().Length >= 4); //TODO change this to check types and make sure it's the constructor we are expecting

		if (_aggStoreCtor == null)
		{
			throw new InvalidOperationException("Constructor of type=[AggregatorStore] not found");
		}

		var mpType = asm.GetType("OpenTelemetry.Metrics.MetricPoint");
		_metricPointCtor = mpType?.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
			.FirstOrDefault(c => c.GetParameters().Length >= 7);

		if (_metricPointCtor == null)
		{
			throw new InvalidOperationException("Constructor of type=[MetricPoint] not found");
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
			Temporality = metric.Temporality.ToString(),
			MetricType = metric.MetricType,
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
				StartTimeOffset = point.StartTime.ToUniversalTime().Offset,
				EndTimeUtc = point.EndTime.ToUniversalTime().UtcDateTime,
				EndTimeOffset = point.EndTime.ToUniversalTime().Offset,
				Tags = tags
			};

			if (metric.MetricType == MetricType.DoubleGauge || metric.MetricType == MetricType.DoubleSum || metric.MetricType == MetricType.DoubleSumNonMonotonic)
			{
				pointData.DoubleValue = point.GetSumDouble();
				metricData.IsLongValue = false;
			}else if (metric.MetricType == MetricType.LongGauge || metric.MetricType == MetricType.LongSum || metric.MetricType == MetricType.LongSumNonMonotonic)
			{
				pointData.LongValue = point.GetSumLong();
				metricData.IsLongValue = true;
			}else
			{
				pointData.DoubleValue = point.GetHistogramSum();
				metricData.IsLongValue = false;
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

		object identity = GetIdentity(serializedMetric.MetricType, serializedMetric.AggregationType,
			serializedMetric.MeterVersion, metricName, unit, description, serializedMetric.IsLongValue);


		List<MetricPoint> metricsPoints = new List<MetricPoint>();
		Type mpType = typeof(MetricPoint);

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

		var startTimeOffset = new DateTimeOffset();
		var endTimeOffset = new DateTimeOffset();

		for (int i = 0; i < serializedMetric.Points.Count; i++)
		{
			KeyValuePair<string, object?>[]? tags = serializedMetric.Points[i].Tags.Select(kv => new KeyValuePair<string, object?>(kv.Key, kv.Value)).ToArray();
			var mpFields = new object[] {
				aggregatorStore,
				aggregationType,
				tags,
				DefaultHistogramBoundsLongSeconds,
				DefaultExponentialHistogramMaxBuckets,
				DefaultExponentialHistogramMaxScale,
				null
			};
			var point = (MetricPoint)_metricPointCtor?.Invoke(mpFields);

			MethodInfo? updateMethodInfo;

			if (serializedMetric.IsLongValue)
			{
				updateMethodInfo = mpType.GetMethod("Update", new Type[] { typeof(long) });
				updateMethodInfo?.Invoke(point, new object[] { serializedMetric.Points[i].LongValue });
			}
			else
			{
				updateMethodInfo = mpType.GetMethod("Update", new Type[] { typeof(double) });
				updateMethodInfo?.Invoke(point, new object[] { serializedMetric.Points[i].DoubleValue });
			}

			startTimeOffset = new DateTimeOffset(serializedMetric.Points[i].StartTimeUtc,
				serializedMetric.Points[i].StartTimeOffset);

			endTimeOffset = new DateTimeOffset(serializedMetric.Points[i].EndTimeUtc,
				serializedMetric.Points[i].EndTimeOffset);

			var aggregatorStoreField = mpType.GetField("aggregatorStore", BindingFlags.NonPublic | BindingFlags.Instance);
			aggregatorStoreField.SetValueDirect(__makeref(point), aggregatorStore);

			metricsPoints.Add(point);
		}

		var metricPointsField = _aggregatorStoreType.GetField("metricPoints", BindingFlags.NonPublic | BindingFlags.Instance);

		if (metricPointsField == null)
		{
			throw new Exception("Could not get field [metricPoints] from the AggregatorStore");
		}

		var metricPointsArray = (Array)metricPointsField.GetValue(aggregatorStore);
		int[] currentMetricPointBatch = new int[metricPointsArray.Length];

		for (int i = 2; i < metricsPoints.Count + 2; i++)
		{
			var point = metricsPoints[i-2];

			metricPointsArray.SetValue(point, i);
			currentMetricPointBatch[i - 2] = i;
		}

		metricPointsField.SetValueDirect(__makeref(aggregatorStore), metricPointsArray);

		var batchSizeField = _aggregatorStoreType.GetField("batchSize", BindingFlags.NonPublic | BindingFlags.Instance);
		batchSizeField.SetValueDirect(__makeref(aggregatorStore), serializedMetric.Points.Count);

		var currentMetricPointBatchField = _aggregatorStoreType.GetField("currentMetricPointBatch", BindingFlags.NonPublic | BindingFlags.Instance);
		currentMetricPointBatchField.SetValueDirect(__makeref(aggregatorStore), currentMetricPointBatch);

		var startTimeProperty = _aggregatorStoreType.GetProperty("StartTimeExclusive", BindingFlags.NonPublic | BindingFlags.Instance);
		startTimeProperty.SetValue(aggregatorStore, startTimeOffset);

		var endTimeProperty = _aggregatorStoreType.GetProperty("EndTimeInclusive", BindingFlags.NonPublic | BindingFlags.Instance);
		endTimeProperty.SetValue(aggregatorStore, endTimeOffset);

		var fields = new object[] {
			identity,
			temporalityType,
			2000,
			null,
			null
		};

		var metric = (Metric)_metricCtor?.Invoke(fields)!;

		var aggregatorField =
			metric.GetType().GetField("AggregatorStore", BindingFlags.NonPublic | BindingFlags.Instance);
		aggregatorField.SetValue(metric, aggregatorStore);


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

	private static object GetIdentity(MetricType type, string aggType, string meterVersion, string metricName, string unit, string description, bool isLong)
	{
		object instrumentIdentity;

		var meter = new Meter(metricName, meterVersion);

		switch ((type, aggType))
		{
			case (MetricType.LongSum, "LongSumIncomingDelta"):
			case (MetricType.LongSum, "LongSumIncomingCumulative"):
				Counter<long> counterLong = meter.CreateCounter<long>(metricName, unit, description);
				instrumentIdentity = _identityCtor?.Invoke(new object[] { counterLong, null });
				break;
			case (MetricType.DoubleSum, "DoubleSumIncomingDelta"):
			case (MetricType.DoubleSum, "DoubleSumIncomingCumulative"):
				Counter<double> counterDouble = meter.CreateCounter<double>(metricName, unit, description);
				instrumentIdentity = _identityCtor?.Invoke(new object[] { counterDouble, null });
				break;
			case (MetricType.LongSumNonMonotonic, "LongSumIncomingCumulative"):
			case (MetricType.LongSumNonMonotonic, "LongSumIncomingDelta"):
				UpDownCounter<long> counterUpDown = meter.CreateUpDownCounter<long>(metricName, unit, description);
				instrumentIdentity = _identityCtor?.Invoke(new object[] { counterUpDown, null });
				break;
			case (MetricType.DoubleSumNonMonotonic, "DoubleSumIncomingDelta"):
			case (MetricType.DoubleSumNonMonotonic, "DoubleSumIncomingCumulative"):
				UpDownCounter<double> counterUpDownDouble = meter.CreateUpDownCounter<double>(metricName, unit, description);
				instrumentIdentity = _identityCtor?.Invoke(new object[] { counterUpDownDouble, null });
				break;
			case (MetricType.DoubleGauge, "DoubleGauge"):
				Gauge<double> gaugeDouble = meter.CreateGauge<double>(metricName, unit, description);
				instrumentIdentity = _identityCtor?.Invoke(new object[] { gaugeDouble, null });
				break;
			case (MetricType.LongGauge, "LongGauge"):
				Gauge<long> gaugeLong = meter.CreateGauge<long>(metricName, unit, description);
				instrumentIdentity = _identityCtor?.Invoke(new object[] { gaugeLong, null });
				break;
			default:
				if (isLong)
				{
					Histogram<long> histogramLong = meter.CreateHistogram<long>(metricName, unit, description);
					instrumentIdentity = _identityCtor?.Invoke(new object[] { histogramLong, null });
				}
				else
				{
					Histogram<double> histogramDouble = meter.CreateHistogram<double>(metricName, unit, description);
					instrumentIdentity = _identityCtor?.Invoke(new object[] { histogramDouble, null });
				}

				break;
		}

		if (instrumentIdentity == null)
		{
			throw new Exception("Couldn't create instrument identity");
		}


		return instrumentIdentity;
	}

}
