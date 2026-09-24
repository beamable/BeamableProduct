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
	private static readonly ConstructorInfo? _metricPointCtor;
	private static readonly ConstructorInfo? _histogramBoundsCtor;
	private static readonly FieldInfo _metricPointsField;
	private static readonly FieldInfo _currentMetricPointBatchField;
	private static readonly FieldInfo _batchSizeField;
	private static readonly FieldInfo _aggregatorStoreField;
	private static readonly FieldInfo _aggregationTypeField;
	private static readonly FieldInfo _metricAggregatorField;
	private static readonly PropertyInfo _startTimeProperty;
	private static readonly PropertyInfo _endTimeProperty;

	private static readonly Type _aggregatorStoreType;

	static MetricsSerializer()
	{
		Type? identityType = Type.GetType("OpenTelemetry.Metrics.MetricStreamIdentity, OpenTelemetry");
		if (identityType == null)
			throw new InvalidOperationException("OpenTelemetry internal type [MetricStreamIdentity] not found; telemetry metric serialization is incompatible with this OpenTelemetry version.");
		_identityCtor = identityType?.GetConstructor(new[] { typeof(Instrument), typeof(MetricStreamConfiguration) });

		if (_identityCtor == null)
		{
			throw new InvalidOperationException("Constructor of type=[MetricStreamIdentity] not found");
		}

		Type? metricType = Type.GetType("OpenTelemetry.Metrics.Metric, OpenTelemetry");
		if (metricType == null)
			throw new InvalidOperationException("OpenTelemetry internal type [Metric] not found; telemetry metric serialization is incompatible with this OpenTelemetry version.");

		_metricCtor = metricType?.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
			new[] { identityType!, typeof(AggregationTemporality), typeof(int),
				Type.GetType("System.Nullable`1[[OpenTelemetry.Metrics.ExemplarFilterType, OpenTelemetry]]")!,
				Type.GetType("System.Func`1[[OpenTelemetry.Metrics.ExemplarReservoir, OpenTelemetry]]")! }, null);

		if (_metricCtor == null)
		{
			throw new InvalidOperationException("Constructor of type=[Metric] not found");
		}

		var asm = typeof(Metric).Assembly;
		_aggregatorStoreType = asm.GetType("OpenTelemetry.Metrics.AggregatorStore")
			?? throw new InvalidOperationException("OpenTelemetry internal type [AggregatorStore] not found; telemetry metric serialization is incompatible with this OpenTelemetry version.");
		var aggregationType = asm.GetType("OpenTelemetry.Metrics.AggregationType")
			?? throw new InvalidOperationException("OpenTelemetry internal type [AggregationType] not found; telemetry metric serialization is incompatible with this OpenTelemetry version.");
		_aggStoreCtor = _aggregatorStoreType?.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
			new[] { identityType!, aggregationType, typeof(AggregationTemporality), typeof(int),
				Type.GetType("System.Nullable`1[[OpenTelemetry.Metrics.ExemplarFilterType, OpenTelemetry]]")!,
				Type.GetType("System.Func`1[[OpenTelemetry.Metrics.ExemplarReservoir, OpenTelemetry]]")! }, null);

		if (_aggStoreCtor == null)
		{
			throw new InvalidOperationException("Constructor of type=[AggregatorStore] not found");
		}

		var mpType = asm.GetType("OpenTelemetry.Metrics.MetricPoint")
			?? throw new InvalidOperationException("OpenTelemetry internal type [MetricPoint] not found; telemetry metric serialization is incompatible with this OpenTelemetry version.");
		var histogramBoundsType = asm.GetType("OpenTelemetry.Metrics.HistogramExplicitBounds")
			?? throw new InvalidOperationException("OpenTelemetry internal type [HistogramExplicitBounds] not found; telemetry metric serialization is incompatible with this OpenTelemetry version.");
		_histogramBoundsCtor = histogramBoundsType?.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
			new[] { typeof(double[]), typeof(double[]) }, null);
		_metricPointCtor = mpType?.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
			new[] { _aggregatorStoreType!, aggregationType, typeof(KeyValuePair<string, object?>[]), histogramBoundsType!, typeof(int), typeof(int),
				asm.GetType("OpenTelemetry.Metrics.LookupData")! }, null);

		if (_metricPointCtor == null)
		{
			throw new InvalidOperationException("Constructor of type=[MetricPoint] not found");
		}
		if (_histogramBoundsCtor == null) throw new InvalidOperationException("Constructor of type=[HistogramExplicitBounds] not found");
		_metricPointsField = _aggregatorStoreType.GetField("metricPoints", BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new InvalidOperationException("Field [metricPoints] not found on AggregatorStore");
		_currentMetricPointBatchField = _aggregatorStoreType.GetField("currentMetricPointBatch", BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new InvalidOperationException("Field [currentMetricPointBatch] not found on AggregatorStore");
		_batchSizeField = _aggregatorStoreType.GetField("batchSize", BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new InvalidOperationException("Field [batchSize] not found on AggregatorStore");
		_aggregatorStoreField = mpType.GetField("aggregatorStore", BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new InvalidOperationException("Field [aggregatorStore] not found on MetricPoint");
		_aggregationTypeField = mpType.GetField("aggType", BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new InvalidOperationException("Field [aggType] not found on MetricPoint");
		_metricAggregatorField = metricType.GetField("AggregatorStore", BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new InvalidOperationException("Field [AggregatorStore] not found on Metric");
		_startTimeProperty = _aggregatorStoreType.GetProperty("StartTimeExclusive", BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new InvalidOperationException("Property [StartTimeExclusive] not found on AggregatorStore");
		_endTimeProperty = _aggregatorStoreType.GetProperty("EndTimeInclusive", BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new InvalidOperationException("Property [EndTimeInclusive] not found on AggregatorStore");
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

		metricData.AggregationType = _aggregationTypeField.GetValue(firstPoint)?.ToString() ?? "";

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
				var histogramBounds = _histogramBoundsCtor.Invoke(new object[] { DefaultHistogramBoundsLongSeconds, DefaultHistogramBoundsLongSeconds });
				var mpFields = new object[] {
					aggregatorStore,
					aggregationType,
					tags,
					histogramBounds,
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

			_aggregatorStoreField.SetValueDirect(__makeref(point), aggregatorStore);

			metricsPoints.Add(point);
		}

		var metricPointsArray = (Array)_metricPointsField.GetValue(aggregatorStore)!;
		int[] currentMetricPointBatch = new int[metricPointsArray.Length];

		for (int i = 2; i < metricsPoints.Count + 2; i++)
		{
			var point = metricsPoints[i-2];

			metricPointsArray.SetValue(point, i);
			currentMetricPointBatch[i - 2] = i;
		}

		_metricPointsField.SetValue(aggregatorStore, metricPointsArray);
		_batchSizeField.SetValue(aggregatorStore, serializedMetric.Points.Count);
		_currentMetricPointBatchField.SetValue(aggregatorStore, currentMetricPointBatch);
		_startTimeProperty.SetValue(aggregatorStore, startTimeOffset);
		_endTimeProperty.SetValue(aggregatorStore, endTimeOffset);

		var fields = new object[] {
			identity,
			temporalityType,
			2000,
			null,
			null
		};

		var metric = (Metric)_metricCtor?.Invoke(fields)!;

		_metricAggregatorField.SetValue(metric, aggregatorStore);


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
