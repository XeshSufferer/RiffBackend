namespace RiffCore.Models;

public class Chat
{
    public MongoDB.Bson.ObjectId Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<MongoDB.Bson.ObjectId> MembersId { get; set; } = new List<MongoDB.Bson.ObjectId>();
    public MongoDB.Bson.BsonDateTime Created { get; set; }
}