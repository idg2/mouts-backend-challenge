using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Validation;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Handler for processing CreateCustomerCommand requests.
/// </summary>
public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, CreateCustomerResult>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of CreateCustomerHandler.
    /// </summary>
    /// <param name="customerRepository">The customer repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public CreateCustomerHandler(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    // Work item: TASK-018 (FEAT-010), FEAT-012
    /// <summary>
    /// Handles the CreateCustomerCommand request. The document is stored without mask and in upper case, and must
    /// not be in use.
    /// </summary>
    /// <param name="command">The CreateCustomer command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created customer</returns>
    public async Task<CreateCustomerResult> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var validator = new CreateCustomerValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var document = DocumentNumber.Normalize(command.Document);
        if (await _customerRepository.GetByDocumentAsync(document, cancellationToken) != null)
            throw new DuplicateEntryException($"Customer with document {document} already exists");

        var customer = _mapper.Map<Customer>(command);
        customer.Document = document;
        var createdCustomer = await _customerRepository.CreateAsync(customer, cancellationToken);
        return _mapper.Map<CreateCustomerResult>(createdCustomer);
    }
}
