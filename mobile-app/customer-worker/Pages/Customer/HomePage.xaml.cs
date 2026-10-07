namespace GTEK.FSM.MobileApp.Pages.Customer;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private async void OnViewRequestsClicked(object sender, EventArgs e)
    {
        await AnimateTappedElementAsync(sender);
        await Shell.Current.GoToAsync("//CustomerRequests");
    }

    private async void OnEditProfileClicked(object sender, EventArgs e)
    {
        await AnimateTappedElementAsync(sender);
        await Shell.Current.GoToAsync("//CustomerProfile");
    }

    private async void OnOpenSettingsClicked(object sender, EventArgs e)
    {
        await AnimateTappedElementAsync(sender);
        await Shell.Current.GoToAsync("//CustomerSettings");
    }

    private async void OnTrackActiveJobClicked(object sender, EventArgs e)
    {
        await AnimateTappedElementAsync(sender);
        await Shell.Current.GoToAsync("//CustomerJobs");
    }

    private async void OnNotificationsClicked(object sender, EventArgs e)
    {
        await AnimateTappedElementAsync(sender);
        await DisplayAlertAsync("Notifications", "Notification center pathway placeholder opened.", "OK");
    }

    private async void OnApproveQuoteClicked(object sender, EventArgs e)
    {
        await AnimateTappedElementAsync(sender);
        await DisplayAlertAsync("Quote", "Approval pathway placeholder: quote approved and queued for dispatch.", "OK");
    }

    private async void OnContactSupportClicked(object sender, EventArgs e)
    {
        await AnimateTappedElementAsync(sender);
        await DisplayAlertAsync("Support", "Support contact pathway placeholder opened.", "OK");
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
