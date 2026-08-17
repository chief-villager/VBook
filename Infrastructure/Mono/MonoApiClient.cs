using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.WebUtilities;
using Mono.Net.Sdk.Models.Account;

namespace Bookkeeping.Infrastructure.Mono
{
   

    internal sealed class MonoApiClient(HttpClient http, ILogger<MonoApiClient> logger)
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public async Task<ExchangeTokenResponse> ExchangeTokenAsync(string code, CancellationToken ct)
        {
            using var response = await http.PostAsJsonAsync(
                "v2/accounts/auth", new { code }, Json, ct);

            return await ReadAsync<ExchangeTokenResponse>(response, ct);
        }

        public Task<MonoAccountResponse> GetAccountAsync(string accountId, CancellationToken ct)
            => SendAsync<MonoAccountResponse>(
                HttpMethod.Get, $"v2/accounts/{accountId}", ct);

        public Task<TransactionsResponse> GetTransactionsAsync(
            string accountId,
            DateOnly from,
            DateOnly to,
            string? cursor,
            CancellationToken ct)
        {
            // NB: verify the date format and pagination param names against the current
            // API reference before you rely on this — Mono's docs have shifted between v1 and v2.
            var query = new Dictionary<string, string?>
            {
                ["start"]    = from.ToString("dd-MM-yyyy"),
                ["end"]      = to.ToString("dd-MM-yyyy"),
                ["paginate"] = "true",
                ["limit"]    = "100",
                ["page"]     = cursor
            };

            var uri = QueryHelpers.AddQueryString(
                $"v2/accounts/{accountId}/transactions",
                query.Where(kv => kv.Value is not null));

            return SendAsync<TransactionsResponse>(HttpMethod.Get, uri, ct);
        }

        public Task UnlinkAsync(string accountId, CancellationToken ct)
            => SendAsync<MonoUnlinkResponse>(HttpMethod.Post, $"v2/accounts/{accountId}/unlink", ct);

        private async Task<T> SendAsync<T>(HttpMethod method, string uri, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(method, uri);
            using var response = await http.SendAsync(request, ct);
            return await ReadAsync<T>(response, ct);
        }

        private async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "Mono returned {StatusCode} for {Path}: {Body}",
                    (int)response.StatusCode, response.RequestMessage?.RequestUri?.PathAndQuery, body);

                throw new MonoApiException(HttpStatusCode.Forbidden, body);
            }

            return await response.Content.ReadFromJsonAsync<T>(Json, ct)
                ?? throw new MonoApiException(response.StatusCode, "Empty response body.");
        }
    }

    public sealed class MonoApiException(HttpStatusCode status, string body)
        : Exception($"Mono API call failed with {(int)status}: {body}")
    {
        public HttpStatusCode StatusCode { get; } = status;
    }

    // Mono wraps the account id under a "data" envelope: { "data": { "id": "..." } }.
    // Expose Id off the top so callers stay ignorant of the envelope.
    internal sealed record ExchangeTokenResponse(ExchangeTokenData Data)
    {
        public string Id => Data.Id;
    }

    internal sealed record ExchangeTokenData(string Id);

    internal sealed record TransactionsResponse(
        IReadOnlyList<MonoTransaction> Data,
        MonoMeta Meta);

  
    // Mono sends page/total as null when there's nothing to paginate, so these must be
    // nullable ints. Note "next" is a *full URL* (…/transactions?page=2), not a bare
    // cursor — NextPage extracts the page number so the caller can pass it back as the
    // `page` query param.
    internal sealed record MonoMeta(int? Total, int? Page, string? Next)
    {
        public string? NextPage =>
            !string.IsNullOrEmpty(Next)
            && QueryHelpers.ParseQuery(new Uri(Next).Query).TryGetValue("page", out var page)
                ? page.ToString()
                : null;
    }
}