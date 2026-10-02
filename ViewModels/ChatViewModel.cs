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

// Экран переписки
public partial class ChatViewModel : ViewModelBase
{
    // Список чатов слева
    [ObservableProperty]
    private ObservableCollection<ChatPreview> chatPreviews = [];

    // Выбранный чат
    [ObservableProperty]
    private ChatPreview? currentChat;

    // Сообщения текущего чата
    [ObservableProperty]
    private ObservableCollection<Message> currentChatMessages = [];

    // Логин собеседника
    [ObservableProperty]
    private string currentChatLogin = "";

    // Статус собеседника
    [ObservableProperty]
    private string currentChatOnlineStatus = "";

    // Текст сообщения в поле ввода
    [ObservableProperty]
    private string currentDraft = "";

    // Сообщение в строке статуса
    [ObservableProperty]
    private string textResponse = "";

    // Логин для создания нового чата
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

        // Опрос сервера каждые 500 мс
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
                    Console.WriteLine($"Ошибка опроса: {ex.Message}");
                }
            }
        }, token);
    }

    // Выбор чата из списка
    partial void OnCurrentChatChanged(ChatPreview? oldValue, ChatPreview? newValue)
    {
        if (newValue == null) return;

        CurrentChatLogin = newValue.Login;
        updateCurrentChatMessages();
    }

    // Получение новых сообщений
    private async Task requestNewMessages()
    {
        LastMessageInfo lastMessageInfo = new LastMessageInfo(lastMessageId);
        Response response = await connection.MakeRequest(3, lastMessageInfo);

        if (!response.IsSuccessful || response.Obj == null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                TextResponse = !connection.GetConnectionStatus()
                    ? "Потеряна связь с сервером..."
                    : $"Ошибка сервера: {response.Message}";
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
            Console.WriteLine($"Ошибка парсинга сообщений: {ex.Message}");
        }

        Dispatcher.UIThread.Post(() => setTextResponse(response));
    }

    // Фильтрация сообщений для выбранного собеседника
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

    // Обновление превью и последних сообщений в списке чатов
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

    // Проверка онлайн-статуса
    private async Task requestOnlineStatus()
    {
        if (string.IsNullOrWhiteSpace(CurrentChatLogin)) return;
        TargetUser targetUser = new TargetUser(CurrentChatLogin);
        Response response = await connection.MakeRequest(4, targetUser);

        Dispatcher.UIThread.Post(() =>
        {
            if (!response.IsSuccessful)
            {
                CurrentChatOnlineStatus = "нет связи";
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

    // Отправка сообщения
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
                TextResponse = $"Ошибка отправки: {response.Message}";
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

    // Создание чата
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

    // Удаление чата
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

    // Выход из профиля
    [RelayCommand]
    public void Logout()
    {
        cancellationTokenSource.Cancel();
        connection.Dispose();
        setCurrentViewModel(new RegAuthViewModel(setCurrentViewModel));
    }
}
