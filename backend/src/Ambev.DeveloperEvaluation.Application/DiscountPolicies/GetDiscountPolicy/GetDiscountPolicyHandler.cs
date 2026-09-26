using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.GetDiscountPolicy;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Handler for processing GetDiscountPolicyCommand requests.
/// </summary>
public class GetDiscountPolicyHandler : IRequestHandler<GetDiscountPolicyCommand, DiscountPolicyResult>
{
    private readonly IDiscountPolicyRepository _repository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of GetDiscountPolicyHandler.
    /// </summary>
    /// <param name="repository">The discount policy repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public GetDiscountPolicyHandler(IDiscountPolicyRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the GetDiscountPolicyCommand request.
    /// </summary>
    /// <param name="request">The GetDiscountPolicy command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The policy with its tiers</returns>
    /// <exception cref="KeyNotFoundException">No policy has the id</exception>
    public async Task<DiscountPolicyResult> Handle(GetDiscountPolicyCommand request, CancellationToken cancellationToken)
    {
        StepTrace.Step("CMN-PIP-10", "Handler validates the command and runs the use case", [("request", nameof(GetDiscountPolicyCommand)), ("id", request.Id)]);
        var validator = new GetDiscountPolicyValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        StepTrace.Step("DSC-GET-02", "Load the policy with its tiers", [("id", request.Id)]);
        var policy = await _repository.GetByIdAsync(request.Id, cancellationToken);
        StepTrace.Step("DSC-GET-03", "Policy found?", [("id", request.Id), ("found", policy != null), ("tiers", policy?.Tiers.Count)]);
        if (policy == null)
            throw new KeyNotFoundException($"Discount policy with ID {request.Id} not found");

        return _mapper.Map<DiscountPolicyResult>(policy);
    }
}
