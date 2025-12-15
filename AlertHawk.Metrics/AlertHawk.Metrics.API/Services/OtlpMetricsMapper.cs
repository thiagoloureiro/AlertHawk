using AlertHawk.Metrics.API.Models;
using System.Reflection;
using Google.Protobuf;

namespace AlertHawk.Metrics.API.Services;

public class OtlpMetricsMapper
{
    public OtlpMetricsData MapToOtlpMetricsData(IMessage exportMetricsServiceRequest)
    {
        var result = new OtlpMetricsData();
        
        try
        {
            var resourceMetricsProperty = exportMetricsServiceRequest.GetType().GetProperty("ResourceMetrics");
            if (resourceMetricsProperty != null)
            {
                var resourceMetrics = resourceMetricsProperty.GetValue(exportMetricsServiceRequest);
                if (resourceMetrics is System.Collections.IEnumerable enumerable)
                {
                    foreach (var resourceMetric in enumerable)
                    {
                        var otlpResourceMetrics = MapResourceMetrics(resourceMetric);
                        if (otlpResourceMetrics != null)
                        {
                            result.ResourceMetrics.Add(otlpResourceMetrics);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but continue
            Console.WriteLine($"Error mapping OTLP metrics: {ex.Message}");
        }
        
        return result;
    }

    private OtlpResourceMetrics? MapResourceMetrics(object? resourceMetric)
    {
        if (resourceMetric == null) return null;

        var result = new OtlpResourceMetrics();

        // Map Resource
        var resourceProperty = resourceMetric.GetType().GetProperty("Resource");
        if (resourceProperty != null)
        {
            var resource = resourceProperty.GetValue(resourceMetric);
            result.Resource = MapResource(resource);
        }

        // Map ScopeMetrics
        var scopeMetricsProperty = resourceMetric.GetType().GetProperty("ScopeMetrics");
        if (scopeMetricsProperty != null)
        {
            var scopeMetrics = scopeMetricsProperty.GetValue(resourceMetric);
            if (scopeMetrics is System.Collections.IEnumerable scopeEnumerable)
            {
                foreach (var scopeMetric in scopeEnumerable)
                {
                    var otlpScopeMetrics = MapScopeMetrics(scopeMetric);
                    if (otlpScopeMetrics != null)
                    {
                        result.ScopeMetrics.Add(otlpScopeMetrics);
                    }
                }
            }
        }

        return result;
    }

    private OtlpResource MapResource(object? resource)
    {
        var result = new OtlpResource();

        if (resource == null) return result;

        var attributesProperty = resource.GetType().GetProperty("Attributes");
        if (attributesProperty != null)
        {
            var attributes = attributesProperty.GetValue(resource);
            if (attributes is System.Collections.IEnumerable attrEnumerable)
            {
                foreach (var attr in attrEnumerable)
                {
                    var keyProperty = attr?.GetType().GetProperty("Key");
                    var valueProperty = attr?.GetType().GetProperty("Value");
                    if (keyProperty != null && valueProperty != null)
                    {
                        var key = keyProperty.GetValue(attr)?.ToString() ?? string.Empty;
                        var valueObj = valueProperty.GetValue(attr);
                        var value = GetAttributeValue(valueObj);

                        result.Attributes[key] = value;

                        // Extract common attributes
                        switch (key)
                        {
                            case "host.name":
                                result.HostName = value;
                                break;
                            case "k8s.namespace.name":
                                result.Namespace = value;
                                break;
                            case "k8s.pod.name":
                                result.PodName = value;
                                break;
                            case "k8s.pod.uid":
                                result.PodUid = value;
                                break;
                            case "service.name":
                                result.ServiceName = value;
                                break;
                            case "service.version":
                                result.ServiceVersion = value;
                                break;
                        }
                    }
                }
            }
        }

        return result;
    }

    private OtlpScopeMetrics? MapScopeMetrics(object? scopeMetric)
    {
        if (scopeMetric == null) return null;

        var result = new OtlpScopeMetrics();

        // Map Scope
        var scopeProperty = scopeMetric.GetType().GetProperty("Scope");
        if (scopeProperty != null)
        {
            var scope = scopeProperty.GetValue(scopeMetric);
            if (scope != null)
            {
                result.Scope = new OtlpScope
                {
                    Name = scope.GetType().GetProperty("Name")?.GetValue(scope)?.ToString(),
                    Version = scope.GetType().GetProperty("Version")?.GetValue(scope)?.ToString()
                };
            }
        }

        // Map Metrics
        var metricsProperty = scopeMetric.GetType().GetProperty("Metrics");
        if (metricsProperty != null)
        {
            var metrics = metricsProperty.GetValue(scopeMetric);
            if (metrics is System.Collections.IEnumerable metricsEnumerable)
            {
                foreach (var metric in metricsEnumerable)
                {
                    var otlpMetric = MapMetric(metric);
                    if (otlpMetric != null)
                    {
                        result.Metrics.Add(otlpMetric);
                    }
                }
            }
        }

        return result;
    }

    private OtlpMetric? MapMetric(object? metric)
    {
        if (metric == null) return null;

        var result = new OtlpMetric
        {
            Name = metric.GetType().GetProperty("Name")?.GetValue(metric)?.ToString() ?? string.Empty,
            Description = metric.GetType().GetProperty("Description")?.GetValue(metric)?.ToString(),
            Unit = metric.GetType().GetProperty("Unit")?.GetValue(metric)?.ToString()
        };

        // Try Gauge
        var gaugeProperty = metric.GetType().GetProperty("Gauge");
        if (gaugeProperty != null)
        {
            var gauge = gaugeProperty.GetValue(metric);
            if (gauge != null)
            {
                result.Type = OtlpMetricType.Gauge;
                MapNumberDataPoints(gauge, result);
                return result;
            }
        }

        // Try Sum
        var sumProperty = metric.GetType().GetProperty("Sum");
        if (sumProperty != null)
        {
            var sum = sumProperty.GetValue(metric);
            if (sum != null)
            {
                result.Type = OtlpMetricType.Sum;
                result.IsMonotonic = sum.GetType().GetProperty("IsMonotonic")?.GetValue(sum) as bool?;
                var aggTemp = sum.GetType().GetProperty("AggregationTemporality");
                result.AggregationTemporality = aggTemp?.GetValue(sum)?.ToString();
                MapNumberDataPoints(sum, result);
                return result;
            }
        }

        // Try Histogram
        var histogramProperty = metric.GetType().GetProperty("Histogram");
        if (histogramProperty != null)
        {
            var histogram = histogramProperty.GetValue(metric);
            if (histogram != null)
            {
                result.Type = OtlpMetricType.Histogram;
                var aggTemp = histogram.GetType().GetProperty("AggregationTemporality");
                result.AggregationTemporality = aggTemp?.GetValue(histogram)?.ToString();
                MapHistogramDataPoints(histogram, result);
                return result;
            }
        }

        // Try Summary
        var summaryProperty = metric.GetType().GetProperty("Summary");
        if (summaryProperty != null)
        {
            var summary = summaryProperty.GetValue(metric);
            if (summary != null)
            {
                result.Type = OtlpMetricType.Summary;
                MapSummaryDataPoints(summary, result);
                return result;
            }
        }

        return result;
    }

    private void MapNumberDataPoints(object? metricData, OtlpMetric metric)
    {
        if (metricData == null) return;

        var dataPointsProperty = metricData.GetType().GetProperty("DataPoints");
        if (dataPointsProperty != null)
        {
            var dataPoints = dataPointsProperty.GetValue(metricData);
            if (dataPoints is System.Collections.IEnumerable dpEnumerable)
            {
                foreach (var dataPoint in dpEnumerable)
                {
                    var otlpDataPoint = MapNumberDataPoint(dataPoint);
                    if (otlpDataPoint != null)
                    {
                        metric.DataPoints.Add(otlpDataPoint);
                    }
                }
            }
        }
    }

    private OtlpDataPoint? MapNumberDataPoint(object? dataPoint)
    {
        if (dataPoint == null) return null;

        var result = new OtlpDataPoint();

        // Get value
        var valueCaseProperty = dataPoint.GetType().GetProperty("ValueCase");
        if (valueCaseProperty != null)
        {
            var valueCase = valueCaseProperty.GetValue(dataPoint);
            if (valueCase?.ToString()?.Contains("AsInt") == true)
            {
                var asIntProperty = dataPoint.GetType().GetProperty("AsInt");
                if (asIntProperty != null)
                {
                    var intVal = asIntProperty.GetValue(dataPoint);
                    if (intVal != null && long.TryParse(intVal.ToString(), out var parsedInt))
                    {
                        result.IntValue = parsedInt;
                        result.Value = parsedInt;
                    }
                }
            }
            else if (valueCase?.ToString()?.Contains("AsDouble") == true)
            {
                var asDoubleProperty = dataPoint.GetType().GetProperty("AsDouble");
                if (asDoubleProperty != null)
                {
                    var doubleVal = asDoubleProperty.GetValue(dataPoint);
                    if (doubleVal != null && double.TryParse(doubleVal.ToString(), out var parsedDouble))
                    {
                        result.Value = parsedDouble;
                    }
                }
            }
        }

        MapDataPointCommon(dataPoint, result);
        return result;
    }

    private void MapHistogramDataPoints(object? histogram, OtlpMetric metric)
    {
        if (histogram == null) return;

        var dataPointsProperty = histogram.GetType().GetProperty("DataPoints");
        if (dataPointsProperty != null)
        {
            var dataPoints = dataPointsProperty.GetValue(histogram);
            if (dataPoints is System.Collections.IEnumerable dpEnumerable)
            {
                foreach (var dataPoint in dpEnumerable)
                {
                    var otlpDataPoint = MapHistogramDataPoint(dataPoint);
                    if (otlpDataPoint != null)
                    {
                        metric.DataPoints.Add(otlpDataPoint);
                    }
                }
            }
        }
    }

    private OtlpDataPoint? MapHistogramDataPoint(object? dataPoint)
    {
        if (dataPoint == null) return null;

        var result = new OtlpDataPoint();

        var countProperty = dataPoint.GetType().GetProperty("Count");
        var sumProperty = dataPoint.GetType().GetProperty("Sum");
        var bucketCountsProperty = dataPoint.GetType().GetProperty("BucketCounts");
        var explicitBoundsProperty = dataPoint.GetType().GetProperty("ExplicitBounds");

        if (countProperty != null)
        {
            var count = countProperty.GetValue(dataPoint);
            if (count != null && ulong.TryParse(count.ToString(), out var parsedCount))
            {
                result.Count = parsedCount;
            }
        }

        if (sumProperty != null)
        {
            var sum = sumProperty.GetValue(dataPoint);
            if (sum != null && double.TryParse(sum.ToString(), out var parsedSum))
            {
                result.Sum = parsedSum;
            }
        }

        if (bucketCountsProperty != null)
        {
            var bucketCounts = bucketCountsProperty.GetValue(dataPoint);
            if (bucketCounts is System.Collections.IEnumerable bucketEnumerable)
            {
                result.BucketCounts = new List<ulong>();
                foreach (var bucket in bucketEnumerable)
                {
                    if (bucket != null && ulong.TryParse(bucket.ToString(), out var parsedBucket))
                    {
                        result.BucketCounts.Add(parsedBucket);
                    }
                }
            }
        }

        if (explicitBoundsProperty != null)
        {
            var explicitBounds = explicitBoundsProperty.GetValue(dataPoint);
            if (explicitBounds is System.Collections.IEnumerable boundsEnumerable)
            {
                result.ExplicitBounds = new List<double>();
                foreach (var bound in boundsEnumerable)
                {
                    if (bound != null && double.TryParse(bound.ToString(), out var parsedBound))
                    {
                        result.ExplicitBounds.Add(parsedBound);
                    }
                }
            }
        }

        MapDataPointCommon(dataPoint, result);
        return result;
    }

    private void MapSummaryDataPoints(object? summary, OtlpMetric metric)
    {
        if (summary == null) return;

        var dataPointsProperty = summary.GetType().GetProperty("DataPoints");
        if (dataPointsProperty != null)
        {
            var dataPoints = dataPointsProperty.GetValue(summary);
            if (dataPoints is System.Collections.IEnumerable dpEnumerable)
            {
                foreach (var dataPoint in dpEnumerable)
                {
                    var otlpDataPoint = MapSummaryDataPoint(dataPoint);
                    if (otlpDataPoint != null)
                    {
                        metric.DataPoints.Add(otlpDataPoint);
                    }
                }
            }
        }
    }

    private OtlpDataPoint? MapSummaryDataPoint(object? dataPoint)
    {
        if (dataPoint == null) return null;

        var result = new OtlpDataPoint();

        var countProperty = dataPoint.GetType().GetProperty("Count");
        var sumProperty = dataPoint.GetType().GetProperty("Sum");
        var quantileValuesProperty = dataPoint.GetType().GetProperty("QuantileValues");

        if (countProperty != null)
        {
            var count = countProperty.GetValue(dataPoint);
            if (count != null && ulong.TryParse(count.ToString(), out var parsedCount))
            {
                result.Count = parsedCount;
            }
        }

        if (sumProperty != null)
        {
            var sum = sumProperty.GetValue(dataPoint);
            if (sum != null && double.TryParse(sum.ToString(), out var parsedSum))
            {
                result.Sum = parsedSum;
            }
        }

        if (quantileValuesProperty != null)
        {
            var quantileValues = quantileValuesProperty.GetValue(dataPoint);
            if (quantileValues is System.Collections.IEnumerable quantileEnumerable)
            {
                result.QuantileValues = new List<OtlpQuantileValue>();
                foreach (var quantile in quantileEnumerable)
                {
                    if (quantile != null)
                    {
                        var quantileProperty = quantile.GetType().GetProperty("Quantile");
                        var valueProperty = quantile.GetType().GetProperty("Value");
                        
                        if (quantileProperty != null && valueProperty != null)
                        {
                            var quantileVal = quantileProperty.GetValue(quantile);
                            var value = valueProperty.GetValue(quantile);
                            
                            if (quantileVal != null && value != null &&
                                double.TryParse(quantileVal.ToString(), out var parsedQuantile) &&
                                double.TryParse(value.ToString(), out var parsedValue))
                            {
                                result.QuantileValues.Add(new OtlpQuantileValue
                                {
                                    Quantile = parsedQuantile,
                                    Value = parsedValue
                                });
                            }
                        }
                    }
                }
            }
        }

        MapDataPointCommon(dataPoint, result);
        return result;
    }

    private void MapDataPointCommon(object? dataPoint, OtlpDataPoint result)
    {
        if (dataPoint == null) return;

        // Map attributes
        var attributesProperty = dataPoint.GetType().GetProperty("Attributes");
        if (attributesProperty != null)
        {
            var attributes = attributesProperty.GetValue(dataPoint);
            if (attributes is System.Collections.IEnumerable attrEnumerable)
            {
                foreach (var attr in attrEnumerable)
                {
                    var keyProperty = attr?.GetType().GetProperty("Key");
                    var valueProperty = attr?.GetType().GetProperty("Value");
                    if (keyProperty != null && valueProperty != null)
                    {
                        var key = keyProperty.GetValue(attr)?.ToString() ?? string.Empty;
                        var valueObj = valueProperty.GetValue(attr);
                        var value = GetAttributeValue(valueObj);
                        result.Attributes[key] = value;
                    }
                }
            }
        }

        // Map timestamp
        var timeUnixNanoProperty = dataPoint.GetType().GetProperty("TimeUnixNano");
        if (timeUnixNanoProperty != null)
        {
            var timeUnixNano = timeUnixNanoProperty.GetValue(dataPoint);
            if (timeUnixNano != null)
            {
                if (timeUnixNano is ulong nanoTime && nanoTime > 0)
                {
                    result.Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)(nanoTime / 1_000_000)).DateTime;
                }
                else if (ulong.TryParse(timeUnixNano.ToString(), out var parsedNano) && parsedNano > 0)
                {
                    result.Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)(parsedNano / 1_000_000)).DateTime;
                }
            }
        }
    }

    private string GetAttributeValue(object? valueObj)
    {
        if (valueObj == null) return "N/A";

        var stringValueProperty = valueObj.GetType().GetProperty("StringValue");
        var intValueProperty = valueObj.GetType().GetProperty("IntValue");
        var doubleValueProperty = valueObj.GetType().GetProperty("DoubleValue");
        var boolValueProperty = valueObj.GetType().GetProperty("BoolValue");

        if (stringValueProperty != null)
        {
            var stringValue = stringValueProperty.GetValue(valueObj);
            if (stringValue != null) return stringValue.ToString() ?? "N/A";
        }

        if (intValueProperty != null)
        {
            var intValue = intValueProperty.GetValue(valueObj);
            if (intValue != null) return intValue.ToString() ?? "N/A";
        }

        if (doubleValueProperty != null)
        {
            var doubleValue = doubleValueProperty.GetValue(valueObj);
            if (doubleValue != null) return doubleValue.ToString() ?? "N/A";
        }

        if (boolValueProperty != null)
        {
            var boolValue = boolValueProperty.GetValue(valueObj);
            if (boolValue != null) return boolValue.ToString() ?? "N/A";
        }

        return valueObj.ToString() ?? "N/A";
    }
}
