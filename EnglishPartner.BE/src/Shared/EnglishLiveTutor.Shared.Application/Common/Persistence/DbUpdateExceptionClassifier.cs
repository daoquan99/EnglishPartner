using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Common.Persistence;

public static class DbUpdateExceptionClassifier
{
    public static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        for (Exception? cur = ex; cur != null; cur = cur.InnerException)
        {
            var prop = cur.GetType().GetProperty("Number");
            if (prop != null && prop.PropertyType == typeof(int))
            {
                int num = (int)prop.GetValue(cur)!;
                if (num is 2601 or 2627)
                    return true;
            }

            if (cur is ArgumentException argEx &&
                argEx.Message.Contains("same key has already been added", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string msg = cur.Message;
            if (msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Primary key constraint violation", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("cannot insert duplicate key", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("violation of unique key", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("violation of primary key", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
