namespace AlertHawk.Authentication.Domain.Entities;

public class UserDeviceToken
{
    public Guid Id { get; set; }
    public string DeviceToken { get; set; }
}