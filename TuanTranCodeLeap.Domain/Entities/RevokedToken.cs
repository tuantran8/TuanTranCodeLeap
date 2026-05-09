namespace TuanTranCodeLeap.Domain.Entities;

public class RevokedToken
{
    public int Id { get; set; }
    public string TokenJti { get; set; } = string.Empty; // JWT unique identifier
    public string Token { get; set; } = string.Empty; // Full token hash for extra security
    public int UserId { get; set; }
    public DateTime RevokedAt { get; set; }
    public DateTime ExpiresAt { get; set; } // When the token naturally expires
    public string? Reason { get; set; }
}
