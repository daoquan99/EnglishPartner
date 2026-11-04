using EnglishLiveTutor.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class DbUpdateExceptionClassifierTests
{
    [Fact]
    public void IsUniqueConstraintViolation_DuplicateKeyMessage_ReturnsTrue()
    {
        var exception = new DbUpdateException("Duplicate key violation on a unique index");

        Assert.True(DbUpdateExceptionClassifier.IsUniqueConstraintViolation(exception));
    }

    [Fact]
    public void IsUniqueConstraintViolation_InMemoryDuplicateKey_ReturnsTrue()
    {
        var exception = new DbUpdateException(
            "Save failed",
            new ArgumentException("An item with the same key has already been added."));

        Assert.True(DbUpdateExceptionClassifier.IsUniqueConstraintViolation(exception));
    }

    [Fact]
    public void IsUniqueConstraintViolation_NonUniqueFailure_ReturnsFalse()
    {
        var exception = new DbUpdateException(
            "The INSERT statement conflicted with the FOREIGN KEY constraint");

        Assert.False(DbUpdateExceptionClassifier.IsUniqueConstraintViolation(exception));
    }
}
