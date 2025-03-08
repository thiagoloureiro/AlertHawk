using Microsoft.AspNetCore.Http;

namespace AlertHawk.Monitoring.Domain.Entities;

public class MonitorK8sDto
{
    public string MonitorK8s { get; set; }  // This will contain the JSON as a string

    public IFormFile? File { get; set; }  // This is the uploaded file
}
