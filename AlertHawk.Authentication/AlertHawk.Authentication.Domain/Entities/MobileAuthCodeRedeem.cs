namespace AlertHawk.Authentication.Domain.Entities;

public class MobileAuthCodeRedeem
{
    public string Code { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
