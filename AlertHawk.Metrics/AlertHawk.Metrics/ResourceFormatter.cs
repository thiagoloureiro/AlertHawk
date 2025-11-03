namespace AlertHawk.Metrics;

public static class ResourceFormatter
{
    public static string FormatCpu(string? cpuValue)
    {
        if (string.IsNullOrWhiteSpace(cpuValue))
            return "N/A";

        // Remove any whitespace
        cpuValue = cpuValue.Trim();
        
        // Parse the value and unit
        double value = 0;
        string unit = "";
        
        // Check for nanocores (n)
        if (cpuValue.EndsWith("n", StringComparison.OrdinalIgnoreCase))
        {
            var numericPart = cpuValue.Substring(0, cpuValue.Length - 1);
            if (double.TryParse(numericPart, out value))
            {
                // Convert nanocores to millicores (divide by 1,000,000)
                value = value / 1_000_000;
                return $"{value:F3} m";
            }
        }
        // Check for millicores (m)
        else if (cpuValue.EndsWith("m", StringComparison.OrdinalIgnoreCase))
        {
            var numericPart = cpuValue.Substring(0, cpuValue.Length - 1);
            if (double.TryParse(numericPart, out value))
            {
                if (value >= 1000)
                {
                    // Convert to cores
                    return $"{value / 1000:F2} cores";
                }
                return $"{value:F2} m";
            }
        }
        // Try parsing as plain number (cores)
        else if (double.TryParse(cpuValue, out value))
        {
            if (value >= 1)
                return $"{value:F2} cores";
            else if (value >= 0.001)
                return $"{(value * 1000):F2} m";
            else
                return $"{(value * 1_000_000):F0} n";
        }

        return cpuValue; // Return original if we can't parse
    }

    public static string FormatMemory(string? memoryValue)
    {
        if (string.IsNullOrWhiteSpace(memoryValue))
            return "N/A";

        // Remove any whitespace
        memoryValue = memoryValue.Trim();
        
        double value = 0;
        string unit = "";
        
        // Check for different memory units
        if (memoryValue.EndsWith("Ki", StringComparison.OrdinalIgnoreCase))
        {
            var numericPart = memoryValue.Substring(0, memoryValue.Length - 2);
            if (double.TryParse(numericPart, out value))
            {
                return FormatBinaryBytes(value * 1024, "Ki");
            }
        }
        else if (memoryValue.EndsWith("Mi", StringComparison.OrdinalIgnoreCase))
        {
            var numericPart = memoryValue.Substring(0, memoryValue.Length - 2);
            if (double.TryParse(numericPart, out value))
            {
                return FormatBinaryBytes(value * 1024 * 1024, "Mi");
            }
        }
        else if (memoryValue.EndsWith("Gi", StringComparison.OrdinalIgnoreCase))
        {
            var numericPart = memoryValue.Substring(0, memoryValue.Length - 2);
            if (double.TryParse(numericPart, out value))
            {
                return FormatBinaryBytes(value * 1024 * 1024 * 1024, "Gi");
            }
        }
        else if (memoryValue.EndsWith("Ti", StringComparison.OrdinalIgnoreCase))
        {
            var numericPart = memoryValue.Substring(0, memoryValue.Length - 2);
            if (double.TryParse(numericPart, out value))
            {
                return FormatBinaryBytes(value * 1024L * 1024 * 1024 * 1024, "Ti");
            }
        }
        else if (memoryValue.EndsWith("K", StringComparison.OrdinalIgnoreCase))
        {
            var numericPart = memoryValue.Substring(0, memoryValue.Length - 1);
            if (double.TryParse(numericPart, out value))
            {
                return FormatBinaryBytes(value * 1000, "K");
            }
        }
        else if (memoryValue.EndsWith("M", StringComparison.OrdinalIgnoreCase))
        {
            var numericPart = memoryValue.Substring(0, memoryValue.Length - 1);
            if (double.TryParse(numericPart, out value))
            {
                return FormatBinaryBytes(value * 1000 * 1000, "M");
            }
        }
        else if (memoryValue.EndsWith("G", StringComparison.OrdinalIgnoreCase))
        {
            var numericPart = memoryValue.Substring(0, memoryValue.Length - 1);
            if (double.TryParse(numericPart, out value))
            {
                return FormatBinaryBytes(value * 1000L * 1000 * 1000, "G");
            }
        }
        // Try parsing as plain bytes
        else if (double.TryParse(memoryValue, out value))
        {
            return FormatBinaryBytes(value, "B");
        }

        return memoryValue; // Return original if we can't parse
    }

    private static string FormatBinaryBytes(double bytes, string originalUnit)
    {
        const double kibibyte = 1024;
        const double mebibyte = kibibyte * 1024;
        const double gibibyte = mebibyte * 1024;
        const double tebibyte = gibibyte * 1024;

        if (bytes >= tebibyte)
            return $"{bytes / tebibyte:F2} TiB";
        else if (bytes >= gibibyte)
            return $"{bytes / gibibyte:F2} GiB";
        else if (bytes >= mebibyte)
            return $"{bytes / mebibyte:F2} MiB";
        else if (bytes >= kibibyte)
            return $"{bytes / kibibyte:F2} KiB";
        else
            return $"{bytes:F0} B";
    }
}

