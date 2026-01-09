namespace InventorySystem2.Auth;

public record UserSession(string Username, bool IsAdmin);

public static class Session
{
    public static UserSession? CurrentUser { get; set; }
}