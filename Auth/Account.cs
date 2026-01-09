using System.ComponentModel.DataAnnotations;

namespace InventorySystem2.Auth;

public class Account
{
    [Key] public string Username { get; set; } = "";

    public byte[] Salt { get; set; } = [];
    public byte[] SaltedPasswordHash { get; set; } = [];
    public bool isAdmin { get; set; }
}