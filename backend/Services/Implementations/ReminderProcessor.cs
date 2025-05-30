using backend.Data;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations;
public class ReminderProcessor : IReminderProcessor
{
    private readonly BulldogDbContext _context;
    private readonly ILogger<ReminderProcessor> _logger;
    private readonly INotificationService _notificationService;

    public ReminderProcessor(BulldogDbContext context, ILogger<ReminderProcessor> logger, INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var dueReminders = await _context.Reminders
            .Include(r => r.ActionItem)
            .Where(r => !r.IsSent && r.ReminderTime <= now)
            .ToListAsync(cancellationToken);

        foreach (var reminder in dueReminders)
        {
            _logger.LogInformation("⏰ Reminder triggered! Message: {Message}, ActionItem: {ActionItemText}, User: {UserId}",
                reminder.Message,
                reminder.ActionItem?.Text,
                reminder.UserId);
            try
            {
                // 🔔 Send fake notification
                await _notificationService.SendReminderAsync(
                    reminder.UserId,
                    "You have a reminder",
                    reminder.Message);

                reminder.IsSent = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send reminder notification to user {UserId}", reminder.UserId);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ Processed {Count} due reminders at {Time}", dueReminders.Count, DateTime.UtcNow);
    }
}
