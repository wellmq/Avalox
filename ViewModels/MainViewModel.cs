using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalox.ViewModels;

// Root ViewModel managing navigation between views
public partial class MainViewModel : ViewModelBase
{
    // Currently active ViewModel
    [ObservableProperty]
    private ViewModelBase currentViewModel;

    public MainViewModel()
    {
        // Initial view is authentication / registration
        currentViewModel = new RegAuthViewModel(setCurrentViewModel);
    }

    // Switch current active view
    private void setCurrentViewModel(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
    }
}
