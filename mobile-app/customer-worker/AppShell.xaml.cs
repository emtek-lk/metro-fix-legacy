namespace GTEK.FSM.MobileApp;

using CustomerHomePage = GTEK.FSM.MobileApp.Pages.Customer.HomePage;
using CustomerRequestsPage = GTEK.FSM.MobileApp.Pages.Customer.RequestsPage;
using CustomerJobsPage = GTEK.FSM.MobileApp.Pages.Customer.JobsPage;
using CustomerProfilePage = GTEK.FSM.MobileApp.Pages.Customer.ProfilePage;
using CustomerSettingsPage = GTEK.FSM.MobileApp.Pages.Customer.SettingsPage;
using GTEK.FSM.MobileApp.Navigation;
using GTEK.FSM.MobileApp.State;
using WorkerHomePage = GTEK.FSM.MobileApp.Pages.Worker.HomePage;
using WorkerRequestsPage = GTEK.FSM.MobileApp.Pages.Worker.RequestsPage;
using WorkerJobsPage = GTEK.FSM.MobileApp.Pages.Worker.JobsPage;
using WorkerProfilePage = GTEK.FSM.MobileApp.Pages.Worker.ProfilePage;
using WorkerSettingsPage = GTEK.FSM.MobileApp.Pages.Worker.SettingsPage;

public partial class AppShell : Shell
{
    private MobileSectionVisibility availableSections = MobileSectionVisibility.Both;
    private bool isWorkerWorkspace;

    public AppShell()
        : this(new SessionContextState())
    {
    }

    public AppShell(SessionContextState sessionContextState)
    {
        InitializeComponent();

        this.ApplyRoleGate(sessionContextState);
        UpdateThemeToolbarText();
    }

    private void ApplyRoleGate(SessionContextState sessionContextState)
    {
        var role = (sessionContextState.Role ?? string.Empty).Trim();
        this.availableSections = RoleGateResolver.Resolve(role);

        if (this.availableSections == MobileSectionVisibility.WorkerOnly)
        {
            this.RoleSwitchPanel.IsVisible = false;
            this.ApplyWorkspace(isWorker: true, navigateHome: true);
            return;
        }

        if (this.availableSections == MobileSectionVisibility.CustomerOnly)
        {
            this.RoleSwitchPanel.IsVisible = false;
            this.ApplyWorkspace(isWorker: false, navigateHome: true);
            return;
        }

        this.RoleSwitchPanel.IsVisible = true;
        this.ApplyWorkspace(isWorker: false, navigateHome: true);
    }

    private void SetCustomerItemsVisible(bool isVisible)
    {
        CustomerHomeItem.IsVisible = isVisible;
        CustomerRequestsItem.IsVisible = isVisible;
        CustomerJobsItem.IsVisible = isVisible;
        CustomerApprovalsItem.IsVisible = isVisible;
        CustomerNotificationsItem.IsVisible = isVisible;
        CustomerProfileItem.IsVisible = isVisible;
        CustomerSettingsItem.IsVisible = isVisible;
        CustomerSupportItem.IsVisible = isVisible;
    }

    private void SetWorkerItemsVisible(bool isVisible)
    {
        WorkerHomeItem.IsVisible = isVisible;
        WorkerRequestsItem.IsVisible = isVisible;
        WorkerScheduleItem.IsVisible = isVisible;
        WorkerNotificationsItem.IsVisible = isVisible;
        WorkerPerformanceItem.IsVisible = isVisible;
        WorkerJobsItem.IsVisible = isVisible;
        WorkerProfileItem.IsVisible = isVisible;
        WorkerSettingsItem.IsVisible = isVisible;
    }

    private void OnCustomerRoleClicked(object sender, EventArgs e)
    {
        this.ApplyWorkspace(isWorker: false, navigateHome: true);
    }

    private void OnWorkerRoleClicked(object sender, EventArgs e)
    {
        this.ApplyWorkspace(isWorker: true, navigateHome: true);
    }

    private void ApplyWorkspace(bool isWorker, bool navigateHome)
    {
        this.isWorkerWorkspace = isWorker;
        this.SetCustomerItemsVisible(!isWorker);
        this.SetWorkerItemsVisible(isWorker);
        this.UpdateRoleSwitchVisualState();

        if (navigateHome)
        {
            CurrentItem = isWorker ? WorkerHomeItem : CustomerHomeItem;
        }
    }

    private void UpdateRoleSwitchVisualState()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var activeBackground = isDark ? Color.FromArgb("#F7A642") : Color.FromArgb("#F38808");
        var inactiveBackground = isDark ? Color.FromArgb("#182539") : Color.FromArgb("#F0F4F9");
        var activeText = isDark ? Color.FromArgb("#090F1A") : Colors.White;
        var inactiveText = isDark ? Color.FromArgb("#CAD6E5") : Color.FromArgb("#334155");

        CustomerRoleButton.BackgroundColor = this.isWorkerWorkspace ? inactiveBackground : activeBackground;
        CustomerRoleButton.TextColor = this.isWorkerWorkspace ? inactiveText : activeText;
        CustomerRoleButton.Scale = this.isWorkerWorkspace ? 1 : 1.02;

        WorkerRoleButton.BackgroundColor = this.isWorkerWorkspace ? activeBackground : inactiveBackground;
        WorkerRoleButton.TextColor = this.isWorkerWorkspace ? activeText : inactiveText;
        WorkerRoleButton.Scale = this.isWorkerWorkspace ? 1.02 : 1;
        RoleBadgeLabel.Text = this.isWorkerWorkspace ? "WORKER" : "CUSTOMER";
    }

    private void OnToggleThemeClicked(object sender, EventArgs e)
    {
        var currentTheme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;
        Application.Current!.UserAppTheme = currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        UpdateThemeToolbarText();
        this.UpdateRoleSwitchVisualState();
    }

    private void UpdateThemeToolbarText()
    {
        var currentTheme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;
        ThemeToolbarItem.Text = currentTheme == AppTheme.Dark ? "☀" : "☾";
    }
}
