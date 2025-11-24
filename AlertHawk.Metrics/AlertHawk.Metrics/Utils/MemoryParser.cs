namespace AlertHawk.Metrics.Utils;

public static class MemoryParser
{
    public static double ParseToBytes(string? memoryValue)
    {
        if (string.IsNullOrWhiteSpace(memoryValue))
            return 0;

        memoryValue = memoryValue.Trim();
        double value = 0;

        if (memoryValue.EndsWith("Ki", StringComparison.OrdinalIgnoreCase))
        {
            var numericPart = memoryValue.Substring(0, memoryValue.Length - 2);
            if (double.TryParse(numericPart, out value))
                return value * 1024;
        }
        else if (memoryValue.EndsWith("Mi", StringComparison.OrdinalIgnoreCase))
        {
            var numericPart = memoryValue.Substring(0, memoryValue.Length - 2);
            if (double.TryParse(numericPart, out value))
                return value * 1024 * 1024;
        }
        else if (memoryValue.EndsWith("Gi", StringComparison.OrdinalIgnoreCase))
        {
            var numericPart = memoryValue.Substring(0, memoryValue.Length - 2);
            if (double.TryParse(numericPart, out value))
                return value * 1024 * 1024 * 1024;
        }
        else if (memoryValue.EndsWith("Ti", StringComparison.OrdinalIgnoreCase))
        {
            var numericPart = memoryValue.Substring(0, memoryValue.Length - 2);
            if (double.TryParse(numericPart, out value))
                return value * 1024L * 1024 * 1024 * 1024;
        }
        else if (double.TryParse(memoryValue, out value))
        {
            return value; // Assume bytes if no unit
        }

        return 0;
    }
}

