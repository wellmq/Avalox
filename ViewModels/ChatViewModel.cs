using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace Avalox.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty]
    private List<ChatPreview> chatPreviews = [];
    [ObservableProperty]
    private ChatPreview? currentChat;
    [ObservableProperty]
    private List<Message> currentChatMessages = [];
    [ObservableProperty]
    private string currentChatLogin;
    [ObservableProperty]
    private string currentChatOnlineStatus;
    [ObservableProperty]
    private string currentDraft;
    [ObservableProperty]
    private string textResponse;
    private List<Message> messages = [];
    private string selfLogin;
    private Action<ViewModelBase> setCurrentViewModel;
    private Connection connection;
    private long lastMessageId = 0;

    public ChatViewModel(Action<ViewModelBase> setCurrentViewModel, Connection connection, string selfLogin)
    {
        this.setCurrentViewModel = setCurrentViewModel;
        this.connection = connection;
        this.selfLogin = selfLogin;
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(500);
                try
                {
                    await requestNewMessages();
                    await requestOnlineStatus();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"error: {ex.Message}{ex.StackTrace}");
                }
            }
        });
    }

    partial void OnCurrentChatChanged(ChatPreview oldPreview, ChatPreview newPreview)
    {
        CurrentChatLogin = newPreview.Login;
        updateCurrentChatMessages();
    }

    private async Task requestNewMessages()
    {
        LastMessageInfo lastMessageInfo = new LastMessageInfo(lastMessageId);
        Response response = await connection.MakeRequest(3, lastMessageInfo);
        List<Message> newMessages = ((JsonElement)response.Obj).Deserialize<List<Message>>();
        if (newMessages.Count > 0)
        {
            messages.AddRange(newMessages);
            lastMessageId = messages.Max(m => m.Id) ?? 0;
            updateCurrentChatMessages();
            updateChatPreviews();
        }
        setTextResponse(response);
    }
    private void updateCurrentChatMessages()
    {
        CurrentChatMessages = messages.Where(m => (m.Sender == selfLogin && m.Receiver == CurrentChatLogin) || (m.Sender == CurrentChatLogin && m.Receiver == selfLogin)).OrderBy(m => m.Id)
        .ToList();
    }
    private void updateChatPreviews()
    {
        List<Message> lastMessages = messages.GroupBy(m => m.Sender == selfLogin ? m.Receiver : m.Sender)
          .Select(g => g.OrderByDescending(m => m.Id).First())
          .ToList();
        List<ChatPreview> newChatPreviews = [];
        foreach (Message msg in lastMessages)
        {
            newChatPreviews.Add(new ChatPreview((msg.Sender == selfLogin) ? msg.Receiver : msg.Sender, msg.Text));
        }
        ChatPreviews = newChatPreviews;
    }

    private async Task requestOnlineStatus()
    {
        if (string.IsNullOrWhiteSpace(currentChatLogin)) return;
        TargetUser TargetUser = new TargetUser(currentChatLogin);
        Response response = await connection.MakeRequest(4, TargetUser);
        if (response.Message == "yes") CurrentChatOnlineStatus = "online";
        else CurrentChatOnlineStatus = "offline";
    }

    private void setTextResponse(Response response)
    {
        string status = (response.IsSuccessful) ? "ok" : "fail";
        TextResponse = $"{status}: {response.Message}";
    }

    [RelayCommand]
    public async Task Send()
    {
        if (string.IsNullOrWhiteSpace(currentDraft) || string.IsNullOrWhiteSpace(currentChatLogin)) return;
        Message message = new Message(currentChatLogin, currentDraft);
        Response response = await connection.MakeRequest(2, message);
        CurrentDraft = "";
        setTextResponse(response);
    }
}
