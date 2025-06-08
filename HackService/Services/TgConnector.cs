using HackService.Models;
using TL;
using WTelegram;

namespace HackService.Services;

public class TgConnector : IDisposable, IAsyncDisposable
{
    public Client Client { get; set; }
    
    public string ApiId { get; set; }
    
    public string ApiHash { get; set; }
    
    public string Phone { get; set; }
    
    public string VerificationCode { get; set; }

    public TgConnector(string apiId, string apiHash, string phone, string verificationCode = null)
    {
        ApiId = apiId;
        ApiHash = apiHash;
        Phone = phone;
        VerificationCode = verificationCode;
        Client = new WTelegram.Client(Config);
    }
    
    string Config(string what)
    {
        switch (what)
        {
            case "api_id": return ApiId;
            case "api_hash": return ApiHash;
            case "phone_number": return Phone;
            case "verification_code":
            {
                if (string.IsNullOrEmpty(VerificationCode))
                {
                    while (VerificationCode == null)
                    {
                        Console.WriteLine("Awaiting verification code");
                        
                        Task.Delay(1000).Wait();
                    }
                }
                return VerificationCode;
            }
            case "session_pathname": return "session_file";
            // case "first_name": return "John";      // if sign-up is required
            // case "last_name": return "Doe";        // if sign-up is required
            // case "password": return "secret!";     // if user has enabled 2FA
            default: return null;                  // let WTelegramClient decide the default config
        }
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