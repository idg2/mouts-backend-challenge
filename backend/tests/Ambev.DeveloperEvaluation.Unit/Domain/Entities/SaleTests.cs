using Ambev.DeveloperEvaluation.Domain.Entities;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

// Work item: TASK-015 (FEAT-010), TASK-062 (FEAT-001)
/// <summary>
/// Contains unit tests for the Sale entity class.
/// Tests cover validation and the item synchronization used by sale updates.
/// </summary>
public class SaleTests
{
    /// <summary>
    /// Tests that validation passes when the sale and its items are valid.
    /// </summary>
    [Fact(DisplayName = "Validation should pass for valid sale data")]
    public void Given_ValidSale_When_Validated_Then_ShouldReturnValid()
    {
        // Arrange
        var sale = NewSale(NewItem(Guid.NewGuid()));

        // Act
        var result = sale.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// Tests that an incoming item without id is added to the sale.
    /// </summary>
    [Fact(DisplayName = "SyncItems should add an item without id")]
    public void Given_NewItem_When_Synced_Then_ItemIsAdded()
    {
        // Arrange
        var existing = NewItem(Guid.NewGuid());
        var sale = NewSale(existing);
        var incomingExisting = NewItem(existing.Id);
        var added = NewItem(Guid.Empty);

        // Act
        sale.SyncItems([incomingExisting, added]);

        // Assert
        Assert.Equal(2, sale.Items.Count);
        Assert.Contains(added, sale.Items);
    }

    /// <summary>
    /// Tests that two incoming items without id are both added; new items all carry
    /// <see cref="Guid.Empty"/> and must not be matched to each other.
    /// </summary>
    [Fact(DisplayName = "SyncItems should add every item without id")]
    public void Given_TwoNewItems_When_Synced_Then_BothAreAdded()
    {
        // Arrange
        var existing = NewItem(Guid.NewGuid());
        var sale = NewSale(existing);
        var first = NewItem(Guid.Empty);
        var second = NewItem(Guid.Empty);

        // Act
        sale.SyncItems([NewItem(existing.Id), first, second]);

        // Assert
        Assert.Equal(3, sale.Items.Count);
        Assert.Contains(first, sale.Items);
        Assert.Contains(second, sale.Items);
    }

    // Work item: TASK-062 (FEAT-001)
    /// <summary>
    /// Tests that an active incoming item with an existing id updates the copied values of that item in place and
    /// leaves its discount snapshot and totals for ApplyDiscounts, which reprices every active item.
    /// </summary>
    [Fact(DisplayName = "SyncItems should update the existing item with the same id and keep its discount snapshot")]
    public void Given_ExistingItemId_When_Synced_Then_ExistingItemIsUpdated()
    {
        // Arrange
        var existing = NewItem(Guid.NewGuid());
        var sale = NewSale(existing);
        var incoming = NewItem(existing.Id);
        incoming.Quantity = 7;
        incoming.RequestedDiscountPercentage = 5m;
        incoming.DiscountAmount = 99m;
        incoming.TotalAmount = 99m;

        // Act
        sale.SyncItems([incoming]);

        // Assert
        var item = Assert.Single(sale.Items);
        Assert.Same(existing, item);
        Assert.Equal(7, item.Quantity);
        Assert.Equal(5m, item.RequestedDiscountPercentage);
        Assert.Equal(5m, item.DiscountAmount);
        Assert.Equal(45m, item.TotalAmount);
        Assert.False(item.IsCancelled);
    }

    // Work item: TASK-062 (FEAT-001)
    /// <summary>
    /// Tests that a cancelled incoming line copies only the cancelled flag, so a priced item keeps the product,
    /// price, quantity, requested discount, and totals it was priced with.
    /// </summary>
    [Fact(DisplayName = "SyncItems should copy only the cancelled flag from a cancelled line")]
    public void Given_CancelledLine_When_Synced_Then_OnlyTheFlagIsCopied()
    {
        // Arrange
        var existing = NewItem(Guid.NewGuid());
        var productId = existing.ProductId;
        var sale = NewSale(existing);
        var incoming = NewItem(existing.Id);
        incoming.ProductDescription = "Soda 2L";
        incoming.UnitPrice = 20m;
        incoming.Quantity = 9;
        incoming.RequestedDiscountPercentage = 3m;
        incoming.DiscountAmount = 99m;
        incoming.TotalAmount = 99m;
        incoming.IsCancelled = true;

        // Act
        sale.SyncItems([incoming]);

        // Assert
        var item = Assert.Single(sale.Items);
        Assert.Same(existing, item);
        Assert.True(item.IsCancelled);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Beer 350ml", item.ProductDescription);
        Assert.Equal(10m, item.UnitPrice);
        Assert.Equal(5, item.Quantity);
        Assert.Null(item.RequestedDiscountPercentage);
        Assert.Equal(10m, item.DiscountPercentage);
        Assert.Equal(5m, item.DiscountAmount);
        Assert.Equal(45m, item.TotalAmount);
    }

    // Work item: TASK-062 (FEAT-001)
    /// <summary>
    /// Tests that an active incoming line reactivates a cancelled item and copies every value, leaving the discount
    /// snapshot and totals for ApplyDiscounts.
    /// </summary>
    [Fact(DisplayName = "SyncItems should copy every value from an active line onto a cancelled item")]
    public void Given_ActiveLineOntoCancelledItem_When_Synced_Then_EverythingIsCopied()
    {
        // Arrange
        var existing = NewItem(Guid.NewGuid());
        existing.IsCancelled = true;
        var sale = NewSale(existing);
        var incoming = NewItem(existing.Id);
        incoming.ProductDescription = "Soda 2L";
        incoming.UnitPrice = 20m;
        incoming.Quantity = 9;
        incoming.RequestedDiscountPercentage = 3m;

        // Act
        sale.SyncItems([incoming]);

        // Assert
        var item = Assert.Single(sale.Items);
        Assert.Same(existing, item);
        Assert.False(item.IsCancelled);
        Assert.Equal(incoming.ProductId, item.ProductId);
        Assert.Equal("Soda 2L", item.ProductDescription);
        Assert.Equal(20m, item.UnitPrice);
        Assert.Equal(9, item.Quantity);
        Assert.Equal(3m, item.RequestedDiscountPercentage);
    }

    /// <summary>
    /// Tests that an existing item missing from the incoming items is removed.
    /// </summary>
    [Fact(DisplayName = "SyncItems should remove an item that is not sent")]
    public void Given_MissingItem_When_Synced_Then_ItemIsRemoved()
    {
        // Arrange
        var kept = NewItem(Guid.NewGuid());
        var removed = NewItem(Guid.NewGuid());
        var sale = NewSale(kept, removed);

        // Act
        sale.SyncItems([NewItem(kept.Id)]);

        // Assert
        var item = Assert.Single(sale.Items);
        Assert.Same(kept, item);
    }

    // Work item: TD-010 (FEAT-010)
    /// <summary>
    /// Tests that after a sync every line is numbered by its position in the incoming list, so kept, moved,
    /// and new items follow the order the client sent.
    /// </summary>
    [Fact(DisplayName = "SyncItems should number the lines in the incoming order")]
    public void Given_ReorderedAndNewItems_When_Synced_Then_LinesFollowIncomingOrder()
    {
        // Arrange
        var first = NewItem(Guid.NewGuid());
        first.LineNumber = 1;
        var second = NewItem(Guid.NewGuid());
        second.LineNumber = 2;
        var sale = NewSale(first, second);
        var added = NewItem(Guid.Empty);

        // Act
        sale.SyncItems([added, NewItem(second.Id), NewItem(first.Id)]);

        // Assert
        Assert.Equal(1, added.LineNumber);
        Assert.Equal(2, second.LineNumber);
        Assert.Equal(3, first.LineNumber);
    }

    // Work item: TD-010 (FEAT-010)
    /// <summary>
    /// Tests that an item without a positive line number is rejected.
    /// </summary>
    [Fact(DisplayName = "Validation should fail for an item without a line number")]
    public void Given_ItemWithoutLineNumber_When_Validated_Then_ShouldReturnInvalid()
    {
        // Arrange
        var item = NewItem(Guid.NewGuid());
        item.LineNumber = 0;
        var sale = NewSale(item);

        // Act
        var result = sale.Validate();

        // Assert
        Assert.False(result.IsValid);
    }

    // Work item: BUG-009 (FEAT-010), TASK-062 (FEAT-001)
    /// <summary>
    /// Tests that sale and item amounts that do not fit numeric(18,2), and percentages that do not fit
    /// numeric(5,2), are rejected.
    /// </summary>
    [Theory(DisplayName = "Validation should fail for an amount beyond the stored precision")]
    [InlineData("TotalAmount", "10.123")]
    [InlineData("Items[0].UnitPrice", "0.001")]
    [InlineData("Items[0].DiscountAmount", "5.555")]
    [InlineData("Items[0].TotalAmount", "12345678901234567")]
    [InlineData("Items[0].DiscountPercentage", "12.345")]
    [InlineData("Items[0].RequestedDiscountPercentage", "12.345")]
    [InlineData("Items[0].DiscountCeilingPercentage", "12.345")]
    public void Given_AmountBeyondPrecision_When_Validated_Then_ShouldReturnInvalid(string property, string value)
    {
        // Arrange
        var item = NewItem(Guid.NewGuid());
        var sale = NewSale(item);
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        switch (property)
        {
            case "TotalAmount": sale.TotalAmount = amount; break;
            case "Items[0].UnitPrice": item.UnitPrice = amount; break;
            case "Items[0].DiscountAmount": item.DiscountAmount = amount; break;
            case "Items[0].TotalAmount": item.TotalAmount = amount; break;
            case "Items[0].DiscountPercentage": item.DiscountPercentage = amount; break;
            case "Items[0].RequestedDiscountPercentage": item.RequestedDiscountPercentage = amount; break;
            case "Items[0].DiscountCeilingPercentage": item.DiscountCeilingPercentage = amount; break;
        }

        // Act
        var result = sale.Validate();

        // Assert
        Assert.False(result.IsValid);
    }

    // Work item: TASK-062 (FEAT-001)
    /// <summary>
    /// Tests that an item without the policy that priced it is rejected.
    /// </summary>
    [Fact(DisplayName = "Validation should fail for an item without a discount policy")]
    public void Given_ItemWithoutPolicy_When_Validated_Then_ShouldReturnInvalid()
    {
        // Arrange
        var item = NewItem(Guid.NewGuid());
        item.DiscountPolicyId = Guid.Empty;
        var sale = NewSale(item);

        // Act
        var result = sale.Validate();

        // Assert
        Assert.False(result.IsValid);
    }

    // Work item: TASK-062 (FEAT-001)
    /// <summary>
    /// Tests that an applied discount above the ceiling is rejected.
    /// </summary>
    [Fact(DisplayName = "Validation should fail for an applied discount above the ceiling")]
    public void Given_AppliedAboveCeiling_When_Validated_Then_ShouldReturnInvalid()
    {
        // Arrange
        var item = NewItem(Guid.NewGuid());
        item.DiscountCeilingPercentage = 5m;
        var sale = NewSale(item);

        // Act
        var result = sale.Validate();

        // Assert
        Assert.False(result.IsValid);
    }

    private static Sale NewSale(params SaleItem[] items) => new()
    {
        Id = Guid.NewGuid(),
        SaleNumber = 1,
        SaleDate = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc),
        CustomerId = Guid.NewGuid(),
        CustomerName = "Acme Market",
        BranchId = Guid.NewGuid(),
        BranchName = "Downtown",
        TotalAmount = 45m,
        Items = items.ToList()
    };

    // Work item: TASK-062 (FEAT-001)
    private static SaleItem NewItem(Guid id) => new()
    {
        Id = id,
        LineNumber = 1,
        ProductId = Guid.NewGuid(),
        ProductDescription = "Beer 350ml",
        UnitPrice = 10m,
        Quantity = 5,
        DiscountPolicyId = Guid.NewGuid(),
        DiscountCeilingPercentage = 10m,
        DiscountPercentage = 10m,
        DiscountAmount = 5m,
        TotalAmount = 45m
    };
}
