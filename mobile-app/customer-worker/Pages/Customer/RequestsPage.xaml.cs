namespace GTEK.FSM.MobileApp.Pages.Customer;

using System.Collections.ObjectModel;
using GTEK.FSM.MobileApp.Services.Api;
using GTEK.FSM.MobileApp.Services.Realtime;
using GTEK.FSM.MobileApp.Workflows;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;
using Microsoft.Extensions.DependencyInjection;

public partial class RequestsPage : ContentPage, IDisposable, IQueryAttributable
{
    private readonly ObservableCollection<CustomerRequestViewModel> _requests;
    private readonly ObservableCollection<RequestCategoryViewModel> _categories;
    private readonly IRequestQueryService _requestQueryService;
    private readonly IRequestDetailQueryService _requestDetailQueryService;
    private readonly ICategoryQueryService _categoryQueryService;
    private readonly IServiceRequestCreationService _requestCreationService;
    private readonly IFeedbackSubmissionService? _feedbackSubmissionService;
    private readonly IMobileOperationalRealtimeClient? _realtimeClient;
    private readonly IDisposable? _statusSubscription;
    private CustomerRequestViewModel _selectedRequest;
    private bool _isSubmitting;
    private string _pendingRequestId;
    private string _selectedRequestActiveJobId = string.Empty;

    public RequestsPage()
    {
        InitializeComponent();

        _requests = new ObservableCollection<CustomerRequestViewModel>
        {
            new CustomerRequestViewModel(
                id: "REQ-2304",
                title: "AC Cooling Issue",
                summary: "Unit is not cooling the second floor.",
                etaText: "Technician ETA: Today 2:30 PM",
                statusLabel: "In Progress",
                statusColor: Color.FromArgb("#F38808"),
                currentStage: 2),
            new CustomerRequestViewModel(
                id: "REQ-2301",
                title: "Water Pressure Drop",
                summary: "Low pressure in kitchen and bathroom lines.",
                etaText: "Awaiting schedule confirmation",
                statusLabel: "Assigned",
                statusColor: Color.FromArgb("#166534"),
                currentStage: 1),
            new CustomerRequestViewModel(
                id: "REQ-2294",
                title: "Generator Inspection",
                summary: "Routine preventive maintenance inspection.",
                etaText: "Completed yesterday",
                statusLabel: "Completed",
                statusColor: Color.FromArgb("#166534"),
                currentStage: 3),
        };

            _categories = new ObservableCollection<RequestCategoryViewModel>();

        RequestsCollectionView.ItemsSource = _requests;
            CategoryPicker.ItemsSource = _categories;
        CustomerFeedbackTypePicker.ItemsSource = CustomerFeedbackTypeOptions;
        CustomerFeedbackTypePicker.ItemDisplayBinding = new Binding(nameof(FeedbackTypeOption.Label));
        CustomerFeedbackTypePicker.SelectedItem = CustomerFeedbackTypeOptions[0];
        CustomerRatingPicker.ItemsSource = FeedbackRatings;
        CustomerRatingPicker.SelectedItem = FeedbackRatings[^1];
        RequestsCollectionView.SelectedItem = _requests[0];
        RenderRequestDetail(_requests[0]);
            CreateRequestFeedbackLabel.Text = string.Empty;

        _requestQueryService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IRequestQueryService>();
        _requestDetailQueryService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IRequestDetailQueryService>();
            _categoryQueryService = Application.Current?.Handler?.MauiContext?.Services?.GetService<ICategoryQueryService>();
            _requestCreationService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IServiceRequestCreationService>();
        _feedbackSubmissionService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IFeedbackSubmissionService>();
        _realtimeClient = Application.Current?.Handler?.MauiContext?.Services?.GetService<IMobileOperationalRealtimeClient>();
        if (_realtimeClient is not null)
        {
            _realtimeClient.ConnectionStateChanged += OnRealtimeConnectionStateChanged;
            _statusSubscription = _realtimeClient.SubscribeToStatusUpdates(HandleStatusUpdateAsync);
            _ = _realtimeClient.EnsureConnectedAsync();
            UpdateRealtimeStatus(_realtimeClient.ConnectionState);
        }
        else
        {
            RequestSyncStatusLabel.Text = "Realtime unavailable";
            RequestSyncPillLabel.Text = "OFF";
        }

        _ = LoadLiveRequestsAsync();
        _ = LoadCategoriesAsync();
    }

    private static int[] FeedbackRatings { get; } = [1, 2, 3, 4, 5];

    private static FeedbackTypeOption[] CustomerFeedbackTypeOptions { get; } =
    [
        new(0, "Service Quality"),
        new(1, "Worker Behavior"),
        new(2, "Response Timeliness"),
        new(3, "Communication"),
        new(4, "Technical Competence"),
        new(5, "Other"),
    ];

    public void Dispose()
    {
        if (_realtimeClient is not null)
        {
            _realtimeClient.ConnectionStateChanged -= OnRealtimeConnectionStateChanged;
        }

        _statusSubscription?.Dispose();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("requestId", out var requestIdValue) && requestIdValue is string requestId)
        {
            _pendingRequestId = Uri.UnescapeDataString(requestId);
            TrySelectPendingRequest();
        }
    }

    private void OnRequestSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is CustomerRequestViewModel request)
        {
            _ = RequestsCollectionView.ScaleTo(0.995, 70);
            _ = RequestsCollectionView.ScaleTo(1, 120);
            RenderRequestDetail(request);
            _ = LoadSelectedRequestDetailAsync(request);
        }
    }

    private async void OnOpenRequestFiltersClicked(object sender, EventArgs e)
    {
        var selected = await DisplayActionSheetAsync(
            "Filter requests",
            "Cancel",
            null,
            "All requests",
            "Open",
            "In progress",
            "Pending approval",
            "Closed");

        if (!string.IsNullOrWhiteSpace(selected) && selected != "Cancel")
        {
            ActiveRequestFilterLabel.Text = selected;
            await ActiveRequestFilterLabel.FadeTo(0.35, 70);
            await ActiveRequestFilterLabel.FadeTo(1, 120);
        }
    }

    private void RenderRequestDetail(CustomerRequestViewModel request)
    {
        _selectedRequest = request;
        SelectedRequestTitleLabel.Text = request.Title;
        SelectedRequestDescriptionLabel.Text = request.Summary;
        SelectedRequestMetaLabel.Text = $"{request.Id} • {request.EtaText}";
        SelectedRequestLifecycleLabel.Text = $"Current lifecycle: {request.StatusLabel}";
        SelectedRequestWorkerLabel.Text = "Assigned worker: checking detail...";
        SelectedRequestJobLabel.Text = "Active job: checking detail...";
        SelectedRequestUpdatedLabel.Text = string.Empty;
        TechnicianTrustLabel.Text = "Assigned technician details will appear after dispatch confirms the visit.";
        _selectedRequestActiveJobId = string.Empty;
        UpdateCustomerFeedbackAvailability(request.StatusLabel);

        var stageLabels = new[] { "New", "Assigned", "In Progress", "Completed" };
        StatusTimelineLayout.Children.Clear();
        RequestTimelineLayout.Children.Clear();

        for (var index = 0; index < stageLabels.Length; index++)
        {
            var isComplete = index <= request.CurrentStage;
            var color = isComplete
                ? ResolveThemeColor("ColorAccentLight", "ColorAccentDark", "#F38808", "#F7A642")
                : ResolveThemeColor("ColorTextMutedLight", "ColorTextMutedDark", "#64748B", "#9FB1C8");

            StatusTimelineLayout.Children.Add(BuildTimelineLabel(
                text: $"{(isComplete ? "Completed" : "Pending")}: {stageLabels[index]}",
                textColor: color,
                emphasized: isComplete));
        }

            RequestTimelineLayout.Children.Add(BuildTimelineLabel(
                "Loading activity timeline...",
                ResolveThemeColor("ColorTextMutedLight", "ColorTextMutedDark", "#64748B", "#9FB1C8"),
                emphasized: false));
    }

    private async void OnEscalateRequestClicked(object sender, EventArgs e)
    {
        if (_selectedRequest is null)
        {
            return;
        }

        await DisplayAlertAsync("Escalation", $"Escalation pathway placeholder triggered for {_selectedRequest.Id}.", "OK");
    }

    private async void OnRefreshEtaClicked(object sender, EventArgs e)
    {
        if (_selectedRequest is null)
        {
            return;
        }

        await DisplayAlertAsync("ETA", $"ETA refresh pathway placeholder triggered for {_selectedRequest.Id}.", "OK");
    }

    private async void OnMessageTechnicianClicked(object sender, EventArgs e)
    {
        if (_selectedRequest is null)
        {
            return;
        }

        await DisplayAlertAsync("Technician", $"Messaging pathway placeholder opened for {_selectedRequest.Id}.", "OK");
    }

    private async void OnSubmitCustomerFeedbackClicked(object sender, EventArgs e)
    {
        if (_selectedRequest is null || _feedbackSubmissionService is null || _isSubmitting)
        {
            return;
        }

        if (!IsCompletedStatus(_selectedRequest.StatusLabel))
        {
            CustomerFeedbackStatusLabel.Text = "Feedback is available after the request is completed.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedRequestActiveJobId))
        {
            CustomerFeedbackStatusLabel.Text = "A completed job link is required before feedback can be submitted.";
            return;
        }

        if (CustomerFeedbackTypePicker.SelectedItem is not FeedbackTypeOption feedbackType
            || CustomerRatingPicker.SelectedItem is not int rating)
        {
            CustomerFeedbackStatusLabel.Text = "Select both a feedback category and rating before submitting.";
            return;
        }

        _isSubmitting = true;
        try
        {
            CustomerFeedbackStatusLabel.Text = "Submitting feedback...";

            var submission = await _feedbackSubmissionService.SubmitRequestFeedbackAsync(
                requestId: _selectedRequest.Id,
                jobId: _selectedRequestActiveJobId,
                rating: rating,
                comment: CustomerFeedbackCommentEditor.Text,
                type: feedbackType.Value);

            CustomerFeedbackStatusLabel.Text = submission.IsSuccess
                ? "Feedback submitted."
                : submission.Message;

            if (submission.IsSuccess)
            {
                CustomerFeedbackCommentEditor.Text = string.Empty;
            }
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task LoadLiveRequestsAsync()
    {
        if (_requestQueryService is null)
        {
            return;
        }

        var result = await _requestQueryService.QueryRequestsAsync();
        if (!result.IsLive || result.Items.Count == 0)
        {
            return;
        }

        _requests.Clear();
        foreach (var item in result.Items)
        {
            var stage = MobileOperationalRealtimeMapper.NormalizeStatus(item.Stage ?? "New");
            var summary = item.Summary ?? "Request details unavailable.";
            var requestId = item.RequestId ?? "REQ-UNKNOWN";
            var title = BuildTitle(summary, requestId);

            _requests.Add(new CustomerRequestViewModel(
                id: requestId,
                title: title,
                summary: summary,
                etaText: $"Updated {item.UpdatedUtc:g}",
                statusLabel: stage,
                statusColor: ResolveStageColor(stage),
                currentStage: ResolveStageIndex(stage)));
        }

        if (_requests.Count > 0)
        {
            RequestsCollectionView.SelectedItem = _requests[0];
            RenderRequestDetail(_requests[0]);
            _ = LoadSelectedRequestDetailAsync(_requests[0]);
            TrySelectPendingRequest();
        }
    }

    private async Task LoadCategoriesAsync()
    {
        if (_categoryQueryService is null)
        {
            return;
        }

        var result = await _categoryQueryService.QueryActiveCategoriesAsync();
        if (!result.IsLive || result.Items.Count == 0)
        {
            CreateRequestFeedbackLabel.Text = "Unable to load categories right now.";
            return;
        }

        _categories.Clear();
        foreach (var item in result.Items.OrderBy(category => category.SortOrder).ThenBy(category => category.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                continue;
            }

            _categories.Add(new RequestCategoryViewModel(
                CategoryId: item.CategoryId ?? string.Empty,
                Code: item.Code ?? string.Empty,
                Name: item.Name,
                SortOrder: item.SortOrder));
        }

        if (_categories.Count > 0)
        {
            CategoryPicker.SelectedItem = _categories[0];
            CreateRequestFeedbackLabel.Text = string.Empty;
        }
    }

    private async void OnSubmitRequestClicked(object sender, EventArgs e)
    {
        if (_isSubmitting)
        {
            return;
        }

        if (_requestCreationService is null)
        {
            CreateRequestFeedbackLabel.Text = "Request submission is unavailable.";
            return;
        }

        var submission = CustomerRequestJourney.PlanSubmission(
            (CategoryPicker.SelectedItem as RequestCategoryViewModel)?.Name,
            RequestDetailsEditor.Text);
        if (!submission.IsValid)
        {
            CreateRequestFeedbackLabel.Text = submission.FeedbackMessage;
            return;
        }

        _isSubmitting = true;
        CreateRequestFeedbackLabel.Text = "Submitting request...";

        try
        {
            var creation = await _requestCreationService.CreateRequestAsync(submission.Title);
            if (!creation.IsSuccess)
            {
                CreateRequestFeedbackLabel.Text = "Request submission failed. Please try again.";
                return;
            }

            RequestDetailsEditor.Text = string.Empty;
            CreateRequestFeedbackLabel.Text = $"Request {creation.Request.RequestId} created.";

            await LoadLiveRequestsAsync();
            EnsureCreatedRequestVisible(creation.Request);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async void OnCreateRequestFabClicked(object sender, EventArgs e)
    {
        await ((VisualElement)sender).ScaleTo(0.92, 70, Easing.CubicOut);
        await ((VisualElement)sender).ScaleTo(1, 120, Easing.CubicIn);
        RequestDetailsEditor.Focus();
        CreateRequestFeedbackLabel.Text = "Ready when you are. Add the issue details and submit.";
    }

    private void EnsureCreatedRequestVisible(CreateServiceRequestResponse createdRequest)
    {
        var existing = _requests.FirstOrDefault(item => string.Equals(item.Id, createdRequest.RequestId, StringComparison.Ordinal));
        if (existing is null)
        {
            var fallback = ToViewModel(CustomerRequestJourney.BuildFallbackCreatedRequest(createdRequest));

            _requests.Insert(0, fallback);
            RequestsCollectionView.SelectedItem = fallback;
            RenderRequestDetail(fallback);
            _ = LoadSelectedRequestDetailAsync(fallback);
            return;
        }

        RequestsCollectionView.SelectedItem = existing;
        RenderRequestDetail(existing);
        _ = LoadSelectedRequestDetailAsync(existing);
    }

    private static string BuildTitle(string summary, string requestId)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return requestId;
        }

        return summary.Length <= 36
            ? summary
            : $"{summary[..33]}...";
    }
    private void TrySelectPendingRequest()
    {
        if (string.IsNullOrWhiteSpace(_pendingRequestId) || _requests.Count == 0)
        {
            return;
        }

        var targetId = CustomerRequestJourney.ResolvePendingRequestId(_pendingRequestId, _requests.Select(ToSnapshot));
        var target = _requests.FirstOrDefault(request => string.Equals(request.Id, targetId, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            return;
        }

        RequestsCollectionView.SelectedItem = target;
        RenderRequestDetail(target);
        _ = LoadSelectedRequestDetailAsync(target);
        _pendingRequestId = string.Empty;
    }

    private async Task LoadSelectedRequestDetailAsync(CustomerRequestViewModel request)
    {
        if (_requestDetailQueryService is null || string.IsNullOrWhiteSpace(request.Id))
        {
            return;
        }

        var detail = await _requestDetailQueryService.GetRequestDetailAsync(request.Id);
        if (!detail.IsSuccess)
        {
            var unavailable = CustomerRequestJourney.BuildUnavailableDetailPresentation(detail.Message);
            SelectedRequestWorkerLabel.Text = unavailable.WorkerText;
            SelectedRequestJobLabel.Text = unavailable.JobText;
            SelectedRequestUpdatedLabel.Text = unavailable.UpdatedText;
            RenderTimeline(unavailable.TimelineLines);
            return;
        }

        if (_selectedRequest is null || !string.Equals(_selectedRequest.Id, request.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var syncedRequest = ToViewModel(CustomerRequestJourney.SyncDetail(ToSnapshot(request), detail.Detail));

        ReplaceRequest(request, syncedRequest);

        var presentation = CustomerRequestJourney.BuildDetailPresentation(detail.Detail);
        _selectedRequestActiveJobId = detail.Detail.ActiveJobId ?? string.Empty;
        SelectedRequestLifecycleLabel.Text = presentation.LifecycleText;
        SelectedRequestWorkerLabel.Text = presentation.WorkerText;
        SelectedRequestJobLabel.Text = presentation.JobText;
        SelectedRequestUpdatedLabel.Text = presentation.UpdatedText;
        TechnicianTrustLabel.Text = string.IsNullOrWhiteSpace(presentation.WorkerText)
            ? "Technician assignment is still pending."
            : $"{presentation.WorkerText}. ETA and arrival confidence will update live.";
        UpdateCustomerFeedbackAvailability(syncedRequest.StatusLabel);

        RenderTimeline(presentation.TimelineLines);
    }

    private async void OnRefreshRequestCenter(object sender, EventArgs e)
    {
        RequestSyncStatusLabel.Text = "Refreshing requests and categories...";
        await LoadLiveRequestsAsync();
        await LoadCategoriesAsync();
        RequestRefreshView.IsRefreshing = false;
        RequestSyncStatusLabel.Text = $"Synced {DateTime.Now:t}";
    }

    private async void OnApproveSelectedQuoteClicked(object sender, EventArgs e)
    {
        if (_selectedRequest is null)
        {
            return;
        }

        await DisplayAlertAsync("Approval", $"Quote approval pathway opened for {_selectedRequest.Id}.", "OK");
    }

    private async void OnRejectSelectedQuoteClicked(object sender, EventArgs e)
    {
        if (_selectedRequest is null)
        {
            return;
        }

        await DisplayAlertAsync("Approval", $"Quote rejection pathway opened for {_selectedRequest.Id}.", "OK");
    }

    private void OnRealtimeConnectionStateChanged(MobileOperationalRealtimeConnectionState state)
    {
        MainThread.BeginInvokeOnMainThread(() => UpdateRealtimeStatus(state));
    }

    private void UpdateRealtimeStatus(MobileOperationalRealtimeConnectionState state)
    {
        RequestSyncStatusLabel.Text = state switch
        {
            MobileOperationalRealtimeConnectionState.Connected => $"Live updates connected • {DateTime.Now:t}",
            MobileOperationalRealtimeConnectionState.Connecting => "Connecting to live updates...",
            MobileOperationalRealtimeConnectionState.Reconnecting => "Reconnecting • actions will refresh shortly",
            MobileOperationalRealtimeConnectionState.AuthenticationRequired => "Session refresh required for live updates",
            MobileOperationalRealtimeConnectionState.Faulted => "Live updates paused • pull to refresh",
            _ => "Offline mode • showing cached request data",
        };

        RequestSyncPillLabel.Text = state == MobileOperationalRealtimeConnectionState.Connected ? "LIVE" : "SYNC";
    }

    private void RenderTimeline(IReadOnlyList<string> timelineLines)
    {
        RequestTimelineLayout.Children.Clear();

        if (timelineLines.Count == 0)
        {
            RequestTimelineLayout.Children.Add(BuildTimelineLabel(
                "No additional activity yet.",
                ResolveThemeColor("ColorTextMutedLight", "ColorTextMutedDark", "#64748B", "#9FB1C8"),
                emphasized: false));
            return;
        }

        foreach (var line in timelineLines)
        {
            RequestTimelineLayout.Children.Add(BuildTimelineLabel(
                line,
                ResolveThemeColor("ColorTextSecondaryLight", "ColorTextSecondaryDark", "#334155", "#CAD6E5"),
                emphasized: false));
        }
    }

    private static Label BuildTimelineLabel(string text, Color textColor, bool emphasized)
    {
        return new Label
        {
            Text = text,
            TextColor = textColor,
            FontAttributes = emphasized ? FontAttributes.Bold : FontAttributes.None,
            FontSize = 13,
            LineBreakMode = LineBreakMode.WordWrap,
        };
    }

    private static Color ResolveThemeColor(string lightKey, string darkKey, string lightFallback, string darkFallback)
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var key = isDark ? darkKey : lightKey;
        var fallback = isDark ? darkFallback : lightFallback;

        return Application.Current?.Resources.TryGetValue(key, out var resource) == true && resource is Color color
            ? color
            : Color.FromArgb(fallback);
    }

    private static int ResolveStageIndex(string stage)
    {
        return MobileOperationalRealtimeMapper.ResolveRequestStageIndex(stage);
    }

    private static Color ResolveStageColor(string stage)
    {
        return MobileOperationalRealtimeMapper.ResolveRequestStageColor(stage);
    }

    private void UpdateCustomerFeedbackAvailability(string? statusLabel)
    {
        var isCompleted = IsCompletedStatus(statusLabel);
        var canSubmit = isCompleted && !string.IsNullOrWhiteSpace(_selectedRequestActiveJobId);

        SubmitCustomerFeedbackButton.IsEnabled = canSubmit;
        CustomerFeedbackTypePicker.IsEnabled = canSubmit;
        CustomerRatingPicker.IsEnabled = canSubmit;
        CustomerFeedbackCommentEditor.IsEnabled = canSubmit;
        CustomerFeedbackHintLabel.Text = canSubmit
            ? "Submit customer feedback for this completed request so management can review service quality trends."
            : isCompleted
                ? "Feedback unlocks once the completed request is linked to a job record."
                : "Feedback becomes available after the request reaches Completed.";
        CustomerFeedbackStatusLabel.Text ??= string.Empty;
    }

    private static bool IsCompletedStatus(string? status)
    {
        return string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase);
    }

    private Task HandleStatusUpdateAsync(ServiceRequestStatusUpdatedEvent payload)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var target = _requests.FirstOrDefault(request => string.Equals(request.Id, payload.RequestId, StringComparison.Ordinal));
            if (target is null)
            {
                return;
            }

            var updated = ToViewModel(CustomerRequestJourney.ApplyStatusUpdate(ToSnapshot(target), payload));

            ReplaceRequest(target, updated);

            if (_selectedRequest is not null && string.Equals(_selectedRequest.Id, updated.Id, StringComparison.OrdinalIgnoreCase))
            {
                _ = LoadSelectedRequestDetailAsync(updated);
            }
        });

        return Task.CompletedTask;
    }

    private void ReplaceRequest(CustomerRequestViewModel previous, CustomerRequestViewModel updated)
    {
        var index = _requests.IndexOf(previous);
        if (index < 0)
        {
            return;
        }

        _requests[index] = updated;
        if (_selectedRequest is not null && string.Equals(_selectedRequest.Id, updated.Id, StringComparison.Ordinal))
        {
            RenderRequestDetail(updated);
        }
    }

    private static CustomerRequestSnapshot ToSnapshot(CustomerRequestViewModel viewModel)
    {
        return new CustomerRequestSnapshot(
            Id: viewModel.Id,
            Title: viewModel.Title,
            Summary: viewModel.Summary,
            EtaText: viewModel.EtaText,
            StatusLabel: viewModel.StatusLabel,
            CurrentStage: viewModel.CurrentStage);
    }

    private static CustomerRequestViewModel ToViewModel(CustomerRequestSnapshot snapshot)
    {
        return new CustomerRequestViewModel(
            id: snapshot.Id,
            title: snapshot.Title,
            summary: snapshot.Summary,
            etaText: snapshot.EtaText,
            statusLabel: snapshot.StatusLabel,
            statusColor: ResolveStageColor(snapshot.StatusLabel),
            currentStage: snapshot.CurrentStage);
    }
}

internal sealed record RequestCategoryViewModel(
    string CategoryId,
    string Code,
    string Name,
    int SortOrder);

internal sealed record CustomerRequestViewModel
{
    public CustomerRequestViewModel(
        string id,
        string title,
        string summary,
        string etaText,
        string statusLabel,
        Color statusColor,
        int currentStage)
    {
        Id = id;
        Title = title;
        Summary = summary;
        EtaText = etaText;
        StatusLabel = statusLabel;
        StatusColor = statusColor;
        CurrentStage = currentStage;
    }

    public string Id { get; }

    public string Title { get; }

    public string Summary { get; }

    public string EtaText { get; init; }

    public string StatusLabel { get; init; }

    public Color StatusColor { get; init; }

    public int CurrentStage { get; init; }
}

internal sealed record FeedbackTypeOption(int Value, string Label);
