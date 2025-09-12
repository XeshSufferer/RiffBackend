using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RiffCore.Models;
using RiffCore.Services;
using RiffCore.Tracker;

namespace RiffCore.Hubs;

public class GatewayHub : Hub
{
    
    private readonly IJWTService _jwt;
    private readonly ILogger<GatewayHub> _logger;
    private readonly IUniversalRequestTracker  _tracker;
    private readonly IRabbitMQService _rabbit;
    
    private static readonly ConcurrentDictionary<string, List<string>> UserConnections = new();
    
    public GatewayHub(IJWTService jwt,  ILogger<GatewayHub> logger, IUniversalRequestTracker tracker,  IRabbitMQService rabbit)
    {
        _jwt = jwt;
        _logger = logger;
        _tracker = tracker;
        _rabbit = rabbit;
    }
    
    
    
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
        {
            UserConnections.AddOrUpdate(userId,
                new List<string> { Context.ConnectionId },
                (key, existingList) =>
                {
                    lock (existingList)
                    {
                        if (!existingList.Contains(Context.ConnectionId))
                            existingList.Add(Context.ConnectionId);
                        return existingList;
                    }
                });
        
            _logger.LogInformation("User {UserId} connected. ConnectionId: {ConnectionId}", 
                userId, Context.ConnectionId);
        }
    
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userId = Context.User.Identity?.Name;
        if (!string.IsNullOrEmpty(userId) && UserConnections.TryGetValue(userId, out var connections))
        {
            lock (connections)
            {
                connections.Remove(Context.ConnectionId);
                if (connections.Count == 0)
                    UserConnections.TryRemove(userId, out _);
            }
        
            _logger.LogInformation("User {UserId} disconnected. ConnectionId: {ConnectionId}", 
                userId, Context.ConnectionId);
        }
    
        await base.OnDisconnectedAsync(exception);
    }
    
    
    public async Task Login(UserLoginData data)
    {
        _logger.LogInformation($"Login request received {data.Login} {data.Password}");
        var correlationId = _tracker.CreatePendingRequest();
        data.CorrelationId = correlationId;
        await _rabbit.SendMessageAsync<UserLoginData>(data, "Riff.Core.Accounts.Login.Input");
        var userdata = await _tracker.WaitForResponseAsync<User>(correlationId);

        if (userdata.PasswordHash == "NULL")
        {
            await Clients.Caller.SendAsync("LoginFailed");
            return;
        }
        await AddUserToGroups(userdata.Id, userdata.ChatsIds);
        var token = _jwt.GenerateToken(userdata.Id);
        
        await Clients.Caller.SendAsync("LoginSuccess", token, userdata.Id);
    }

    public async Task Register(UserRegisterData data)
    {
        var correlationId = _tracker.CreatePendingRequest();
        data.CorrelationID = correlationId;
        _logger.LogInformation("Register request received from {nickname}", data.Nickname);
        await _rabbit.SendMessageAsync<UserRegisterData>(data, "Riff.Core.Accounts.Register.Input");
        var userdata = await _tracker.WaitForResponseAsync<User>(correlationId);

        if (userdata.PasswordHash == "NULL")
        {
            await Clients.Caller.SendAsync("RegistrationFailed");
            return;
        }
        await AddUserToGroups(userdata.Id, userdata.ChatsIds);
        var token = _jwt.GenerateToken(userdata.Id);
        await Clients.Caller.SendAsync("RegisterSuccess", token, userdata.Id);
    }
    
    
    [Authorize]
    public async Task Autologin(string token)
    {
        var correlationId = _tracker.CreatePendingRequest();
        UserIdDTO data = new UserIdDTO
        {
            CorrelationId = correlationId,
            Id = Context.User.Identity.Name,
        };
        await _rabbit.SendMessageAsync(data, "Riff.Core.Accounts.GetByID.Input");
        var userdata = await _tracker.WaitForResponseAsync<User>(correlationId);
        
        
        if (userdata.Id == "000000000000000000000000")
        {
            await Clients.Caller.SendAsync("LoginFailed");
            return;
        }
        
        await AddUserToGroups(userdata.Id, userdata.ChatsIds);
        await Clients.Caller.SendAsync("LoginSuccess", token, userdata.Id);
    }

    [Authorize]
    public async Task SendMessage(MessageSendingDTO message)
    {
        string correlationid = _tracker.CreatePendingRequest();

        var buildedMessage = new Message{
            Text = message.Message,
            SenderId = Context.User.Identity.Name,
            ChatId = message.ChatId,
            Created = DateTime.Now,
            IsModified = false,
            CorrelationId = correlationid
        };

        await _rabbit.SendMessageAsync<Message>(buildedMessage, "Riff.Core.Messages.SendMessage.Input");
        Message data = await _tracker.WaitForResponseAsync<Message>(correlationid);

        if(data.SenderId == "000000000000000000000000")
        {
            return;
        }

        await Clients.Group(message.ChatId + "_chat").SendAsync("OnMessageReceived", data);
    }

    [Authorize]
    public async Task CreateChatWith(string username)
    {
        var correlationId = _tracker.CreatePendingRequest();
        ChatCreatingRequestDTO data = new ChatCreatingRequestDTO()
        {
            CorrelationId = correlationId,
            RequestedUsername = username,
            RequesterId = Context.User.Identity.Name,
        };
        
        _logger.LogInformation("Builded chat creating request correlation: {cor_id} | requested username: {username} | requesterId : {id}", data.CorrelationId, data.RequestedUsername, data.RequesterId);
        
        await _rabbit.SendMessageAsync(data, "Riff.Core.Chats.Creating.Input");
        ChatCreatingAcceptDTO responseData = await _tracker.WaitForResponseAsync<ChatCreatingAcceptDTO>(correlationId);
        
        _logger.LogInformation("Response data: requester: {requesterID} requested: {requestedID}", responseData.Requester, responseData.Requested);
        
        await AddToNewChatGroup(responseData.Requested, responseData.ChatId);
        await AddToNewChatGroup(responseData.Requester, responseData.ChatId);

        Chat chat = new Chat{
            Id = responseData.ChatId,
            Name = responseData.Requester + " " + responseData.Requested,
            
        };

        await Clients.Group(responseData.ChatId + "_chat").SendAsync("OnChatCreated", chat);
    }

    private async Task AddToNewChatGroup(string userId, string chatId)
    {
        if (UserConnections.TryGetValue(userId, out var connections))
        {
            foreach (var connectionId in connections.ToList())
            {
                await Groups.AddToGroupAsync(connectionId, chatId + "_chat");
                _logger.LogInformation("User {UserId} added to chat {ChatId} via connection {ConnectionId}",
                    userId, chatId, connectionId);
            }
        }
        else
        {
            _logger.LogWarning("User {UserId} has no active connections when trying to add to chat {ChatId}", 
                userId, chatId);
        }
    }

    private async Task AddUserToGroups(string userId, List<string> chatIds)
    {
        if (UserConnections.TryGetValue(userId, out var connections))
        {
            _logger.LogInformation("Adding user {UserId} to groups. Connections: {Count}, Chats: {ChatCount}",
                userId, connections.Count, chatIds?.Count ?? 0);
        
            foreach (var connectionId in connections.ToList())
            {
                await Groups.AddToGroupAsync(connectionId, userId + "_user");
            
                
                if (chatIds != null)
                {
                    foreach (var chatId in chatIds)
                    {
                        await Groups.AddToGroupAsync(connectionId, chatId + "_chat");
                        _logger.LogInformation("User {UserId} added to chat {ChatId} via connection {ConnectionId}",
                            userId, chatId, connectionId);
                    }
                }
            }
        }
        else
        {
            _logger.LogWarning("User {UserId} has no active connections", userId);
        }
    }
    
}