using System.Net;
using System.Text;
using System.Text.Json;

namespace WalletPj.UI.Services
{

    public class ApiException(string message) : Exception(message);

    // ---- UI models (adjust property names if your API DTOs differ) ----
    public class WalletInfo
    {
        public string MobileNo { get; set; } = "";
        public string? Name { get; set; }
        public decimal Balance { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class TxItem
    {
        public string Type { get; set; } = "";
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public DateTime? Date { get; set; }
        public decimal? BalanceAfter { get; set; }
        public string? Counterparty { get; set; }
        public bool IsCredit { get; set; }
    }

    public record StatementPage(List<TxItem> Items, int PageSize)
    {
        public bool HasMore => Items.Count >= PageSize;
    }

    public static class Fmt
    {
        public static string Money(decimal v) => v.ToString("N2");
    }
    public class WalletApiClient(HttpClient http)
    {
        private static readonly JsonSerializerOptions J = new(JsonSerializerDefaults.Web);
        private static string M(string mobile) => Uri.EscapeDataString(mobile);

        public async Task<WalletInfo> GetWalletAsync(string mobile) =>
       JsonSerializer.Deserialize<WalletInfo>(await SendAsync(HttpMethod.Get, $"api/wallets/{M(mobile)}"), J)
       ?? throw new ApiException("Wallet not found.");

        public Task RegisterAsync( string name, string mobile, string pin) =>
        SendAsync(HttpMethod.Post, "api/wallets", new { FullName = name, MobileNo = mobile, Pin = pin });

        public Task DepositAsync(string mobile, decimal amount, string? note) =>
            SendAsync(HttpMethod.Post, $"api/wallets/{M(mobile)}/deposit", new { Amount = amount, Note = note });

        public Task WithdrawAsync(string mobile, decimal amount, string pin, string? note) =>
            SendAsync(HttpMethod.Post, $"api/wallets/{M(mobile)}/withdraw", new { Amount = amount, Pin = pin, Note = note });

        public Task TransferAsync(string mobile, string toMobile, decimal amount, string pin, string? note) =>
            SendAsync(HttpMethod.Post, $"api/wallets/{M(mobile)}/transfer",
                new { ToMobileNo = toMobile, Amount = amount, Pin = pin, Note = note });

        public Task ChangePinAsync(string mobile, string oldPin, string newPin) =>
            SendAsync(HttpMethod.Put, $"api/wallets/{M(mobile)}/pin", new { OldPin = oldPin, NewPin = newPin });

        public Task SetActiveAsync(string mobile, bool active) =>
            SendAsync(HttpMethod.Put, $"api/wallets/{M(mobile)}/{(active ? "activate" : "deactivate")}");

        public async Task<StatementPage> GetStatementAsync(string mobile, DateTime? from, DateTime? to, int page, int pageSize)
        {
            var q = new StringBuilder($"api/wallets/{M(mobile)}/statement?page={page}&pageSize={pageSize}");
            if (from.HasValue) q.Append($"&from={from.Value:yyyy-MM-dd}");
            if (to.HasValue) q.Append($"&to={to.Value:yyyy-MM-dd}");

            var text = await SendAsync(HttpMethod.Get, q.ToString());
            var items = new List<TxItem>();
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            JsonElement? arr = null;
            if (root.ValueKind == JsonValueKind.Array) arr = root;
            else if (root.ValueKind == JsonValueKind.Object)
                foreach (var name in new[] { "items", "data", "transactions", "results", "statement" })
                    if (TryGet(root, out var e, name) && e.ValueKind == JsonValueKind.Array) { arr = e; break; }

            if (arr is { } a)
                foreach (var el in a.EnumerateArray()) items.Add(ParseTx(el));

            return new StatementPage(items, pageSize);
        }

        // ---------- helpers ----------
        private async Task<string> SendAsync(HttpMethod method, string url, object? body = null)
        {
            using var req = new HttpRequestMessage(method, url);
            if (body is not null) req.Content = JsonContent.Create(body, options: J);

            HttpResponseMessage resp;
            try { resp = await http.SendAsync(req); }
            catch (HttpRequestException) { throw new ApiException("Cannot reach the server. Please try again."); }

            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new ApiException(ParseError(text, resp.StatusCode));
            return text;
        }

        private static string ParseError(string text, HttpStatusCode status)
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                var r = doc.RootElement;
                if (r.ValueKind == JsonValueKind.Object)
                {
                    if (TryGet(r, out var errs, "errors") && errs.ValueKind == JsonValueKind.Object)
                        foreach (var p in errs.EnumerateObject())
                            if (p.Value.ValueKind == JsonValueKind.Array && p.Value.GetArrayLength() > 0)
                                return p.Value[0].GetString() ?? "Validation failed.";
                    foreach (var n in new[] { "detail", "message", "error", "title" })
                        if (TryGet(r, out var e, n) && e.ValueKind == JsonValueKind.String) return e.GetString()!;
                }
            }
            catch { /* not json */ }

            if (!string.IsNullOrWhiteSpace(text) && text.Length < 200) return text.Trim('"');
            return status switch
            {
                HttpStatusCode.NotFound => "Wallet not found.",
                HttpStatusCode.BadRequest => "Invalid request.",
                _ => $"Something went wrong ({(int)status})."
            };
        }

        private static bool TryGet(JsonElement obj, out JsonElement value, params string[] names)
        {
            foreach (var p in obj.EnumerateObject())
                foreach (var n in names)
                    if (string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase)) { value = p.Value; return true; }
            value = default;
            return false;
        }

        private static string? Str(JsonElement o, params string[] n) =>
            TryGet(o, out var v, n) && v.ValueKind != JsonValueKind.Null ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString()) : null;

        private static decimal? Dec(JsonElement o, params string[] n) =>
            TryGet(o, out var v, n) && v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d) ? d : null;

        private static TxItem ParseTx(JsonElement e)
        {
            var type = Str(e, "type", "transactionType", "txType") ?? "";
            var amount = Dec(e, "amount") ?? 0;
            var t = type.ToLowerInvariant();

            bool credit;
            if (t.Contains("withdraw") || t.Contains("debit") || t.Contains("out") || t.Contains("sent")) credit = false;
            else if (t.Contains("deposit") || t.Contains("credit") || t.Contains("in") || t.Contains("receiv")) credit = true;
            else credit = amount >= 0;

            DateTime? date = null;
            var ds = Str(e, "createdAt", "createdOn", "date", "transactionDate", "timestamp");
            if (DateTime.TryParse(ds, out var dt)) date = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;

            return new TxItem
            {
                Type = type,
                Amount = Math.Abs(amount),
                Note = Str(e, "note", "description", "remarks"),
                Date = date,
                BalanceAfter = Dec(e, "balanceAfter", "balance", "runningBalance"),
                Counterparty = Str(e, "counterparty", "toMobileNo", "fromMobileNo", "otherParty"),
                IsCredit = credit
            };
        }
    }
}
