using Avalonia.Controls;
using MinecraftServerManager.ViewModels;

namespace MinecraftServerManager.Views;

public partial class ClusterWizardWindow : Window
{
    public ClusterWizardWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ClusterWizardViewModel vm)
                vm.AttachWindow(this);
        };
    }
}
