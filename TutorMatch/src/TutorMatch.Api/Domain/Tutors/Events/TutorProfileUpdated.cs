namespace TutorMatch.Api.Domain.Tutors.Events;

// Notification emitted when a tutor profile is updated, triggering AI embedding synchronization
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record TutorProfileUpdated(Guid TutorId, string Bio, List<string> Subjects)
{
    // In-process event handler delegate for consumer modules
    public static event Action<TutorProfileUpdated>? Handler;

    // Dispatches the notification in-process to registered subscribers
    public static void Publish(TutorProfileUpdated notification)
    {
        Handler?.Invoke(notification);
        // Synchronize with existing AI embedding infrastructure
        TutorSavedNotification.Publish(new TutorSavedNotification(notification.TutorId, notification.Bio, notification.Subjects));
    }
}
