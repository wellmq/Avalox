using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net;
using System.Threading.Tasks;
using System;

namespace Avalox.ViewModels;

public partial class RegAuthViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    [NotifyCanExecuteChangedFor(nameof(AuthCommand))]
    private string login = "";
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    [NotifyCanExecuteChangedFor(nameof(AuthCommand))]
    private string password = "";
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    [NotifyCanExecuteChangedFor(nameof(AuthCommand))]
    private string ipAndPort = "127.0.0.1:7777";
    [ObservableProperty] private string textResponse;
    [ObservableProperty] private string textWarning;
    private Connection connection;
    private Action<ViewModelBase> setCurrentViewModel;

    public RegAuthViewModel(Action<ViewModelBase> setCurrentViewModelCallback)
    {
        connection = new Connection();
        setCurrentViewModel = setCurrentViewModelCallback;
    }

    private async Task<bool> connect()
    {
        IPEndPoint ip = IPEndPoint.Parse(ipAndPort);
        bool result = await connection.TryConnect(ip);
        if (!result)
        {
            TextWarning = "connection failed";
            return false;
        }
        return true;
    }

    private bool canAnything()
    {
        bool isNotEmpty = !string.IsNullOrWhiteSpace(login)
          && !string.IsNullOrWhiteSpace(password)
          && !string.IsNullOrWhiteSpace(ipAndPort);
        bool isIpCorrect = IPEndPoint.TryParse(ipAndPort, out IPEndPoint ip) && ipAndPort.Contains(":");
        bool isLoginOrPasswordNotTooLong = (login.Length < 32) && (password.Length < 32);
        return isNotEmpty && isIpCorrect && isLoginOrPasswordNotTooLong;
    }

    private void cleanScreenText()
    {
        TextWarning = "";
        TextResponse = "";
    }

    private void SetTextResponse(Response response)
    {
        string status = (response.IsSuccessful) ? "ok" : "fail";
        TextResponse = $"{status}: {response.Message}";
    }

    [RelayCommand(CanExecute = nameof(canAnything))]
    public async Task Register()
    {
        cleanScreenText();
        if (!connection.GetConnectionStatus()) await connect();
        if (!connection.GetConnectionStatus()) return;
        Credentials credentials = new Credentials(login, password);
        Response response = await connection.MakeRequest(0, credentials);
        SetTextResponse(response);
    }

    [RelayCommand(CanExecute = nameof(canAnything))]
    public async Task Auth()
    {
        cleanScreenText();
        if (!connection.GetConnectionStatus()) await connect();
        if (!connection.GetConnectionStatus()) return;
        Credentials credentials = new Credentials(login, password);
        Response response = await connection.MakeRequest(1, credentials);
        SetTextResponse(response);
        if (response.IsSuccessful)
        {
            await Task.Delay(1000);
            setCurrentViewModel(new ChatViewModel(setCurrentViewModel, connection, login));
        }
    }
}
