using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.CreateDiscountPolicy;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Handler for processing CreateDiscountPolicyCommand requests. Every rejection is a ValidationException, so the API
/// answers 400; the guards of <see cref="DiscountPolicy.Create"/> only back it up.
/// </summary>
public class CreateDiscountPolicyHandler : IRequestHandler<CreateDiscountPolicyCommand, DiscountPolicyResult>
{
    /// <summary>
    /// The error code of a ValidFrom before the current time (D5).
    /// </summary>
    public const string ValidFromInPast = "ValidFromInPast";

    private readonly IDiscountPolicyRepository _repository;
    private readonly IMapper _mapper;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of CreateDiscountPolicyHandler.
    /// </summary>
    /// <param name="repository">The discount policy repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="timeProvider">The source of the current time and of CreatedAt</param>
    public CreateDiscountPolicyHandler(IDiscountPolicyRepository repository, IMapper mapper, TimeProvider timeProvider)
    {
        _repository = repository;
        _mapper = mapper;
        _timeProvider = timeProvider;
    }

    // Work item: TASK-063 (FEAT-001), TASK-065 (FEAT-001)
    /// <summary>
    /// Handles the CreateDiscountPolicyCommand request.
    /// </summary>
    /// <param name="command">The CreateDiscountPolicy command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created policy with its tiers</returns>
    public async Task<DiscountPolicyResult> Handle(CreateDiscountPolicyCommand command, CancellationToken cancellationToken)
    {
        var validator = new CreateDiscountPolicyValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        StepTrace.Step("DSC-CRT-02", "CMN-PIP-10", "Validate the command",
            [("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count), ("tiers", command.Tiers?.Count)]);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // PostgreSQL stores microseconds: truncating keeps CreatedAt in the response equal to what a later read returns.
        var clock = _timeProvider.GetUtcNow().UtcDateTime;
        var now = clock.AddTicks(-(clock.Ticks % TimeSpan.TicksPerMicrosecond));
        var inPast = command.ValidFrom < now;
        StepTrace.Step("DSC-CRT-03", "ValidFrom before now?", [("validFrom", command.ValidFrom), ("now", now), ("inPast", inPast)]);
        if (inPast)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.ValidFrom),
                    $"ValidFrom {command.ValidFrom:O} is before the current time {now:O}; a discount policy cannot start in the past.")
                {
                    ErrorCode = ValidFromInPast
                }
            });

        var policy = DiscountPolicy.Create(
            command.ProductId,
            command.BranchId,
            command.ValidFrom,
            command.ValidTo,
            command.MaxQuantityPerProduct,
            (command.Tiers ?? []).Select(tier => new DiscountTier(tier.MinQuantity, tier.MaxQuantity, tier.Percentage)),
            now);
        var created = await _repository.CreateAsync(policy, cancellationToken);
        StepTrace.Step("DSC-CRT-04", "Insert the policy and its tiers",
            [("id", created.Id), ("productId", created.ProductId), ("branchId", created.BranchId), ("validFrom", created.ValidFrom), ("tiers", created.Tiers.Count)]);
        return _mapper.Map<DiscountPolicyResult>(created);
    }
}
