using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.DisableDiscountPolicies;

// Work item: TD-032
/// <summary>
/// Handler for processing DisableDiscountPoliciesCommand requests. All or nothing: an unknown id answers 404 before
/// anything is written. A policy already disabled keeps its first DisabledAt.
/// </summary>
public class DisableDiscountPoliciesHandler : IRequestHandler<DisableDiscountPoliciesCommand, DisableDiscountPoliciesResult>
{
    private readonly IDiscountPolicyRepository _repository;
    private readonly IMapper _mapper;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of DisableDiscountPoliciesHandler.
    /// </summary>
    /// <param name="repository">The discount policy repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="timeProvider">The source of DisabledAt</param>
    public DisableDiscountPoliciesHandler(IDiscountPolicyRepository repository, IMapper mapper, TimeProvider timeProvider)
    {
        _repository = repository;
        _mapper = mapper;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Handles the DisableDiscountPoliciesCommand request.
    /// </summary>
    /// <param name="command">The DisableDiscountPolicies command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The disabled policies in the order of the requested ids</returns>
    /// <exception cref="KeyNotFoundException">At least one id has no policy; the message lists every such id</exception>
    public async Task<DisableDiscountPoliciesResult> Handle(DisableDiscountPoliciesCommand command, CancellationToken cancellationToken)
    {
        var validator = new DisableDiscountPoliciesValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        StepTrace.Step("DSC-DIS-02", "CMN-PIP-10", "Validate the command",
            [("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count), ("ids", command.Ids?.Count)]);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // The validator rejects a null list.
        var ids = command.Ids!;
        StepTrace.Step("DSC-DIS-03", "Load the policies with their tiers", [("ids", ids.Count)]);
        var policies = await _repository.GetByIdsAsync(ids, cancellationToken);
        var policiesById = policies.ToDictionary(policy => policy.Id);
        var missing = ids.Where(id => !policiesById.ContainsKey(id)).ToList();
        StepTrace.Step("DSC-DIS-04", "Every policy found?", [("ids", ids.Count), ("found", policies.Count), ("missing", missing.Count)]);
        if (missing.Count > 0)
            throw new KeyNotFoundException($"Discount policies not found: {string.Join(", ", missing)}");

        // PostgreSQL stores microseconds: truncating keeps DisabledAt in the response equal to what a later read returns.
        var clock = _timeProvider.GetUtcNow().UtcDateTime;
        var now = clock.AddTicks(-(clock.Ticks % TimeSpan.TicksPerMicrosecond));
        var alreadyDisabled = policies.Count(policy => policy.DisabledAt is not null);
        foreach (var policy in policies)
            policy.Disable(now);

        await _repository.UpdateAsync(policies, cancellationToken);
        StepTrace.Step("DSC-DIS-05", "Disable the policies and save",
            [("disabled", policies.Count - alreadyDisabled), ("alreadyDisabled", alreadyDisabled), ("disabledAt", now)]);

        return new DisableDiscountPoliciesResult
        {
            Policies = ids.Select(id => _mapper.Map<DiscountPolicyResult>(policiesById[id])).ToList()
        };
    }
}
