namespace HackService.Models;

public record UserInfo
{
    public long id { get; set; }
    public long AccessHash { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Username { get; set; }
    public string Phone { get; set; }
    public bool IsActive { get; set; }
    public bool IsBot { get; set; }
    public string? Bio { get; set; }
    
    //Photo 
    public long? PhotoId { get; set; }
    public string? PhotoBase64 { get; set; }
}