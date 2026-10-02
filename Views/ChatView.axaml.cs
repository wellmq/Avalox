using Avalonia.Controls;
using Avalonia.Threading;
using Avalox.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;

namespace Avalox.Views;

// Code-behind for ChatView with auto-scroll to the latest message
public partial class ChatView : UserControl
{
    private ChatViewModel? _viewModel;

    public ChatView()
    {
        InitializeComponent();
    }

    // Subscribe to message updates when DataContext changes
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

    // Handle messages collection change
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatViewModel.CurrentChatMessages))
        {
            ScrollToLastMessage();
        }
    }

    // Scroll message list to the bottom
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
