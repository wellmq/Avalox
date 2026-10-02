using Avalonia.Controls;
using Avalonia.Threading;
using Avalox.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;

namespace Avalox.Views;

// Code-behind для ChatView с автоскроллом к последнему сообщению
public partial class ChatView : UserControl
{
    private ChatViewModel? _viewModel;

    public ChatView()
    {
        InitializeComponent();
    }

    // Подписка на смену сообщений при изменении DataContext
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as ChatViewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            ScrollToLastMessage();
        }
    }

    // Обработка обновления списка сообщений
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatViewModel.CurrentChatMessages))
        {
            ScrollToLastMessage();
        }
    }

    // Прокрутка списка сообщений в самый низ
    private void ScrollToLastMessage()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_viewModel != null && _viewModel.CurrentChatMessages.Count > 0)
            {
                var lastItem = _viewModel.CurrentChatMessages.Last();
                MessagesListBox.ScrollIntoView(lastItem);
            }
        }, DispatcherPriority.Background);
    }
}
