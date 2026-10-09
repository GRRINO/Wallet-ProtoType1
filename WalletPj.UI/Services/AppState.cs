using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Reflection;
using System.Threading.Channels;

namespace WalletPj.UI.Services
{
    public class AppState(ProtectedLocalStorage storage)
    {
        private const string Key = "wallet.mobile";
        private Task? _load;
        public string? Mobile { get; private set; }
        public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Mobile);
        public event Action? Changed;
        public Task EnsureLoadedAsync() => _load ??= LoadAsync();


        private async Task LoadAsync()
        {
            try
            {
                var r = await storage.GetAsync<string>(Key);
                if (r.Success) Mobile = r.Value;
            }
            catch { /* ignore tampered / unreadable value */ }
            Changed?.Invoke();
        }

        public async Task LoginAsync(string mobile)
        {
            Mobile = mobile.Trim();
            await storage.SetAsync(Key, Mobile);
            Changed?.Invoke();
        }

        public async Task LogoutAsync()
        {
            Mobile = null;
            await storage.DeleteAsync(Key);
            Changed?.Invoke();
        }
    }
}
