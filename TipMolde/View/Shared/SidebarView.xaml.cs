using TipMolde.ViewModel;

namespace TipMolde.View.Shared;

public partial class SidebarView : ContentView
{
    private double _sidebarWidth;
    private bool _showLabels;

    public double SidebarWidth
    {
        get => _sidebarWidth;
        set
        {
            _sidebarWidth = value;
            OnPropertyChanged();
        }
    }

    public bool ShowLabels
    {
        get => _showLabels;
        set
        {
            _showLabels = value;
            OnPropertyChanged();
        }
    }

    private bool IsPhone => DeviceInfo.Current.Idiom == DeviceIdiom.Phone;

    public SidebarView()
    {
        InitializeComponent();
        BindingContext = new SidebarViewModel();
        ConfigureSidebar();
    }

    private void ConfigureSidebar()
    {
        if (IsPhone)
            SetExpanded(false);
        else
            SetExpanded(true);
    }

    private void SetExpanded(bool expanded)
    {
        ShowLabels = expanded;

        if (expanded)
            SidebarWidth = IsPhone ? 230 : 280;
        else
            SidebarWidth = IsPhone ? 88 : 96;
    }

    private void OnHeaderTapped(object sender, TappedEventArgs e)
    {
        if (!IsPhone)
            return;

        SetExpanded(!ShowLabels);
    }
}