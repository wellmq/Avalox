using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net;
using System.Threading.Tasks;
using System;

namespace Avalox.ViewModels;

// ViewModel экрана регистрации и входа
public partial class RegAuthViewModel : ViewModelBase
{
    // Логин (до 32 символов)
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    [NotifyCanExecuteChangedFor(nameof(AuthCommand))]
    private string login = "";

    // Пароль (до 32 символов)
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    [NotifyCanExecuteChangedFor(nameof(AuthCommand))]
    private string password = "";

    // Адрес сервера в формате IP:Port (по умолчанию 127.0.0.1:7777)
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    [NotifyCanExecuteChangedFor(nameof(AuthCommand))]
    private string ipAndPort = "127.0.0.1:7777";

    // Сообщение об ответе сервера в нижней строке
    [ObservableProperty]
    private string textResponse = "";

    // Предупреждение / ошибка валидации в нижней строке
    [ObservableProperty]
    private string textWarning = "";

    // Сетевое подключение к серверу
    private Connection connection;

    // Делегат для перехода в ChatViewModel
    private Action<ViewModelBase> setCurrentViewModel;

    public RegAuthViewModel(Action<ViewModelBase> setCurrentViewModelCallback)
    {
        connection = new Connection();
        setCurrentViewModel = setCurrentViewModelCallback;
    }

    // Попытка TCP-подключения к серверу
    private async Task<bool> connect()
    {
        if (!IPEndPoint.TryParse(IpAndPort, out IPEndPoint? ip) || !IpAndPort.Contains(":"))
        {
            TextWarning = "неверный ip:порт";
            return false;
        }

        bool result = await connection.TryConnect(ip);
        if (!result)
        {
            TextWarning = "не удалось подключиться";
            return false;
        }
        return true;
    }

    // Проверка заполненности полей и длины перед отправкой
    private bool canAnything()
    {
        bool isNotEmpty = !string.IsNullOrWhiteSpace(Login)
          && !string.IsNullOrWhiteSpace(Password)
          && !string.IsNullOrWhiteSpace(IpAndPort);

        bool isIpCorrect = IPEndPoint.TryParse(IpAndPort, out IPEndPoint? ip) && IpAndPort.Contains(":");
        bool isLoginOrPasswordNotTooLong = (Login.Length < 32) && (Password.Length < 32);

        return isNotEmpty && isIpCorrect && isLoginOrPasswordNotTooLong;
    }

    // Очистка текста ошибок на форме
    private void cleanScreenText()
    {
        TextWarning = "";
        TextResponse = "";
    }

    // Вывод статуса ответа сервера
    private void SetTextResponse(Response response)
    {
        string status = (response.IsSuccessful) ? "ok" : "fail";
        TextResponse = $"{status}: {response.Message}";
    }

    // Регистрация нового аккаунта (тип 0)
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

    // Авторизация (тип 1) и переход в чат
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
