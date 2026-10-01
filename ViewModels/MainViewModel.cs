using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalox.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        currentViewModel = new RegAuthViewModel(setCurrentViewModel);
    }
    [ObservableProperty]
    private ViewModelBase currentViewModel;

    private void setCurrentViewModel(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
    }
}
