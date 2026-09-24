using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Validation;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Handler for processing UpdateCustomerCommand requests.
/// </summary>
public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, UpdateCustomerResult>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of UpdateCustomerHandler.
    /// </summary>
    /// <param name="customerRepository">The customer repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public UpdateCustomerHandler(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    // Work item: TASK-018 (FEAT-010), FEAT-012
    /// <summary>
    /// Handles the UpdateCustomerCommand request. The document is stored without mask and in upper case, and must
    /// not belong to another customer.
    /// </summary>
    /// <param name="command">The UpdateCustomer command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated customer</returns>
    public async Task<UpdateCustomerResult> Handle(UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        var validator = new UpdateCustomerValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var customer = await _customerRepository.GetByIdAsync(command.Id, cancellationToken);
        if (customer == null)
            throw new KeyNotFoundException($"Customer with ID {command.Id} not found");

        var document = DocumentNumber.Normalize(command.Document);
        var customerWithDocument = await _customerRepository.GetByDocumentAsync(document, cancellationToken);
        if (customerWithDocument != null && customerWithDocument.Id != customer.Id)
            throw new DuplicateEntryException($"Customer with document {document} already exists");

        customer.Name = command.Name;
        customer.Document = document;

        var updatedCustomer = await _customerRepository.UpdateAsync(customer, cancellationToken);
        return _mapper.Map<UpdateCustomerResult>(updatedCustomer);
    }
}
