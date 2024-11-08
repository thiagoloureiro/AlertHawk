namespace AlertHawk.Monitoring.Domain
{
    public class DownSamplingSettings
    {
        public static readonly string DownSampling = "Downsampling";

        public bool Active { get; set; }
        public int IntervalInSeconds { get; set; } = 60;
    }
}
