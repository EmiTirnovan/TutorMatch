namespace TutorMatch.Api.Domain.Tutors.Events;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record TutorSavedNotification(Guid TutorId, string Bio, List<string> Subjects)
{
    // In-process event handler delegate for consumer modules (e.g., AI embedding generation)
    public static event Action<TutorSavedNotification>? Handler;

    // Dispatches the notification in-process to registered subscribers
    public static void Publish(TutorSavedNotification notification)
    {
        Handler?.Invoke(notification);
    }
}
