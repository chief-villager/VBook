using Bookkeeping.Domain.Common;

namespace Bookkeeping.Application.ChatBot;

// Port for the AI assistant. The business is fixed by the caller (the authorized route),
// never chosen by the model, so a prompt can't steer it into another business's books.
public interface IChatBotService
{
    Task<Result<string>> AskAsync(BusinessId businessId, string question, CancellationToken ct = default);
}
