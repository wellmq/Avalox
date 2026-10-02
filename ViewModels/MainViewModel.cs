using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalox.ViewModels;

// Корневая ViewModel приложения для переключения между экранами
public partial class MainViewModel : ViewModelBase
{
    // Текущий активный экран
    [ObservableProperty]
    private ViewModelBase currentViewModel;

    public MainViewModel()
    {
        // Стартовый экран — вход / регистрация
        currentViewModel = new RegAuthViewModel(setCurrentViewModel);
    }

    // Смена текущего экрана
    private void setCurrentViewModel(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
    }
}
