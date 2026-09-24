using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.DeleteCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Handler for processing DeleteCustomerCommand requests.
/// </summary>
public class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand, DeleteCustomerResult>
{
    private readonly ICustomerRepository _customerRepository;

    /// <summary>
    /// Initializes a new instance of DeleteCustomerHandler.
    /// </summary>
    /// <param name="customerRepository">The customer repository</param>
    public DeleteCustomerHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    /// <summary>
    /// Handles the DeleteCustomerCommand request.
    /// </summary>
    /// <param name="request">The DeleteCustomer command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the delete operation</returns>
    public async Task<DeleteCustomerResult> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var validator = new DeleteCustomerValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var success = await _customerRepository.DeleteAsync(request.Id, cancellationToken);
        if (!success)
            throw new KeyNotFoundException($"Customer with ID {request.Id} not found");

        return new DeleteCustomerResult { Success = true };
    }
}
