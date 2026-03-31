using System.Collections.Generic;

namespace FinOpsToolSample.Models
{
    public class AIApiResponse
    {
        public string message_id { get; set; } = string.Empty;
        public string agent_id { get; set; } = string.Empty;
        public string model { get; set; } = string.Empty;
        public double timestamp { get; set; }
        public string conversation_id { get; set; } = string.Empty;
        public string application_id { get; set; } = string.Empty;
        public AIOutputContent output { get; set; } = new();
    }

    public class AIOutputContent
    {
        public string content { get; set; } = string.Empty;
    }
}
