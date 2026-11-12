using System.ComponentModel;
using Bookkeeping.Application.ChatBot;
using Bookkeeping.Application.Reporting;
using Bookkeeping.Application.Transactions;
using Bookkeeping.Domain.Common;
using Microsoft.Extensions.AI;

namespace Bookkeeping.Infrastructure.ChatBot;

public sealed class ChatBotService : IChatBotService
{
    public const int MaxQuestionLength = 2000;

    private readonly IChatClient _chatClient;
    private readonly ITransactionService _transactions;
    private readonly IFinancialReportingService _reporting;

    public ChatBotService(IChatClient chatClient, ITransactionService transactions, IFinancialReportingService reporting)
    {
        _chatClient = chatClient;
        _transactions = transactions;
        _reporting = reporting;
    }

    public async Task<Result<string>> AskAsync(BusinessId businessId, string question, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            return Result<string>.Failure("Question is required.");
        if (question.Length > MaxQuestionLength)
            return Result<string>.Failure($"Question must be at most {MaxQuestionLength} characters.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        List<ChatMessage> chat =
        [
            new(ChatRole.System, $"""
                You are a helpful assistant that answers questions about this business's financials and transactions.
                Today's date is {today:yyyy-MM-dd}. Amounts are in Nigerian Naira (NGN).

                Rules:
                1. Only answer questions about the business's financials and transactions.
                2. Use the provided tools to get data; dates are yyyy-MM-dd.
                3. Do not make up answers. If you don't know the answer, say "I don't know."
                4. Do not provide information unrelated to the business's financials and transactions.
                """),
            new(ChatRole.User, question.Trim())
        ];

        var response = await _chatClient.GetResponseAsync(chat, new ChatOptions { Tools = ToolsFor(businessId) }, ct);
        return Result<string>.Success(response.Text);
    }

    // Tools are built per request and close over the route's business id, so the model
    // only supplies dates. Exposing BusinessId as a tool parameter would let a prompt
    // read any business's data, bypassing the per-business permission checks.
    private List<AITool> ToolsFor(BusinessId businessId) =>
    [
        AIFunctionFactory.Create(
            [Description("Lists the business's transactions between two dates (inclusive), newest first. Use it for questions about a specific transaction, transactions or spending/income detail.")]
            (DateOnly from, DateOnly to, CancellationToken ct) => _transactions.ListAsync(businessId, new DateRange(from, to), ct),
            "list_transactions"),
        AIFunctionFactory.Create(
            [Description("Returns the profit and loss statement (revenue, expenses, net profit) between two dates (inclusive).")]
            (DateOnly from, DateOnly to, CancellationToken ct) => _reporting.ProfitAndLossAsync(businessId, new DateRange(from, to), ct),
            "profit_and_loss"),
        AIFunctionFactory.Create(
            [Description("Returns the balance sheet (assets, liabilities, equity) as at a date.")]
            (DateOnly asOf, CancellationToken ct) => _reporting.BalanceSheetAsync(businessId, asOf, ct),
            "balance_sheet"),
        AIFunctionFactory.Create(
            [Description("Returns the cash flow statement (opening cash, closing cash, net change) between two dates (inclusive).")]
            (DateOnly from, DateOnly to, CancellationToken ct) => _reporting.CashFlowAsync(businessId, new DateRange(from, to), ct),
            "cash_flow"),
    ];
}
