using HackService.Models;
using HackService.Models.GetBio;
using HackService.Models.Login;
using HackService.Models.UserPortrait;
using HackService.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace HackService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder();
        
        var app = builder.Build();

        CheckConfig(builder);
        
        DeepSeekClient.ApiKey = builder.Configuration["DeepSeekClient:ApiKey"];
        
        using var tgConnector =  new TgConnector(
                builder.Configuration["TgUserClient:ApiId"],
                builder.Configuration["TgUserClient:ApiHash"],
                builder.Configuration["TgUserClient:Phone"]
            );

        //TODO: Proper DI!
        using var userInfoService = new UserInfoService(tgConnector);
        using var userChatPortraits = new ChatInfoService(tgConnector);
        
        
        app.Urls.Add(builder.Configuration["Url"]);
        
        app.MapPost("/api/v1/verificationCode/{code}", async ([FromRoute] string code) =>
        {
            return TypedResults.Ok(tgConnector.VerificationCode = code);
        });
        
        
        app.MapPost("/api/v1/login", async (LoginRequest req) =>
        {
            return TypedResults.Ok(await userInfoService.LoginToChat(req.ChatId));
        });
        
        
        app.MapPost("/api/v1/getChatInfo", async (UserInfoRequest req) =>
        {
            return TypedResults.Ok(await userInfoService.GetUserInfo(req.ChatId, req.UserId));
        });
        
        app.MapPost("/api/v1/getBio", async (UserInfoRequest request) =>
        {
            return TypedResults.Ok(await userInfoService.GetUserBio(request.UserId.Value, request.ChatId));
        });

        app.MapGet("/api/v1/samvel", () =>
        {
            return TypedResults.PhysicalFile("/Users/hmorgan/Downloads/melon.jpeg", "image/jpeg");
        });
        
        app.MapPost("/api/v1/getChatPortrait", async (ChatPortraitRequest req) =>
        {
            return TypedResults.Ok(await userChatPortraits.GetChatPortraitCached(req.ChatId));
        });
        
        app.MapPost("/api/v1/getUserPortrait", async (UserPortraitRequest req)  =>
        {
            return TypedResults.Ok(await userChatPortraits.GetUserPortraitCached(req.ChatId, req.UserId));
        });
        
        app.MapPost("/api/v1/compareUserPortraits", async (UserPortraitRequest req)  =>
        {
            return TypedResults.Ok(await userChatPortraits.ComparePortraits(req.ChatId, req.UserId));
        });
        
        app.MapOpenApi();

        app.Run();
    }

    private static void CheckConfig(WebApplicationBuilder builder)
    {
        var deepseekapikey = builder.Configuration["DeepSeekClient:ApiKey"];
        
        var apiid = builder.Configuration["TgUserClient:ApiId"];
        var apihash = builder.Configuration["TgUserClient:ApiHash"];
        var phone = builder.Configuration["TgUserClient:Phone"];


        if (string.IsNullOrWhiteSpace(deepseekapikey) || string.IsNullOrWhiteSpace(apiid) ||
            string.IsNullOrWhiteSpace(apihash) || string.IsNullOrWhiteSpace(phone))
        {
            Console.WriteLine($"Check config depsek {deepseekapikey} apiid {apiid} apihash {apihash} phone {phone}");
        }
    }
}