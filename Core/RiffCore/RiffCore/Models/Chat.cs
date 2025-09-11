namespace RiffCore.Models;

public class Chat
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> MembersId { get; set; } = new List<string>();
    public DateTime Created { get; set; }
}