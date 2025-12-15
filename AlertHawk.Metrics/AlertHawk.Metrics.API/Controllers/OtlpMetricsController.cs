using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Text;
using System.Linq;

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
                Console.WriteLine($"Query String: {Request.QueryString}");

                // Log all headers
                Console.WriteLine("\n--- Headers ---");
                foreach (var header in Request.Headers)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }

                // Read the raw request body
                Console.WriteLine("\n--- Request Body ---");
                var isProtobuf = Request.ContentType?.Contains("application/x-protobuf") == true ||
                                Request.ContentType?.Contains("application/protobuf") == true ||
                                Request.ContentType?.Contains("application/x-otlp") == true;

                // Enable buffering to allow reading the body multiple times if needed
                Request.EnableBuffering();

                if (isProtobuf)
                {
                    // For protobuf, read as bytes
                    Request.Body.Position = 0;
                    using var memoryStream = new MemoryStream();
                    await Request.Body.CopyToAsync(memoryStream);
                    var bodyBytes = memoryStream.ToArray();
                    Request.Body.Position = 0; // Reset for potential future reads

                    Console.WriteLine($"Body (Protobuf - Binary, {bodyBytes.Length} bytes)");
                    Console.WriteLine($"Body (Base64): {Convert.ToBase64String(bodyBytes)}");
                    Console.WriteLine($"Body (Hex - first 100 bytes): {BitConverter.ToString(bodyBytes.Take(100).ToArray()).Replace("-", " ")}");
                    
                    _logger.LogInformation("OTLP metrics received (Protobuf). Content-Type: {ContentType}, Body Length: {BodyLength}",
                        Request.ContentType, bodyBytes.Length);
                }
                else
                {
                    // For JSON or other text formats, read as string
                    Request.Body.Position = 0;
                    using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    Request.Body.Position = 0; // Reset for potential future reads

                    if (!string.IsNullOrEmpty(body))
                    {
                        Console.WriteLine($"Body (Text, {body.Length} characters):");
                        Console.WriteLine(body);
                    }
                    else
                    {
                        Console.WriteLine("(Empty body)");
                    }

                    _logger.LogInformation("OTLP metrics received. Content-Type: {ContentType}, Body Length: {BodyLength}",
                        Request.ContentType, body?.Length ?? 0);
                }

                Console.WriteLine("=== End OTLP Metrics ===\n");

                return Ok(new { success = true, message = "Metrics received" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving OTLP metrics: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                _logger.LogError(ex, "Error receiving OTLP metrics");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
