namespace DesktopAgent.Services
{
    public interface ILLMClient
    {
        Task<bool> CheckStatusAsync();
        Task<string[]> ListModelsAsync();
        Task<string> ChatAsync(string model, IEnumerable<ChatMessage> messages, CancellationToken ct = default);
    }

}
