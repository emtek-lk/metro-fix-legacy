namespace GTEK.FSM.MobileApp.Pages.Worker;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private async void OnOpenJobsWorkspaceClicked(object sender, EventArgs e)
    {
        await AnimateTappedElementAsync(sender);
        await Shell.Current.GoToAsync("//WorkerJobs");
    }

    private async void OnOpenRequestsClicked(object sender, EventArgs e)
    {
        await AnimateTappedElementAsync(sender);
        await Shell.Current.GoToAsync("//WorkerRequests");
    }

    private async void OnOpenProfileClicked(object sender, EventArgs e)
    {
        await AnimateTappedElementAsync(sender);
        await Shell.Current.GoToAsync("//WorkerProfile");
    }

    private async void OnOpenSettingsClicked(object sender, EventArgs e)
    {
        await AnimateTappedElementAsync(sender);
        await Shell.Current.GoToAsync("//WorkerSettings");
    }

    private async void OnAcceptTopJobClicked(object sender, EventArgs e)
    {
        await AnimateTappedElementAsync(sender);
        await Shell.Current.GoToAsync("//WorkerJobs");
        await DisplayAlertAsync("Dispatch", "Top available job pathway opened.", "OK");
    }

    private async void OnUpdateStatusClicked(object sender, EventArgs e)
    {
        await AnimateTappedElementAsync(sender);
        await Shell.Current.GoToAsync("//WorkerJobs");
        await DisplayAlertAsync("Status", "Status update pathway opened in jobs workspace.", "OK");
    }

    private async void OnCallDispatchClicked(object sender, EventArgs e)
    {
        await AnimateTappedElementAsync(sender);
        await DisplayAlertAsync("Dispatch", "Dispatch contact pathway placeholder opened.", "OK");
    }

    private static async Task AnimateTappedElementAsync(object sender)
    {
        if (sender is VisualElement element)
        {
            await element.ScaleTo(0.97, 70, Easing.CubicOut);
            await element.ScaleTo(1, 120, Easing.CubicIn);
        }
    }
}
