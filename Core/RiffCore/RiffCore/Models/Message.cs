namespace RiffCore.Models;

public class Message
{
    public MongoDB.Bson.ObjectId Id { get; set; }
    public MongoDB.Bson.ObjectId ChatId { get; set; }
    public MongoDB.Bson.ObjectId SenderId { get; set; }
    public string Text { get; set; }
    public MongoDB.Bson.BsonDateTime Created { get; set; }
    public bool IsModified { get; set; }
    
    
}