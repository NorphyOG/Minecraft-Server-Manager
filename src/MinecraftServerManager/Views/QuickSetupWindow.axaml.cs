using Avalonia.Controls;
using MinecraftServerManager.ViewModels;

namespace MinecraftServerManager.Views;

public partial class QuickSetupWindow : Window
{
    public QuickSetupWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is QuickSetupViewModel vm)
                vm.AttachWindow(this);
        };
    }
}
