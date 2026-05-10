namespace GTEK.FSM.MobileApp.Controls;

public partial class RoleBottomNavigationBar : ContentView
{
    public static readonly BindableProperty RoleProperty = BindableProperty.Create(
        nameof(Role),
        typeof(string),
        typeof(RoleBottomNavigationBar),
        "Customer",
        propertyChanged: OnNavigationPropertyChanged);

    public static readonly BindableProperty ActiveItemProperty = BindableProperty.Create(
        nameof(ActiveItem),
        typeof(string),
        typeof(RoleBottomNavigationBar),
        "Home",
        propertyChanged: OnNavigationPropertyChanged);

    public RoleBottomNavigationBar()
    {
        InitializeComponent();
        ApplyLabels();
        ApplyActiveState();
    }

    public string Role
    {
        get => (string)GetValue(RoleProperty);
        set => SetValue(RoleProperty, value);
    }

    public string ActiveItem
    {
        get => (string)GetValue(ActiveItemProperty);
        set => SetValue(ActiveItemProperty, value);
    }

    private static void OnNavigationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is RoleBottomNavigationBar navigation)
        {
            navigation.ApplyLabels();
            navigation.ApplyActiveState();
        }
    }

    private void ApplyLabels()
    {
        var isWorker = IsWorker;
        FirstIcon.Text = "⌂";
        FirstLabel.Text = "Home";
        SecondIcon.Text = isWorker ? "◈" : "▤";
        SecondLabel.Text = isWorker ? "Assigned" : "Requests";
        ThirdIcon.Text = isWorker ? "◷" : "◆";
        ThirdLabel.Text = isWorker ? "Schedule" : "Jobs";
        FourthIcon.Text = "◔";
        FourthLabel.Text = "Alerts";
        FifthIcon.Text = "◎";
        FifthLabel.Text = "Profile";
    }

    private void ApplyActiveState()
    {
        ApplyItemState(FirstItem, FirstIcon, FirstLabel, "Home");
        ApplyItemState(SecondItem, SecondIcon, SecondLabel, IsWorker ? "Assigned" : "Requests");
        ApplyItemState(ThirdItem, ThirdIcon, ThirdLabel, IsWorker ? "Schedule" : "Jobs");
        ApplyItemState(FourthItem, FourthIcon, FourthLabel, "Notifications");
        ApplyItemState(FifthItem, FifthIcon, FifthLabel, "Profile");
    }

    private void ApplyItemState(VisualElement container, Label icon, Label label, string item)
    {
        var active = string.Equals(ActiveItem, item, StringComparison.OrdinalIgnoreCase);
        var activeColor = ResolveThemeColor("ColorAccentLight", "ColorAccentDark", "#F38808", "#F7A642");
        var inactiveColor = ResolveThemeColor("ColorTextMutedLight", "ColorTextMutedDark", "#64748B", "#9FB1C8");

        icon.TextColor = active ? activeColor : inactiveColor;
        label.TextColor = active ? activeColor : inactiveColor;
        container.Scale = active ? 1.04 : 1;
        container.Opacity = active ? 1 : 0.78;
    }

    private async void OnFirstTapped(object sender, EventArgs e)
    {
        await NavigateAsync(IsWorker ? "//WorkerHome" : "//CustomerHome", FirstItem);
    }

    private async void OnSecondTapped(object sender, EventArgs e)
    {
        await NavigateAsync(IsWorker ? "//WorkerJobs" : "//CustomerRequests", SecondItem);
    }

    private async void OnThirdTapped(object sender, EventArgs e)
    {
        await NavigateAsync(IsWorker ? "//WorkerSchedule" : "//CustomerJobs", ThirdItem);
    }

    private async void OnFourthTapped(object sender, EventArgs e)
    {
        await NavigateAsync(IsWorker ? "//WorkerNotifications" : "//CustomerNotifications", FourthItem);
    }

    private async void OnFifthTapped(object sender, EventArgs e)
    {
        await NavigateAsync(IsWorker ? "//WorkerProfile" : "//CustomerProfile", FifthItem);
    }

    private async Task NavigateAsync(string route, VisualElement source)
    {
        await source.ScaleTo(0.94, 70, Easing.CubicOut);
        await source.ScaleTo(1.04, 120, Easing.CubicIn);
        await Shell.Current.GoToAsync(route);
    }

    private bool IsWorker => string.Equals(Role, "Worker", StringComparison.OrdinalIgnoreCase);

    private static Color ResolveThemeColor(string lightKey, string darkKey, string lightFallback, string darkFallback)
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var key = isDark ? darkKey : lightKey;
        var fallback = isDark ? darkFallback : lightFallback;

        return Application.Current?.Resources.TryGetValue(key, out var resource) == true && resource is Color color
            ? color
            : Color.FromArgb(fallback);
    }
}
