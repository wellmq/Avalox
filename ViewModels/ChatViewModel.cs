using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading;
using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace Avalox.ViewModels;

// Main chat screen ViewModel
public partial class ChatViewModel : ViewModelBase
{
    // Left sidebar chat previews
    [ObservableProperty]
    private ObservableCollection<ChatPreview> chatPreviews = [];

    // Currently selected chat
    [ObservableProperty]
    private ChatPreview? currentChat;

    // Messages in the active chat
    [ObservableProperty]
    private ObservableCollection<Message> currentChatMessages = [];

    // Active conversation partner username
    [ObservableProperty]
    private string currentChatLogin = "";

    // Active conversation partner online status
    [ObservableProperty]
    private string currentChatOnlineStatus = "";

    // Message draft input text
    [ObservableProperty]
    private string currentDraft = "";

    // Status bar response message
    [ObservableProperty]
    private string textResponse = "";

    // New chat target username
    [ObservableProperty]
    private string newChatLogin = "";

    private List<Message> messages = [];
    private string selfLogin;
    private Action<ViewModelBase> setCurrentViewModel;
    private Connection connection;
    private long lastMessageId = 0;
    private readonly CancellationTokenSource cancellationTokenSource = new();

    public ChatViewModel(Action<ViewModelBase> setCurrentViewModel, Connection connection, string selfLogin)
    {
        this.setCurrentViewModel = setCurrentViewModel;
        this.connection = connection;
        this.selfLogin = selfLogin;

        CancellationToken token = cancellationTokenSource.Token;

        // Background polling for messages and statuses every 500 ms
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(500, token);
                    await requestNewMessages();
                    await requestOnlineStatus();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Polling error: {ex.Message}");
                }
            }
        }, token);
    }

    // Chat selection handler
    partial void OnCurrentChatChanged(ChatPreview? oldValue, ChatPreview? newValue)
    {
        if (newValue == null) return;

        CurrentChatLogin = newValue.Login;
        updateCurrentChatMessages();
    }

    // Fetch incoming messages from server
    private async Task requestNewMessages()
    {
        LastMessageInfo lastMessageInfo = new LastMessageInfo(lastMessageId);
        Response response = await connection.MakeRequest(3, lastMessageInfo);

        if (!response.IsSuccessful || response.Obj == null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                TextResponse = !connection.GetConnectionStatus()
                    ? "Connection to server lost..."
                    : $"Server error: {response.Message}";
            });
            return;
        }

        try
        {
            List<Message>? newMessages = ((JsonElement)response.Obj).Deserialize<List<Message>>();

            if (newMessages != null && newMessages.Count > 0)
            {
                messages.AddRange(newMessages);
                lastMessageId = messages.Max(m => m.Id) ?? lastMessageId;

                Dispatcher.UIThread.Post(() =>
                {
                    updateCurrentChatMessages();
                    updateChatPreviews();
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Message parsing error: {ex.Message}");
        }

        Dispatcher.UIThread.Post(() => setTextResponse(response));
    }

    // Filter messages for the active conversation
    private void updateCurrentChatMessages()
    {
        if (string.IsNullOrEmpty(CurrentChatLogin))
        {
            CurrentChatMessages.Clear();
            return;
        }

        var filtered = messages
            .Where(m => (m.Sender == selfLogin && m.Receiver == CurrentChatLogin) ||
                        (m.Sender == CurrentChatLogin && m.Receiver == selfLogin))
            .OrderBy(m => m.Id)
            .ToList();

        foreach (var m in filtered)
        {
            m.IsMine = (m.Sender == selfLogin);
        }

        CurrentChatMessages = new ObservableCollection<Message>(filtered);
    }

    // Update previews and last messages in the sidebar
    private void updateChatPreviews()
    {
        var lastMessages = messages
            .GroupBy(m => m.Sender == selfLogin ? m.Receiver : m.Sender)
            .Select(g => g.OrderByDescending(m => m.Id).First())
            .ToList();

        foreach (var msg in lastMessages)
        {
            string partner = (msg.Sender == selfLogin) ? msg.Receiver : msg.Sender;
            var existing = ChatPreviews.FirstOrDefault(c => c.Login == partner);
            if (existing != null)
            {
                if (existing.LastMessage != msg.Text)
                {
                    int index = ChatPreviews.IndexOf(existing);
                    var updated = new ChatPreview(partner, msg.Text);
                    ChatPreviews[index] = updated;
                    if (partner == CurrentChatLogin)
                    {
                        CurrentChat = updated;
                    }
                }
            }
            else
            {
                ChatPreviews.Insert(0, new ChatPreview(partner, msg.Text));
            }
        }
    }

    // Query active partner online status
    private async Task requestOnlineStatus()
    {
        if (string.IsNullOrWhiteSpace(CurrentChatLogin)) return;
        TargetUser targetUser = new TargetUser(CurrentChatLogin);
        Response response = await connection.MakeRequest(4, targetUser);

        Dispatcher.UIThread.Post(() =>
        {
            if (!response.IsSuccessful)
            {
                CurrentChatOnlineStatus = "offline";
            }
            else
            {
                CurrentChatOnlineStatus = (response.Message == "yes") ? "online" : "offline";
            }
        });
    }

    private void setTextResponse(Response response)
    {
        string status = response.IsSuccessful ? "ok" : "fail";
        TextResponse = $"{status}: {response.Message}";
    }

    // Send chat message
    [RelayCommand]
    public async Task Send()
    {
        if (string.IsNullOrWhiteSpace(CurrentDraft) || string.IsNullOrWhiteSpace(CurrentChatLogin)) return;
        string textToSend = CurrentDraft;
        CurrentDraft = "";

        Message message = new Message(CurrentChatLogin, textToSend);
        Response response = await connection.MakeRequest(2, message);

        Dispatcher.UIThread.Post(() =>
        {
            if (!response.IsSuccessful)
            {
                TextResponse = $"Send error: {response.Message}";
                if (string.IsNullOrEmpty(CurrentDraft))
                {
                    CurrentDraft = textToSend;
                }
            }
            else
            {
                setTextResponse(response);
            }
        });
    }

    // Create new chat
    [RelayCommand]
    public void CreateChat()
    {
        string target = NewChatLogin?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(target) || target == selfLogin) return;

        ChatPreview? existing = ChatPreviews.FirstOrDefault(c => c.Login == target);
        if (existing == null)
        {
            existing = new ChatPreview(target, "");
            ChatPreviews.Insert(0, existing);
        }

        CurrentChat = existing;
        CurrentChatLogin = existing.Login;
        updateCurrentChatMessages();
        NewChatLogin = "";
    }

    // Delete chat from list
    [RelayCommand]
    public void DeleteChat(ChatPreview? chat)
    {
        if (chat == null) return;
        string targetLogin = chat.Login;

        if (CurrentChatLogin == targetLogin)
        {
            CurrentChat = null;
            CurrentChatLogin = "";
            CurrentChatOnlineStatus = "";
            CurrentChatMessages.Clear();
        }

        messages.RemoveAll(m => m.Sender == targetLogin || m.Receiver == targetLogin);

        ChatPreview? toRemove = ChatPreviews.FirstOrDefault(c => c.Login == targetLogin);
        if (toRemove != null)
        {
            ChatPreviews.Remove(toRemove);
        }
    }

    // Logout and return to auth view
    [RelayCommand]
    public void Logout()
    {
        cancellationTokenSource.Cancel();
        connection.Dispose();
        setCurrentViewModel(new RegAuthViewModel(setCurrentViewModel));
    }
}
