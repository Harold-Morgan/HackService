using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using HackService.Models;
using TL;
using WTelegram;

namespace HackService.Services;

public class ChatInfoService : IDisposable, IAsyncDisposable
{
    public Client Client { get; set; }
    
    public static ConcurrentDictionary<long, string> ChatPortraitsCache = new ();
    public static ConcurrentDictionary<(long, long), string> UserPortraitsCache = new ();
    
    private object PortraitsLock = new();
    private object UsersLock = new();
    
    public ChatInfoService(TgConnector connector)
    {
        Client = connector.Client;
    }

    public async Task<string> GetUserPortraitCached(long chatId, long userId)
    {
        // Silly hackaton concurrency, don't use in prod

        if (UserPortraitsCache.TryGetValue((chatId, userId), out var cached))
        {
            Task.Run(async () =>
            {
                var res = await GetUserPortrait(chatId, userId);
                
                if (!string.IsNullOrEmpty(res)) 
                    UserPortraitsCache.TryAdd((chatId, userId), res);
            });
            return cached;
        }
        
        var res = await GetUserPortrait(chatId, userId);
                
        UserPortraitsCache.TryAdd((chatId, userId), res);

        return res;
    }
    
    public async Task<string> GetUserPortrait(long chatId, long userId)
    {
        await Client.LoginUserIfNeeded();
        
        var chats = await Client.Messages_GetAllChats();
        InputPeer peer = chats.chats[chatId]; // the chat (or User) we want

        var messagesDict = new Dictionary<int, MessageModel>();
        
        for (int offset_id = 0; ;)
        {
            var messages = await Client.Messages_GetHistory(peer, offset_id);
            if (messages.Messages.Length == 0) 
                break;
            
            foreach (var msgBase in messages.Messages)
            {
                if (msgBase is TL.Message msg)
                {
                    if (msgBase.From.ID != userId)
                        continue;
                    
                    var weight = 0;

                    if (msg.replies?.replies is not null && msg.replies?.replies > 0)
                        weight += msg.replies.replies;

                    var reaction = msg.Reactions?.results.ToArray();
                    if (reaction is not null && reaction.Length > 0)
                        weight += reaction.Length;
                    
                    if (weight > 0)
                        messagesDict.Add(msg.id, new MessageModel
                        {
                            Message = msg,
                            Weight = weight
                        });
                }
            }
            offset_id = messages.Messages[^1].ID;
            
            if (offset_id > 1000)
                break;
        }

        var topPerformingMessages = messagesDict
            .OrderByDescending(x => x.Value.Weight)
            .Take(10)
            .ToArray();


        var sb = new StringBuilder();

        sb.AppendLine(UserPortraitPrompt.PropmtHeader);
        
        foreach (var (_, message) in topPerformingMessages)
        {
            sb.AppendLine();
            sb.Append("\"");
            sb.Append(message.Message?.message);
            sb.Append("\"");
        }

        sb.AppendLine(UserPortraitPrompt.PromptEnd);
        
        var request = sb.ToString();
        
        Console.WriteLine(request);

        // using var client = 
        using var client = new DeepSeekClient();

        var res = await client.ChatAsync(request);
        
        return res;
    }

    public async Task<string> GetChatPortraitCached(long chatId)
    {
        // Silly hackaton concurrency, don't use in prod
        if (ChatPortraitsCache.TryGetValue(chatId, out var cached))
        {
            Task.Run(async () =>
            {
                var res = await GetChatPortrait(chatId);
                
                if (!string.IsNullOrEmpty(res)) 
                    ChatPortraitsCache.TryAdd(chatId, res);
            });
            return cached;
        }
        
        var res = await GetChatPortrait(chatId);
                
        ChatPortraitsCache.TryAdd(chatId, res);

        return res;
    }
    
    public async Task<string> GetChatPortrait(long chatId)
    {
        await Client.LoginUserIfNeeded();
        
        var chats = await Client.Messages_GetAllChats();
        InputPeer peer = chats.chats[chatId]; // the chat (or User) we want

        var messagesDict = new Dictionary<int, MessageModel>();
        
        for (int offset_id = 0; ;)
        {
            var messages = await Client.Messages_GetHistory(peer, offset_id);
            if (messages.Messages.Length == 0) 
                break;
            
            foreach (var msgBase in messages.Messages)
            {
                // var from = messages.UserOrChat(msgBase.From ?? msgBase.Peer); // from can be User/Chat/Channel

                if (msgBase is TL.Message msg)
                {
                    var weight = 0;

                    if (msg.replies?.replies is not null && msg.replies?.replies > 0)
                        weight += msg.replies.replies;

                    var reaction = msg.Reactions?.results.ToArray();
                    if (reaction is not null && reaction.Length > 0)
                        weight += reaction.Length;
                    
                    if (weight > 0)
                        messagesDict.Add(msg.id, new MessageModel
                        {
                            Message = msg,
                            Weight = weight
                        });
                }
            }
            offset_id = messages.Messages[^1].ID;
            
            if (offset_id > 1000)
                break;
        }

        var topPerformingMessages = messagesDict
            .OrderByDescending(x => x.Value.Weight)
            .Take(10)
            .ToArray();
        
        var sb = new StringBuilder();

        sb.AppendLine(ChatPortraitPrompt.PropmtHeader);
        
        foreach (var (_, message) in topPerformingMessages)
        {
            sb.AppendLine();
            sb.Append("\"");
            sb.Append(message.Message?.message);
            sb.Append("\"");
        }

        sb.AppendLine(ChatPortraitPrompt.PromptEnd);
        
        var request = sb.ToString();
        
        Console.WriteLine(request);

        // using var client = 
        using var client = new DeepSeekClient();

        var res = await client.ChatAsync(request);
        
        return res;
    }


    public async Task<decimal> ComparePortraits(long chatId, long userId)
    {
        var chatPortrait = GetChatPortraitCached(chatId);
        var userPortrait = GetUserPortraitCached(chatId, userId);

        await Task.WhenAll(chatPortrait, userPortrait);
        
        using var client = new DeepSeekClient();
        
        var sb = new StringBuilder();

        sb.AppendLine(ComparePortraitsPrompt.PropmtHeader);
        
        foreach (var str in new[]{await chatPortrait, await userPortrait})
        {
            sb.AppendLine();
            sb.Append("\"");
            sb.Append(str);
            sb.Append("\"");
        }

        sb.AppendLine(ComparePortraitsPrompt.PromptEnd);
        
        var request = sb.ToString();
        
        var res = await client.ChatAsync(request);

        res = res.Replace('.', ',');

        if (decimal.TryParse(res, out var parsed))
            return parsed;
        
        return 1;
    }
    
    public void Dispose()
    {
        Client.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
    }

    class MessageModel
    {
        public TL.Message Message { get; set; }
        public long Weight { get; set; }
    }
}