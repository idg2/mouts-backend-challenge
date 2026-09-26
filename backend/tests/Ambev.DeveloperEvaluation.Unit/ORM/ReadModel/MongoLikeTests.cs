using System.Linq.Expressions;
using Ambev.DeveloperEvaluation.ORM.ReadModel;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.ORM.ReadModel;

// Work item: TASK-074 (FEAT-003)
/// <summary>
/// Contains unit tests for the <see cref="MongoLike"/> class.
/// </summary>
public class MongoLikeTests
{
    /// <summary>
    /// Tests the translation of the ILIKE pattern the list parser produces into an anchored regular expression.
    /// </summary>
    [Theory(DisplayName = "Given an ILIKE pattern When translating Then the regular expression keeps its meaning")]
    [InlineData("Jo%", "^Jo.*$")]
    [InlineData("%son", "^.*son$")]
    [InlineData("%o%", "^.*o.*$")]
    [InlineData("%", "^.*$")]
    [InlineData("Jo", "^Jo$")]
    [InlineData("a\\_b", "^a_b$")]
    [InlineData("100\\%", "^100%$")]
    [InlineData("a\\\\b", "^a\\\\b$")]
    [InlineData("a.b(c)+", "^a\\.b\\(c\\)\\+$")]
    public void Given_LikePattern_When_Translating_Then_KeepsMeaning(string likePattern, string expected)
    {
        // Act
        var pattern = MongoLike.Pattern(likePattern);

        // Assert
        pattern.Should().Be(expected);
    }

    /// <summary>
    /// Tests that the expression compiles to a case-insensitive match of the property.
    /// </summary>
    [Theory(DisplayName = "Given a translated expression When evaluating Then matches case-insensitively")]
    [InlineData("jo%", "John", true)]
    [InlineData("jo%", "Mary", false)]
    [InlineData("%market", "Acme MARKET", true)]
    [InlineData("a.b", "axb", false)]
    [InlineData("a.b", "A.B", true)]
    public void Given_TranslatedExpression_When_Evaluating_Then_MatchesCaseInsensitively(string likePattern, string value, bool expected)
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(SaleDocument), "d");
        var property = Expression.Property(parameter, nameof(SaleDocument.CustomerName));
        var predicate = Expression.Lambda<Func<SaleDocument, bool>>(MongoLike.Translate(property, likePattern), parameter).Compile();

        // Act
        var matches = predicate(new SaleDocument { CustomerName = value });

        // Assert
        matches.Should().Be(expected);
    }
}
