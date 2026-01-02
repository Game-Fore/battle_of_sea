gigiusing System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace battle_sea.Models.Chat
{
    public interface IChatService
    {
        event Action<ChatMessage>? MessageReceived;
        ObservableCollection<ChatMessage> Messages { get; }
        Task SendMessageAsync(ChatMessage message);
        Task ConnectAsync(string playerName);
        Task DisconnectAsync();
    }
}