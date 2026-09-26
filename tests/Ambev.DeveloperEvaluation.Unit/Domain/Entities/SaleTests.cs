using System.Reflection;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using Ambev.DeveloperEvaluation.Unit.TestData;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

// Work item: TASK-015 (FEAT-010), TASK-062 (FEAT-001), TD-039, TD-042
/// <summary>
/// Contains unit tests for the Sale aggregate: creation, the header changes of an update, the item synchronization,
/// validation, and the encapsulation of Sale and SaleItem.
/// </summary>
public class SaleTests
{
    private static readonly DateTime SaleDate = new(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

    // Work item: TD-039
    /// <summary>
    /// Tests that neither Sale nor SaleItem declares a public setter, so their state changes only through the aggregate.
    /// </summary>
    [Fact(DisplayName = "Sale and SaleItem should declare no public setter")]
    public void Given_SaleAndSaleItem_When_Inspected_Then_NoPublicSetter()
    {
        // Arrange
        var types = new[] { typeof(Sale), typeof(SaleItem) };

        // Act
        var publicSetters = types
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(property => property.SetMethod?.IsPublic == true)
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToList();

        // Assert
        Assert.Empty(publicSetters);
    }

    // Work item: TD-039
    /// <summary>
    /// Tests that Create sets the header, starts active with no total, and adds one item per line numbered from 1.
    /// </summary>
    [Fact(DisplayName = "Create should set the header and number one item per line")]
    public void Given_Lines_When_Created_Then_HeaderIsSetAndItemsAreNumbered()
    {
        // Arrange
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var beer = Line(null, description: "Beer 350ml", unitPrice: 10m, quantity: 4, requested: 5m);
        var soda = Line(null, description: "Soda 2L", unitPrice: 5m, quantity: 2);

        // Act
        var sale = Sale.Create(id, SaleDate, customerId, "Acme Market", branchId, "Downtown", [beer, soda]);

        // Assert
        Assert.Equal(id, sale.Id);
        Assert.Equal(SaleDate, sale.SaleDate);
        Assert.Equal(customerId, sale.CustomerId);
        Assert.Equal("Acme Market", sale.CustomerName);
        Assert.Equal(branchId, sale.BranchId);
        Assert.Equal("Downtown", sale.BranchName);
        Assert.False(sale.IsCancelled);
        Assert.Equal(0m, sale.TotalAmount);
        Assert.Equal(new[] { 1, 2 }, sale.Items.Select(item => item.LineNumber));
        var first = sale.Items[0];
        Assert.Equal(Guid.Empty, first.Id);
        Assert.Equal(beer.ProductId, first.ProductId);
        Assert.Equal("Beer 350ml", first.ProductDescription);
        Assert.Equal(10m, first.UnitPrice);
        Assert.Equal(4, first.Quantity);
        Assert.Equal(5m, first.RequestedDiscountPercentage);
        Assert.False(first.IsCancelled);
        Assert.Equal("Soda 2L", sale.Items[1].ProductDescription);
    }

    // Work item: TD-039
    /// <summary>
    /// Tests that ChangeCustomer replaces the customer id and name and nothing else.
    /// </summary>
    [Fact(DisplayName = "ChangeCustomer should replace only the customer")]
    public void Given_Sale_When_CustomerChanged_Then_OnlyCustomerChanges()
    {
        // Arrange
        var sale = StoredSale(StoredItem());
        var branchId = sale.BranchId;
        var customerId = Guid.NewGuid();

        // Act
        sale.ChangeCustomer(customerId, "Globex");

        // Assert
        Assert.Equal(customerId, sale.CustomerId);
        Assert.Equal("Globex", sale.CustomerName);
        Assert.Equal(branchId, sale.BranchId);
        Assert.Equal("Downtown", sale.BranchName);
    }

    // Work item: TD-039
    /// <summary>
    /// Tests that ChangeBranch replaces the branch id and name and nothing else.
    /// </summary>
    [Fact(DisplayName = "ChangeBranch should replace only the branch")]
    public void Given_Sale_When_BranchChanged_Then_OnlyBranchChanges()
    {
        // Arrange
        var sale = StoredSale(StoredItem());
        var customerId = sale.CustomerId;
        var branchId = Guid.NewGuid();

        // Act
        sale.ChangeBranch(branchId, "Uptown");

        // Assert
        Assert.Equal(branchId, sale.BranchId);
        Assert.Equal("Uptown", sale.BranchName);
        Assert.Equal(customerId, sale.CustomerId);
        Assert.Equal("Acme Market", sale.CustomerName);
    }

    // Work item: TD-039
    /// <summary>
    /// Tests that SetCancelled sets the flag both ways, as the PUT does today (sale state rules are FEAT-002).
    /// </summary>
    [Fact(DisplayName = "SetCancelled should set the flag both ways")]
    public void Given_Sale_When_CancelledAndReopened_Then_FlagFollows()
    {
        // Arrange
        var sale = StoredSale(StoredItem());

        // Act
        sale.SetCancelled(true);
        var afterCancel = sale.IsCancelled;
        sale.SetCancelled(false);

        // Assert
        Assert.True(afterCancel);
        Assert.False(sale.IsCancelled);
    }

    // Work item: TD-039
    /// <summary>
    /// Tests that validation passes when the sale and its items are valid.
    /// </summary>
    [Fact(DisplayName = "Validation should pass for valid sale data")]
    public void Given_ValidSale_When_Validated_Then_ShouldReturnValid()
    {
        // Arrange
        var sale = StoredSale(StoredItem());

        // Act
        var result = sale.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // Work item: TD-042, TD-043
    /// <summary>
    /// Tests that a generated sale, created from its lines and priced by the default policy, passes validation.
    /// </summary>
    [Fact(DisplayName = "Validation should pass for a generated sale priced by the default policy")]
    public void Given_GeneratedSale_When_PricedAndValidated_Then_ShouldReturnValid()
    {
        // Arrange
        var sale = SaleTestData.GenerateValidSale();
        var policies = sale.Items.ToDictionary(item => item.ProductId, _ => DiscountPolicyTestData.Create());

        // Act
        sale.ApplyDiscounts(policies);
        var result = sale.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(sale.Items.Sum(item => item.TotalAmount), sale.TotalAmount);
    }

    // Work item: TD-039
    /// <summary>
    /// Tests that a line without item id is added to the sale.
    /// </summary>
    [Fact(DisplayName = "SyncItems should add a line without item id")]
    public void Given_NewLine_When_Synced_Then_ItemIsAdded()
    {
        // Arrange
        var existing = StoredItem();
        var sale = StoredSale(existing);

        // Act
        sale.SyncItems([SameAs(existing), Line(null, description: "Soda 2L")]);

        // Assert
        Assert.Equal(2, sale.Items.Count);
        Assert.Same(existing, sale.Items[0]);
        Assert.Equal(Guid.Empty, sale.Items[1].Id);
        Assert.Equal("Soda 2L", sale.Items[1].ProductDescription);
    }

    // Work item: TD-039
    /// <summary>
    /// Tests that two lines without item id are both added; they must not be matched to each other.
    /// </summary>
    [Fact(DisplayName = "SyncItems should add every line without item id")]
    public void Given_TwoNewLines_When_Synced_Then_BothAreAdded()
    {
        // Arrange
        var existing = StoredItem();
        var sale = StoredSale(existing);

        // Act
        sale.SyncItems([SameAs(existing), Line(null, description: "Soda 2L"), Line(null, description: "Water 500ml")]);

        // Assert
        Assert.Equal(3, sale.Items.Count);
        Assert.Equal(new[] { "Beer 350ml", "Soda 2L", "Water 500ml" }, sale.Items.Select(item => item.ProductDescription));
    }

    // Work item: TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Tests that an active line with an existing item id updates the copied values of that item in place and leaves
    /// its discount snapshot and totals for ApplyDiscounts, which reprices every active item.
    /// </summary>
    [Fact(DisplayName = "SyncItems should update the existing item with the same id and keep its discount snapshot")]
    public void Given_ExistingItemId_When_Synced_Then_ExistingItemIsUpdated()
    {
        // Arrange
        var existing = StoredItem();
        var sale = StoredSale(existing);

        // Act
        sale.SyncItems([Line(existing.Id, existing.ProductId, quantity: 7, requested: 5m)]);

        // Assert
        var item = Assert.Single(sale.Items);
        Assert.Same(existing, item);
        Assert.Equal(7, item.Quantity);
        Assert.Equal(5m, item.RequestedDiscountPercentage);
        Assert.Equal(5m, item.DiscountAmount);
        Assert.Equal(45m, item.TotalAmount);
        Assert.False(item.IsCancelled);
    }

    // Work item: TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Tests that a cancelled line copies only the cancelled flag, so a priced item keeps the product, price,
    /// quantity, requested discount, and totals it was priced with.
    /// </summary>
    [Fact(DisplayName = "SyncItems should copy only the cancelled flag from a cancelled line")]
    public void Given_CancelledLine_When_Synced_Then_OnlyTheFlagIsCopied()
    {
        // Arrange
        var existing = StoredItem();
        var productId = existing.ProductId;
        var sale = StoredSale(existing);

        // Act
        sale.SyncItems([Line(existing.Id, description: "Soda 2L", unitPrice: 20m, quantity: 9, requested: 3m, isCancelled: true)]);

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

    // Work item: TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Tests that an active line reactivates a cancelled item and copies every value, leaving the discount snapshot
    /// and totals for ApplyDiscounts.
    /// </summary>
    [Fact(DisplayName = "SyncItems should copy every value from an active line onto a cancelled item")]
    public void Given_ActiveLineOntoCancelledItem_When_Synced_Then_EverythingIsCopied()
    {
        // Arrange
        var existing = StoredItem(isCancelled: true);
        var sale = StoredSale(existing);
        var incoming = Line(existing.Id, description: "Soda 2L", unitPrice: 20m, quantity: 9, requested: 3m);

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

    // Work item: TD-039
    /// <summary>
    /// Tests that an existing item missing from the lines is removed.
    /// </summary>
    [Fact(DisplayName = "SyncItems should remove an item that is not sent")]
    public void Given_MissingItem_When_Synced_Then_ItemIsRemoved()
    {
        // Arrange
        var kept = StoredItem();
        var removed = StoredItem(lineNumber: 2);
        var sale = StoredSale(kept, removed);

        // Act
        sale.SyncItems([SameAs(kept)]);

        // Assert
        var item = Assert.Single(sale.Items);
        Assert.Same(kept, item);
    }

    // Work item: TD-010 (FEAT-010), TD-039
    /// <summary>
    /// Tests that after a sync every line is numbered by its position in the incoming list, so kept, moved, and new
    /// items follow the order the client sent.
    /// </summary>
    [Fact(DisplayName = "SyncItems should number the lines in the incoming order")]
    public void Given_ReorderedAndNewLines_When_Synced_Then_LinesFollowIncomingOrder()
    {
        // Arrange
        var first = StoredItem(lineNumber: 1);
        var second = StoredItem(lineNumber: 2);
        var sale = StoredSale(first, second);

        // Act
        sale.SyncItems([Line(null, description: "Soda 2L"), SameAs(second), SameAs(first)]);

        // Assert
        var added = sale.Items.Single(item => item.Id == Guid.Empty);
        Assert.Equal(1, added.LineNumber);
        Assert.Equal(2, second.LineNumber);
        Assert.Equal(3, first.LineNumber);
    }

    // Work item: TD-010 (FEAT-010), TD-039
    /// <summary>
    /// Tests that an item without a positive line number is rejected.
    /// </summary>
    [Fact(DisplayName = "Validation should fail for an item without a line number")]
    public void Given_ItemWithoutLineNumber_When_Validated_Then_ShouldReturnInvalid()
    {
        // Arrange
        var item = Persisted.Set(StoredItem(), nameof(SaleItem.LineNumber), 0);
        var sale = StoredSale(item);

        // Act
        var result = sale.Validate();

        // Assert
        Assert.False(result.IsValid);
    }

    // Work item: BUG-009 (FEAT-010), TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Tests that sale and item amounts that do not fit numeric(18,2), and percentages that do not fit numeric(5,2),
    /// are rejected.
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
        var item = StoredItem();
        var sale = StoredSale(item);
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        const string itemPrefix = "Items[0].";
        if (property.StartsWith(itemPrefix, StringComparison.Ordinal))
            Persisted.Set(item, property[itemPrefix.Length..], amount);
        else
            Persisted.Set(sale, property, amount);

        // Act
        var result = sale.Validate();

        // Assert
        Assert.False(result.IsValid);
    }

    // Work item: TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Tests that an item without the policy that priced it is rejected.
    /// </summary>
    [Fact(DisplayName = "Validation should fail for an item without a discount policy")]
    public void Given_ItemWithoutPolicy_When_Validated_Then_ShouldReturnInvalid()
    {
        // Arrange
        var item = Persisted.Set(StoredItem(), nameof(SaleItem.DiscountPolicyId), Guid.Empty);
        var sale = StoredSale(item);

        // Act
        var result = sale.Validate();

        // Assert
        Assert.False(result.IsValid);
    }

    // Work item: TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Tests that an applied discount above the ceiling is rejected.
    /// </summary>
    [Fact(DisplayName = "Validation should fail for an applied discount above the ceiling")]
    public void Given_AppliedAboveCeiling_When_Validated_Then_ShouldReturnInvalid()
    {
        // Arrange
        var item = Persisted.Set(StoredItem(), nameof(SaleItem.DiscountCeilingPercentage), 5m);
        var sale = StoredSale(item);

        // Act
        var result = sale.Validate();

        // Assert
        Assert.False(result.IsValid);
    }

    // Work item: TD-039
    // A sale as EF loads it: id and number from the database, total from an earlier pricing.
    private static Sale StoredSale(params SaleItem[] items) => Persisted.New<Sale>(new
    {
        Id = Guid.NewGuid(),
        SaleNumber = 1L,
        SaleDate,
        CustomerId = Guid.NewGuid(),
        CustomerName = "Acme Market",
        BranchId = Guid.NewGuid(),
        BranchName = "Downtown",
        TotalAmount = 45m,
        Items = items.ToList()
    });

    // Work item: TD-039
    // A priced item as EF loads it: 5 x 10.00 at 10%.
    private static SaleItem StoredItem(int lineNumber = 1, bool isCancelled = false) => Persisted.New<SaleItem>(new
    {
        Id = Guid.NewGuid(),
        LineNumber = lineNumber,
        ProductId = Guid.NewGuid(),
        ProductDescription = "Beer 350ml",
        UnitPrice = 10m,
        Quantity = 5,
        DiscountPolicyId = Guid.NewGuid(),
        DiscountCeilingPercentage = 10m,
        DiscountPercentage = 10m,
        DiscountAmount = 5m,
        TotalAmount = 45m,
        IsCancelled = isCancelled
    });

    // Work item: TD-039
    private static SaleLine Line(
        Guid? itemId, Guid? productId = null, string description = "Beer 350ml", decimal unitPrice = 10m,
        int quantity = 5, decimal? requested = null, bool isCancelled = false) =>
        new(itemId, productId ?? Guid.NewGuid(), description, unitPrice, quantity, requested, isCancelled);

    // Work item: TD-039
    // The line a client sends to keep an item unchanged.
    private static SaleLine SameAs(SaleItem item) =>
        new(item.Id, item.ProductId, item.ProductDescription, item.UnitPrice, item.Quantity, item.RequestedDiscountPercentage, item.IsCancelled);
}
