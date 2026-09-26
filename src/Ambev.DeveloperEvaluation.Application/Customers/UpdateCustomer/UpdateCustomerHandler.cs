using Ambev.DeveloperEvaluation.Common.Tracing;
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

    // Work item: TASK-018 (FEAT-010), FEAT-012, TASK-049 (FEAT-017)
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
        StepTrace.Step("CUS-UPD-02", "CMN-PIP-10", "Validate the command", [("id", command.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        StepTrace.Step("CUS-UPD-03", "Load the customer", [("id", command.Id)]);
        var customer = await _customerRepository.GetByIdAsync(command.Id, cancellationToken);
        StepTrace.Step("CUS-UPD-04", "Customer found?", [("id", command.Id), ("found", customer != null)]);
        if (customer == null)
            throw new KeyNotFoundException($"Customer with ID {command.Id} not found");

        var document = DocumentNumber.Normalize(command.Document);
        StepTrace.Step("CUS-UPD-05", "Normalize the document", [("normalized", document), ("length", document.Length)]);
        var customerWithDocument = await _customerRepository.GetByDocumentAsync(document, cancellationToken);
        StepTrace.Step("CUS-UPD-06", "Document used by another customer?", [("document", document), ("usedByOther", customerWithDocument != null && customerWithDocument.Id != customer.Id), ("otherId", customerWithDocument?.Id)]);
        if (customerWithDocument != null && customerWithDocument.Id != customer.Id)
            throw new DuplicateEntryException($"Customer with document {document} already exists");

        customer.Name = command.Name;
        customer.Document = document;

        var updatedCustomer = await _customerRepository.UpdateAsync(customer, cancellationToken);
        StepTrace.Step("CUS-UPD-07", "Save the customer", [("id", updatedCustomer.Id), ("document", updatedCustomer.Document)]);
        return _mapper.Map<UpdateCustomerResult>(updatedCustomer);
    }
}
