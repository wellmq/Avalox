using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net;
using System.Threading.Tasks;
using System;

namespace Avalox.ViewModels;

// ViewModel for user registration and authentication
public partial class RegAuthViewModel : ViewModelBase
{
    // Username (up to 32 characters)
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    [NotifyCanExecuteChangedFor(nameof(AuthCommand))]
    private string login = "";

    // Password (up to 32 characters)
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    [NotifyCanExecuteChangedFor(nameof(AuthCommand))]
    private string password = "";

    // Server address in IP:Port format (defaults to 127.0.0.1:7777)
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    [NotifyCanExecuteChangedFor(nameof(AuthCommand))]
    private string ipAndPort = "127.0.0.1:7777";

    // Server response message on status bar
    [ObservableProperty]
    private string textResponse = "";

    // Input validation warning message
    [ObservableProperty]
    private string textWarning = "";

    // TCP connection to server
    private Connection connection;

    // View navigation callback to switch to ChatViewModel
    private Action<ViewModelBase> setCurrentViewModel;

    public RegAuthViewModel(Action<ViewModelBase> setCurrentViewModelCallback)
    {
        connection = new Connection();
        setCurrentViewModel = setCurrentViewModelCallback;
    }

    // Connect to server via TCP
    private async Task<bool> connect()
    {
        if (!IPEndPoint.TryParse(IpAndPort, out IPEndPoint? ip) || !IpAndPort.Contains(":"))
        {
            TextWarning = "invalid ip:port";
            return false;
        }

        bool result = await connection.TryConnect(ip);
        if (!result)
        {
            TextWarning = "failed to connect";
            return false;
        }
        return true;
    }

    // Validate inputs before sending command
    private bool canAnything()
    {
        bool isNotEmpty = !string.IsNullOrWhiteSpace(Login)
          && !string.IsNullOrWhiteSpace(Password)
          && !string.IsNullOrWhiteSpace(IpAndPort);

        bool isIpCorrect = IPEndPoint.TryParse(IpAndPort, out IPEndPoint? ip) && IpAndPort.Contains(":");
        bool isLoginOrPasswordNotTooLong = (Login.Length < 32) && (Password.Length < 32);

        return isNotEmpty && isIpCorrect && isLoginOrPasswordNotTooLong;
    }

    // Clear form error messages
    private void cleanScreenText()
    {
        TextWarning = "";
        TextResponse = "";
    }

    // Display formatted server response
    private void SetTextResponse(Response response)
    {
        string status = (response.IsSuccessful) ? "ok" : "fail";
        TextResponse = $"{status}: {response.Message}";
    }

    // Register new user account (type 0)
    [RelayCommand(CanExecute = nameof(canAnything))]
    public async Task Register()
    {
        cleanScreenText();

        if (!connection.GetConnectionStatus())
        {
            bool connected = await connect();
            if (!connected) return;
        }

        Credentials credentials = new Credentials(Login, Password);
        Response response = await connection.MakeRequest(0, credentials);
        SetTextResponse(response);
    }

    // Authenticate user (type 1) and navigate to ChatView
    [RelayCommand(CanExecute = nameof(canAnything))]
    public async Task Auth()
    {
        cleanScreenText();

        if (!connection.GetConnectionStatus())
        {
            bool connected = await connect();
            if (!connected) return;
        }

        Credentials credentials = new Credentials(Login, Password);
        Response response = await connection.MakeRequest(1, credentials);
        SetTextResponse(response);

        if (response.IsSuccessful)
        {
            await Task.Delay(1000);
            setCurrentViewModel(new ChatViewModel(setCurrentViewModel, connection, Login));
        }
    }
}
