using HackService.Models;
using TL;
using WTelegram;

namespace HackService.Services;

public class UserInfoService : IDisposable, IAsyncDisposable
{
    public Client Client { get; set; }

    public UserInfoService(TgConnector connector)
    {
        Client = connector.Client;
    }
    
    public async Task<bool> LoginToChat(string? chatId)
    {
        await Client.LoginUserIfNeeded();

        string hash;
        if (chatId.Contains("t.me"))
        {
            hash = chatId.Split('+').Last();
        }
        else
        {
            hash = chatId;
        }
        
        // var chatInvite = await Client.Messages_CheckChatInvite(hash); // optional: get information before joining  

        try
        {
            await Client.Messages_ImportChatInvite(hash); // join the channel/group  
        }
        catch (RpcException e)
        {
            if (e.Message != "USER_ALREADY_PARTICIPANT")
                throw;
        }
        

        Console.WriteLine($"joined chat id: {chatId} hash: {hash}");

        return true;
    }
    
    public async Task<UserInfo[]> GetUserInfo(long chatId, long? userId) 
    {
        chatId = ModuleBio(chatId)!.Value;
        
        Console.WriteLine($"getuserinfo chatid: {chatId} userid: {userId}");
        
        await Client.LoginUserIfNeeded();
        
        var chats = await Client.Messages_GetAllChats();
        
        var channelFull = chats.chats[chatId];

        if (channelFull is Channel && channelFull.IsChannel)
        {
            var participants = await Client.Channels_GetAllParticipants((Channel)chats.chats[chatId]);

            return await UserInfosFromParticipants(userId, participants);
        }
        
        if (channelFull is Channel && !channelFull.IsChannel)
        {
            var participants = await Client.Channels_GetAllParticipants((Channel)chats.chats[chatId]);

            return await UserInfosFromParticipants(userId, participants);
        }
        
        var chat = await Client.Messages_GetFullChat(chatId);

        return await UserInfosFromChatUsers(userId, chat.users);
    }

    private async Task<UserInfo[]> UserInfosFromParticipants(long? userId, Channels_ChannelParticipants participants)
    {
        UserInfo[] result = participants.users
            .Where((kv, _) =>
            {
                return !kv.Value.IsBot;
            })
            .Select((kv, thisId) =>
            {
                (long id, User? user) = kv;

                var thumbBytes = user.photo?.stripped_thumb;
            
                return new UserInfo
                {
                    id = user.id,
                    AccessHash = user.access_hash,
                    FirstName = user.first_name,
                    LastName = user.last_name,
                    Username = user.username,
                    Phone = user.phone,
                    IsActive = user.IsActive,
                    IsBot = user.IsBot,
                    PhotoBase64 = thumbBytes != null ? Convert.ToBase64String(thumbBytes) : null,
                    PhotoId = user.photo?.photo_id, 
                };
            }).ToArray();
        
        if (userId != null)
        {
            result = result.Where(x => x.id == userId).ToArray();
        }

        foreach (var userInfo in result)
        {
            var user = participants.users[userInfo.id];

            var inputUser = user;
            // var inputUser = new InputUser(userInfo.id, userInfo.AccessHash);
            
            var fullInfo = await Client.Users_GetFullUser(inputUser);

            userInfo.Bio = fullInfo.full_user.about;
        }
        

        return result;
    }
    
    private async Task<UserInfo[]> UserInfosFromChatUsers(long? userId, Dictionary<long, User> participants)
    {
        UserInfo[] result = participants
            .Where((kv, _) =>
            {
                return !kv.Value.IsBot;
            })
            .Select((kv, thisId) =>
            {
                (long id, User? user) = kv;

                var thumbBytes = user.photo?.stripped_thumb;
            
                return new UserInfo
                {
                    id = user.id,
                    AccessHash = user.access_hash,
                    FirstName = user.first_name,
                    LastName = user.last_name,
                    Username = user.username,
                    Phone = user.phone,
                    IsActive = user.IsActive,
                    IsBot = user.IsBot,
                    PhotoBase64 = thumbBytes != null ? Convert.ToBase64String(thumbBytes) : null,
                    PhotoId = user.photo?.photo_id, 
                };
            }).ToArray();
        
        if (userId != null)
        {
            result = result.Where(x => x.id == userId).ToArray();
        }

        foreach (var userInfo in result)
        {
            var user = participants[userInfo.id];

            var inputUser = user;
            // var inputUser = new InputUser(userInfo.id, userInfo.AccessHash);
            
            var fullInfo = await Client.Users_GetFullUser(inputUser);

            userInfo.Bio = fullInfo.full_user.about;
        }
        

        return result;
    }

    public async Task<string> GetUserBio(long userId, long? chatId = null)
    {
        chatId = ModuleBio(chatId);
        
        await Client.LoginUserIfNeeded();
        
        Console.WriteLine($"getting user bio useid {userId} chatid {chatId}");
        
        var chats = await Client.Messages_GetAllChats();

        var chat = chats.chats[chatId!.Value];
        
        if (chat is Channel && chat.IsChannel)
        {
            Console.WriteLine($"chatid {chatId} is a channel");
            
            var participants = await Client.Channels_GetAllParticipants((Channel)chats.chats[chatId!.Value]);

            var participant = participants.users[userId];
            
            var participantFullInfo = await Client.Users_GetFullUser(participant);

            return participantFullInfo.full_user.about;
        }
        
        if (chat is Channel && !chat.IsChannel)
        {
            Console.WriteLine($"chatid {chatId} is a megagroup");
            
            var participants = await Client.Channels_GetAllParticipants((Channel)chats.chats[chatId!.Value]);

            var participant = participants.users[userId];
            
            var participantFullInfo = await Client.Users_GetFullUser(participant);

            return participantFullInfo.full_user.about;
        }
        
        Console.WriteLine($"chatid {chatId} is a chat");
        
        var chatFull = await Client.Messages_GetFullChat(chatId.Value);
        
        var user = chatFull.users[userId];

        var inputUser = user;
            
        var fullInfo = await Client.Users_GetFullUser(inputUser);

        return fullInfo.full_user.about;
    }

    public long? ModuleBio(long? chatId)
    {
        if (chatId == null)
            return null;
        
        chatId = Math.Abs(chatId.Value);

        var str = chatId.ToString();

        if (str.StartsWith("100"))
        {
            str = str.Remove(0, 3);
        }

        chatId = Convert.ToInt64(str);
        
        return chatId;
    }

    public void Dispose()
    {
        Client.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
    }
}