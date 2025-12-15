using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Text;
using System.Linq;
using System.IO.Compression;
using System.Reflection;
using Google.Protobuf;

namespace AlertHawk.Metrics.API.Controllers
{
    [ApiController]
    [Route("otlp/v1/metrics")]
    public class OtlpMetricsController : ControllerBase
    {
        private readonly ILogger<OtlpMetricsController> _logger;

        public OtlpMetricsController(ILogger<OtlpMetricsController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Receive OTLP metrics
        /// </summary>
        /// <returns>Success status</returns>
        [HttpPost]
        [SwaggerOperation(Summary = "Receive OTLP metrics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ReceiveMetrics()
        {
            try
            {
                // Log request headers
                Console.WriteLine("=== OTLP Metrics Received ===");
                Console.WriteLine($"Content-Type: {Request.ContentType}");
                Console.WriteLine($"Content-Length: {Request.ContentLength}");
                Console.WriteLine($"Method: {Request.Method}");
                Console.WriteLine($"Path: {Request.Path}");

                // Enable buffering to allow reading the body multiple times if needed
                Request.EnableBuffering();

                // Read the raw request body
                Request.Body.Position = 0;
                using var memoryStream = new MemoryStream();
                await Request.Body.CopyToAsync(memoryStream);
                var bodyBytes = memoryStream.ToArray();
                Request.Body.Position = 0; // Reset for potential future reads

                // Check if content is gzip compressed
                var isGzipCompressed = Request.Headers.ContainsKey("Content-Encoding") &&
                                      Request.Headers["Content-Encoding"].ToString().Contains("gzip");

                byte[] decompressedBytes = bodyBytes;
                if (isGzipCompressed)
                {
                    Console.WriteLine("Decompressing gzip content...");
                    using var gzipStream = new GZipStream(new MemoryStream(bodyBytes), CompressionMode.Decompress);
                    using var decompressedStream = new MemoryStream();
                    await gzipStream.CopyToAsync(decompressedStream);
                    decompressedBytes = decompressedStream.ToArray();
                    Console.WriteLine($"Decompressed from {bodyBytes.Length} bytes to {decompressedBytes.Length} bytes");
                }

                // Parse OTLP protobuf
                var isProtobuf = Request.ContentType?.Contains("application/x-protobuf") == true ||
                                Request.ContentType?.Contains("application/protobuf") == true ||
                                Request.ContentType?.Contains("application/x-otlp") == true;

                if (isProtobuf)
                {
                    // Parse OTLP protobuf using reflection to access internal types
                    IMessage? exportMetricsServiceRequest = null;
                    try
                    {
                        // Find the OpenTelemetry.Proto assembly
                        var protoAssembly = AppDomain.CurrentDomain.GetAssemblies()
                            .FirstOrDefault(a => a.GetName().Name?.Contains("OpenTelemetry.Proto") == true ||
                                                 a.GetTypes().Any(t => t.Namespace?.Contains("OpenTelemetry.Proto.Collector.Metrics") == true));

                        if (protoAssembly == null)
                        {
                            // Try to load it explicitly
                            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                            protoAssembly = assemblies.FirstOrDefault(a => 
                                a.GetTypes().Any(t => t.FullName?.Contains("OpenTelemetry.Proto.Collector.Metrics.V1.ExportMetricsServiceRequest") == true));
                        }

                        if (protoAssembly != null)
                        {
                            // Get the ExportMetricsServiceRequest type
                            var requestType = protoAssembly.GetTypes()
                                .FirstOrDefault(t => t.Name == "ExportMetricsServiceRequest" && 
                                                     t.Namespace?.Contains("Collector.Metrics") == true);

                            if (requestType != null)
                            {
                                // Get the Parser property
                                var parserProperty = requestType.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
                                if (parserProperty != null)
                                {
                                    var parser = parserProperty.GetValue(null);
                                    var parseMethod = parser?.GetType().GetMethod("ParseFrom", new[] { typeof(byte[]) });
                                    if (parseMethod != null)
                                    {
                                        exportMetricsServiceRequest = parseMethod.Invoke(parser, new object[] { decompressedBytes }) as IMessage;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception reflectionEx)
                    {
                        Console.WriteLine($"Error using reflection to parse: {reflectionEx.Message}");
                    }

                    if (exportMetricsServiceRequest != null)
                    {
                        // Use reflection to access properties and parse metrics
                        ParseMetricsUsingReflection(exportMetricsServiceRequest);
                    }
                    else
                    {
                        Console.WriteLine("\n--- OTLP Protobuf Metrics (Could not parse) ---");
                        Console.WriteLine("Note: Proto classes are internal. Showing raw data.");
                        Console.WriteLine($"Protobuf data size: {decompressedBytes.Length} bytes");
                        Console.WriteLine("\nRaw protobuf data (Base64):");
                        Console.WriteLine(Convert.ToBase64String(decompressedBytes));
                        Console.WriteLine("\nRaw protobuf data (Hex - first 200 bytes):");
                        var hexBytes = decompressedBytes.Take(200).ToArray();
                        Console.WriteLine(BitConverter.ToString(hexBytes).Replace("-", " "));
                    }
                }
                else
                {
                    // For JSON or other text formats, read as string
                    var body = Encoding.UTF8.GetString(decompressedBytes);
                    Console.WriteLine("\n--- Request Body (Text) ---");
                    Console.WriteLine(body);
                }

                Console.WriteLine("\n=== End OTLP Metrics ===\n");

                _logger.LogInformation("OTLP metrics received and parsed successfully. Content-Type: {ContentType}, Body Length: {BodyLength}",
                    Request.ContentType, bodyBytes.Length);

                return Ok(new { success = true, message = "Metrics received and parsed" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving OTLP metrics: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                _logger.LogError(ex, "Error receiving OTLP metrics");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private void ParseMetricsUsingReflection(IMessage exportMetricsServiceRequest)
        {
            try
            {
                Console.WriteLine("\n--- Parsed OTLP Metrics ---");
                
                // Get ResourceMetrics property
                var resourceMetricsProperty = exportMetricsServiceRequest.GetType().GetProperty("ResourceMetrics");
                if (resourceMetricsProperty != null)
                {
                    var resourceMetrics = resourceMetricsProperty.GetValue(exportMetricsServiceRequest);
                    if (resourceMetrics != null)
                    {
                        var countProperty = resourceMetrics.GetType().GetProperty("Count");
                        var count = countProperty?.GetValue(resourceMetrics);
                        Console.WriteLine($"Resource Metrics Count: {count}");

                        // Try to iterate through ResourceMetrics
                        if (resourceMetrics is System.Collections.IEnumerable enumerable)
                        {
                            int index = 0;
                            foreach (var resourceMetric in enumerable)
                            {
                                Console.WriteLine($"\n--- Resource {index + 1} ---");
                                
                                // Get Resource property
                                var resourceProperty = resourceMetric?.GetType().GetProperty("Resource");
                                if (resourceProperty != null)
                                {
                                    var resource = resourceProperty.GetValue(resourceMetric);
                                    var attributesProperty = resource?.GetType().GetProperty("Attributes");
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
                                                    var key = keyProperty.GetValue(attr)?.ToString() ?? "N/A";
                                                    var valueObj = valueProperty.GetValue(attr);
                                                    var value = GetAttributeValue(valueObj);
                                                    Console.WriteLine($"  Attribute: {key} = {value}");
                                                }
                                            }
                                        }
                                    }
                                }

                                // Get ScopeMetrics property
                                var scopeMetricsProperty = resourceMetric?.GetType().GetProperty("ScopeMetrics");
                                if (scopeMetricsProperty != null)
                                {
                                    var scopeMetrics = scopeMetricsProperty.GetValue(resourceMetric);
                                    if (scopeMetrics is System.Collections.IEnumerable scopeEnumerable)
                                    {
                                        int scopeIndex = 0;
                                        foreach (var scopeMetric in scopeEnumerable)
                                        {
                                            var scopeProperty = scopeMetric?.GetType().GetProperty("Scope");
                                            var scope = scopeProperty?.GetValue(scopeMetric);
                                            var scopeNameProperty = scope?.GetType().GetProperty("Name");
                                            var scopeVersionProperty = scope?.GetType().GetProperty("Version");
                                            var scopeName = scopeNameProperty?.GetValue(scope)?.ToString() ?? "Unknown";
                                            var scopeVersion = scopeVersionProperty?.GetValue(scope)?.ToString() ?? "N/A";
                                            
                                            Console.WriteLine($"\n  --- Scope {scopeIndex + 1}: {scopeName} ---");
                                            Console.WriteLine($"  Version: {scopeVersion}");

                                            var metricsProperty = scopeMetric?.GetType().GetProperty("Metrics");
                                            if (metricsProperty != null)
                                            {
                                                var metrics = metricsProperty.GetValue(scopeMetric);
                                                if (metrics is System.Collections.IEnumerable metricsEnumerable)
                                                {
                                                    int metricIndex = 0;
                                                    foreach (var metric in metricsEnumerable)
                                                    {
                                                        var nameProperty = metric?.GetType().GetProperty("Name");
                                                        var descriptionProperty = metric?.GetType().GetProperty("Description");
                                                        var unitProperty = metric?.GetType().GetProperty("Unit");
                                                        var name = nameProperty?.GetValue(metric)?.ToString() ?? "Unknown";
                                                        var description = descriptionProperty?.GetValue(metric)?.ToString() ?? "N/A";
                                                        var unit = unitProperty?.GetValue(metric)?.ToString() ?? "N/A";
                                                        
                                                        Console.WriteLine($"\n    Metric {metricIndex + 1}: {name}");
                                                        Console.WriteLine($"    Description: {description}");
                                                        Console.WriteLine($"    Unit: {unit}");

                                                        // Try to get Gauge, Sum, Histogram, or Summary
                                                        ParseMetricDataPoints(metric);
                                                        metricIndex++;
                                                    }
                                                }
                                            }
                                            scopeIndex++;
                                        }
                                    }
                                }
                                index++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing metrics with reflection: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void ParseMetricDataPoints(object? metric)
        {
            if (metric == null) return;

            // Try Gauge
            var gaugeProperty = metric.GetType().GetProperty("Gauge");
            if (gaugeProperty != null)
            {
                var gauge = gaugeProperty.GetValue(metric);
                if (gauge != null)
                {
                    Console.WriteLine($"    Type: Gauge");
                    var dataPointsProperty = gauge.GetType().GetProperty("DataPoints");
                    if (dataPointsProperty != null)
                    {
                        var dataPoints = dataPointsProperty.GetValue(gauge);
                        ParseNumberDataPoints(dataPoints);
                    }
                    return;
                }
            }

            // Try Sum
            var sumProperty = metric.GetType().GetProperty("Sum");
            if (sumProperty != null)
            {
                var sum = sumProperty.GetValue(metric);
                if (sum != null)
                {
                    var isMonotonicProperty = sum.GetType().GetProperty("IsMonotonic");
                    var aggregationTemporalityProperty = sum.GetType().GetProperty("AggregationTemporality");
                    var isMonotonic = isMonotonicProperty?.GetValue(sum)?.ToString() ?? "N/A";
                    var aggregationTemporality = aggregationTemporalityProperty?.GetValue(sum)?.ToString() ?? "N/A";
                    Console.WriteLine($"    Type: Sum (IsMonotonic: {isMonotonic}, AggregationTemporality: {aggregationTemporality})");
                    var dataPointsProperty = sum.GetType().GetProperty("DataPoints");
                    if (dataPointsProperty != null)
                    {
                        var dataPoints = dataPointsProperty.GetValue(sum);
                        ParseNumberDataPoints(dataPoints);
                    }
                    return;
                }
            }

            // Try Histogram
            var histogramProperty = metric.GetType().GetProperty("Histogram");
            if (histogramProperty != null)
            {
                var histogram = histogramProperty.GetValue(metric);
                if (histogram != null)
                {
                    var aggregationTemporalityProperty = histogram.GetType().GetProperty("AggregationTemporality");
                    var aggregationTemporality = aggregationTemporalityProperty?.GetValue(histogram)?.ToString() ?? "N/A";
                    Console.WriteLine($"    Type: Histogram (AggregationTemporality: {aggregationTemporality})");
                    var dataPointsProperty = histogram.GetType().GetProperty("DataPoints");
                    if (dataPointsProperty != null)
                    {
                        var dataPoints = dataPointsProperty.GetValue(histogram);
                        ParseHistogramDataPoints(dataPoints);
                    }
                    return;
                }
            }

            // Try Summary
            var summaryProperty = metric.GetType().GetProperty("Summary");
            if (summaryProperty != null)
            {
                var summary = summaryProperty.GetValue(metric);
                if (summary != null)
                {
                    Console.WriteLine($"    Type: Summary");
                    var dataPointsProperty = summary.GetType().GetProperty("DataPoints");
                    if (dataPointsProperty != null)
                    {
                        var dataPoints = dataPointsProperty.GetValue(summary);
                        ParseSummaryDataPoints(dataPoints);
                    }
                }
            }
        }

        private void ParseNumberDataPoints(object? dataPoints)
        {
            if (dataPoints == null || !(dataPoints is System.Collections.IEnumerable enumerable)) return;

            foreach (var dataPoint in enumerable)
            {
                if (dataPoint == null) continue;

                // Try to get value (AsInt or AsDouble)
                var asIntProperty = dataPoint.GetType().GetProperty("AsInt");
                var asDoubleProperty = dataPoint.GetType().GetProperty("AsDouble");
                var valueCaseProperty = dataPoint.GetType().GetProperty("ValueCase");
                
                string valueStr = "N/A";
                if (valueCaseProperty != null)
                {
                    var valueCase = valueCaseProperty.GetValue(dataPoint);
                    if (valueCase?.ToString()?.Contains("AsInt") == true && asIntProperty != null)
                    {
                        valueStr = asIntProperty.GetValue(dataPoint)?.ToString() ?? "N/A";
                    }
                    else if (valueCase?.ToString()?.Contains("AsDouble") == true && asDoubleProperty != null)
                    {
                        valueStr = asDoubleProperty.GetValue(dataPoint)?.ToString() ?? "N/A";
                    }
                }

                Console.WriteLine($"      Value: {valueStr}");
                ParseDataPointAttributes(dataPoint);
                ParseDataPointTimestamp(dataPoint);
            }
        }

        private void ParseHistogramDataPoints(object? dataPoints)
        {
            if (dataPoints == null || !(dataPoints is System.Collections.IEnumerable enumerable)) return;

            foreach (var dataPoint in enumerable)
            {
                if (dataPoint == null) continue;

                var countProperty = dataPoint.GetType().GetProperty("Count");
                var sumProperty = dataPoint.GetType().GetProperty("Sum");
                var count = countProperty?.GetValue(dataPoint)?.ToString() ?? "N/A";
                var sum = sumProperty?.GetValue(dataPoint)?.ToString() ?? "N/A";
                
                Console.WriteLine($"      Count: {count}");
                Console.WriteLine($"      Sum: {sum}");

                var bucketCountsProperty = dataPoint.GetType().GetProperty("BucketCounts");
                var explicitBoundsProperty = dataPoint.GetType().GetProperty("ExplicitBounds");
                
                if (bucketCountsProperty != null)
                {
                    var bucketCounts = bucketCountsProperty.GetValue(dataPoint);
                    if (bucketCounts is System.Collections.IEnumerable bucketEnumerable)
                    {
                        var buckets = bucketEnumerable.Cast<object>().Select(b => b?.ToString() ?? "").ToList();
                        Console.WriteLine($"      Bucket Counts: {string.Join(", ", buckets)}");
                    }
                }
                
                if (explicitBoundsProperty != null)
                {
                    var explicitBounds = explicitBoundsProperty.GetValue(dataPoint);
                    if (explicitBounds is System.Collections.IEnumerable boundsEnumerable)
                    {
                        var bounds = boundsEnumerable.Cast<object>().Select(b => b?.ToString() ?? "").ToList();
                        Console.WriteLine($"      Explicit Bounds: {string.Join(", ", bounds)}");
                    }
                }

                ParseDataPointAttributes(dataPoint);
                ParseDataPointTimestamp(dataPoint);
            }
        }

        private void ParseSummaryDataPoints(object? dataPoints)
        {
            if (dataPoints == null || !(dataPoints is System.Collections.IEnumerable enumerable)) return;

            foreach (var dataPoint in enumerable)
            {
                if (dataPoint == null) continue;

                var countProperty = dataPoint.GetType().GetProperty("Count");
                var sumProperty = dataPoint.GetType().GetProperty("Sum");
                var count = countProperty?.GetValue(dataPoint)?.ToString() ?? "N/A";
                var sum = sumProperty?.GetValue(dataPoint)?.ToString() ?? "N/A";
                
                Console.WriteLine($"      Count: {count}");
                Console.WriteLine($"      Sum: {sum}");

                var quantileValuesProperty = dataPoint.GetType().GetProperty("QuantileValues");
                if (quantileValuesProperty != null)
                {
                    var quantileValues = quantileValuesProperty.GetValue(dataPoint);
                    if (quantileValues is System.Collections.IEnumerable quantileEnumerable)
                    {
                        Console.WriteLine("      Quantiles:");
                        foreach (var quantile in quantileEnumerable)
                        {
                            var quantileProperty = quantile?.GetType().GetProperty("Quantile");
                            var valueProperty = quantile?.GetType().GetProperty("Value");
                            var quantileVal = quantileProperty?.GetValue(quantile)?.ToString() ?? "N/A";
                            var value = valueProperty?.GetValue(quantile)?.ToString() ?? "N/A";
                            Console.WriteLine($"        {quantileVal} = {value}");
                        }
                    }
                }

                ParseDataPointAttributes(dataPoint);
                ParseDataPointTimestamp(dataPoint);
            }
        }

        private void ParseDataPointAttributes(object? dataPoint)
        {
            if (dataPoint == null) return;

            var attributesProperty = dataPoint.GetType().GetProperty("Attributes");
            if (attributesProperty != null)
            {
                var attributes = attributesProperty.GetValue(dataPoint);
                if (attributes is System.Collections.IEnumerable attrEnumerable && attributes != null)
                {
                    var hasAttributes = false;
                    foreach (var attr in attrEnumerable)
                    {
                        if (!hasAttributes)
                        {
                            Console.WriteLine("      Attributes:");
                            hasAttributes = true;
                        }
                        var keyProperty = attr?.GetType().GetProperty("Key");
                        var valueProperty = attr?.GetType().GetProperty("Value");
                        if (keyProperty != null && valueProperty != null)
                        {
                            var key = keyProperty.GetValue(attr)?.ToString() ?? "N/A";
                            var valueObj = valueProperty.GetValue(attr);
                            var value = GetAttributeValue(valueObj);
                            Console.WriteLine($"        {key} = {value}");
                        }
                    }
                }
            }
        }

        private void ParseDataPointTimestamp(object? dataPoint)
        {
            if (dataPoint == null) return;

            var timeUnixNanoProperty = dataPoint.GetType().GetProperty("TimeUnixNano");
            if (timeUnixNanoProperty != null)
            {
                var timeUnixNano = timeUnixNanoProperty.GetValue(dataPoint);
                if (timeUnixNano != null)
                {
                    if (timeUnixNano is ulong nanoTime)
                    {
                        if (nanoTime > 0)
                        {
                            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)(nanoTime / 1_000_000));
                            Console.WriteLine($"      Timestamp: {timestamp:yyyy-MM-dd HH:mm:ss.fff}");
                        }
                    }
                    else if (ulong.TryParse(timeUnixNano.ToString(), out var parsedNano))
                    {
                        if (parsedNano > 0)
                        {
                            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)(parsedNano / 1_000_000));
                            Console.WriteLine($"      Timestamp: {timestamp:yyyy-MM-dd HH:mm:ss.fff}");
                        }
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
}
