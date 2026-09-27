# Registro de implementação

Um registro por sessão de trabalho, escrito antes do commit: o que foi implementado, por quê e o caminho completo (a partir da raiz do repositório) de cada arquivo tocado.

## FEAT-010 — CRUDs de cadastro

**Data:** 2026-09-24 · **Commit:** `49c848f` (PR #2, merge `ca00037` em `dev`)

**Implementado:** APIs CRUD de clientes, filiais, produtos e vendas, seguindo a arquitetura e o template de Users (Request/Validator/Profile no WebApi, Command/Handler/Validator/Result no Application). Vendas são um cadastro simples, sem regras de desconto (ficam para a FEAT-001). Também foram corrigidos problemas do template que bloqueavam a feature (BUG-004, BUG-006, factory de migrations e 404).

**Por quê:** os CRUDs vêm antes da matriz de descontos (FEAT-001), que depende de clientes, filiais, produtos e vendas persistidos.

**Verificação:** build limpo, 79/79 testes unitários, nenhuma alteração de modelo pendente e smoke test manual contra o PostgreSQL do compose (76 checagens ok) — TASK-023, sem arquivos versionados.

### TASK-013 — Correção da factory de design-time e migration de drift

A `YourDbContextFactory` passou a usar o assembly de migrations do ORM, e foi gerada a migration `SyncUserTimestamps`.

**Por quê:** `dotnet ef migrations add` falhava porque a factory apontava para o assembly WebApi, e o snapshot tinha uma divergência nos timestamps de `Users` que precisava ser migrada antes das tabelas novas.

FILES

- `backend/src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260924151808_SyncUserTimestamps.Designer.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260924151808_SyncUserTimestamps.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs` (alterado)

### TASK-014 — Entidades Customer, Branch e Product

Entidades `Customer`, `Branch` e `Product` com seus validators e testes de caminho feliz.

**Por quê:** são os cadastros que a venda referencia por id (External Identities do README).

FILES

- `backend/src/Ambev.DeveloperEvaluation.Domain/Entities/Branch.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Entities/Customer.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Entities/Product.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/BranchValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/CustomerValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/ProductValidator.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/BranchTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/CustomerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/ProductTests.cs` (novo)

### TASK-015 — Entidades Sale e SaleItem com SyncItems

Entidades `Sale` e `SaleItem`, validators e `Sale.SyncItems`: item com `Guid.Empty` é adicionado, item com id existente é atualizado, item não enviado é removido.

**Por quê:** A venda é um cadastro simples, sem regras de desconto nesta fase, e o PUT precisa sincronizar os itens por id.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/SaleItemValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/SaleValidator.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs` (novo)

### TASK-016 — Persistência: repositórios, mapeamentos, sequence e migration

Interfaces e implementações EF Core dos repositórios, mapeamentos sem FK para cliente, filial e produto, sequence `SaleNumbers` para o `SaleNumber`, `DbSet`s no `DefaultContext`, registro dos repositórios no IoC e a migration `AddRegistryCruds`.

**Por quê:** Persistir os cadastros no PostgreSQL seguindo External Identities (sem FK externa, descrição copiada no registro), com o número da venda gerado pelo banco.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/IBranchRepository.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/ICustomerRepository.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/IProductRepository.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Mapping/BranchConfiguration.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Mapping/CustomerConfiguration.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Mapping/ProductConfiguration.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleItemConfiguration.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260924151926_AddRegistryCruds.Designer.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260924151926_AddRegistryCruds.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/BranchRepository.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/CustomerRepository.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/ProductRepository.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs` (novo)

### TASK-017 — 404 para registro não encontrado

`ValidationExceptionMiddleware` passou a capturar `KeyNotFoundException` e devolver 404 com `ApiResponse`.

**Por quê:** Os handlers sinalizam "não encontrado" com `KeyNotFoundException`, que antes virava 500.

FILES

- `backend/src/Ambev.DeveloperEvaluation.WebApi/Middleware/ValidationExceptionMiddleware.cs` (alterado)

### BUG-004 — BaseController.Ok<T> envolvia a resposta duas vezes

Removido `BaseController.Ok<T>`; `OkPaginated` passou a usar `ControllerBase.Ok`.

**Por quê:** `Ok<T>` sombreava `ControllerBase.Ok`, e as respostas de Users, login e listas paginadas saíam com o envelope duplicado.

FILES

- `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/BaseController.cs` (alterado)

### BUG-006 — Login retornava 500 por falta de mapeamentos

`AuthenticateUserProfile` ganhou os mapas `AuthenticateUserRequest` → `AuthenticateUserCommand` e `AuthenticateUserResult` → `AuthenticateUserResponse`.

**Por quê:** Sem esses mapas o login dava 500, e os endpoints de escrita precisam do token.

FILES

- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Auth/AuthenticateUserFeature/AuthenticateUserProfile.cs` (alterado)

### TASK-018 — API de Customers

Casos de uso Create, Get, List, Update e Delete de clientes, `CustomersController` e testes dos handlers.

**Por quê:** CRUD de clientes no padrão do template de Users.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/CreateCustomer/CreateCustomerCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/CreateCustomer/CreateCustomerHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/CreateCustomer/CreateCustomerProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/CreateCustomer/CreateCustomerResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/CreateCustomer/CreateCustomerValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/DeleteCustomer/DeleteCustomerCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/DeleteCustomer/DeleteCustomerHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/DeleteCustomer/DeleteCustomerResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/DeleteCustomer/DeleteCustomerValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/GetCustomer/GetCustomerCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/GetCustomer/GetCustomerHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/GetCustomer/GetCustomerProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/GetCustomer/GetCustomerResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/GetCustomer/GetCustomerValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/ListCustomers/ListCustomersCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/ListCustomers/ListCustomersHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/ListCustomers/ListCustomersProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/ListCustomers/ListCustomersResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/ListCustomers/ListCustomersValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/UpdateCustomer/UpdateCustomerCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/UpdateCustomer/UpdateCustomerHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/UpdateCustomer/UpdateCustomerProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/UpdateCustomer/UpdateCustomerResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/UpdateCustomer/UpdateCustomerValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CreateCustomer/CreateCustomerProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CreateCustomer/CreateCustomerRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CreateCustomer/CreateCustomerRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CreateCustomer/CreateCustomerResponse.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CustomersController.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/DeleteCustomer/DeleteCustomerProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/DeleteCustomer/DeleteCustomerRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/DeleteCustomer/DeleteCustomerRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/GetCustomer/GetCustomerProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/GetCustomer/GetCustomerRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/GetCustomer/GetCustomerRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/GetCustomer/GetCustomerResponse.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/ListCustomers/ListCustomersProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/ListCustomers/ListCustomersRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/ListCustomers/ListCustomersRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/ListCustomers/ListCustomersResponse.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/UpdateCustomer/UpdateCustomerProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/UpdateCustomer/UpdateCustomerRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/UpdateCustomer/UpdateCustomerRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/UpdateCustomer/UpdateCustomerResponse.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Customers/CreateCustomerHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Customers/DeleteCustomerHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Customers/GetCustomerHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Customers/ListCustomersHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Customers/UpdateCustomerHandlerTests.cs` (novo)

### TASK-019 — API de Branches

Casos de uso Create, Get, List, Update e Delete de filiais, `BranchesController` e testes dos handlers.

**Por quê:** CRUD de filiais no padrão do template de Users.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/CreateBranch/CreateBranchCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/CreateBranch/CreateBranchHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/CreateBranch/CreateBranchProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/CreateBranch/CreateBranchResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/CreateBranch/CreateBranchValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/DeleteBranch/DeleteBranchCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/DeleteBranch/DeleteBranchHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/DeleteBranch/DeleteBranchResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/DeleteBranch/DeleteBranchValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/GetBranch/GetBranchCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/GetBranch/GetBranchHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/GetBranch/GetBranchProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/GetBranch/GetBranchResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/GetBranch/GetBranchValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/ListBranches/ListBranchesCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/ListBranches/ListBranchesHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/ListBranches/ListBranchesProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/ListBranches/ListBranchesResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/ListBranches/ListBranchesValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/UpdateBranch/UpdateBranchCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/UpdateBranch/UpdateBranchHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/UpdateBranch/UpdateBranchProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/UpdateBranch/UpdateBranchResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/UpdateBranch/UpdateBranchValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/BranchesController.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/CreateBranch/CreateBranchProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/CreateBranch/CreateBranchRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/CreateBranch/CreateBranchRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/CreateBranch/CreateBranchResponse.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/DeleteBranch/DeleteBranchProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/DeleteBranch/DeleteBranchRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/DeleteBranch/DeleteBranchRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/GetBranch/GetBranchProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/GetBranch/GetBranchRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/GetBranch/GetBranchRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/GetBranch/GetBranchResponse.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/ListBranches/ListBranchesProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/ListBranches/ListBranchesRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/ListBranches/ListBranchesRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/ListBranches/ListBranchesResponse.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/UpdateBranch/UpdateBranchProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/UpdateBranch/UpdateBranchRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/UpdateBranch/UpdateBranchRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/UpdateBranch/UpdateBranchResponse.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Branches/CreateBranchHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Branches/DeleteBranchHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Branches/GetBranchHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Branches/ListBranchesHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Branches/UpdateBranchHandlerTests.cs` (novo)

### TASK-020 — API de Products

Casos de uso Create, Get, List, Update e Delete de produtos, `ProductsController` e testes dos handlers.

**Por quê:** CRUD de produtos no padrão do template de Users.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Application/Products/CreateProduct/CreateProductCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/CreateProduct/CreateProductHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/CreateProduct/CreateProductProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/CreateProduct/CreateProductResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/CreateProduct/CreateProductValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/DeleteProduct/DeleteProductCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/DeleteProduct/DeleteProductHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/DeleteProduct/DeleteProductResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/DeleteProduct/DeleteProductValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/GetProduct/GetProductCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/GetProduct/GetProductHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/GetProduct/GetProductProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/GetProduct/GetProductResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/GetProduct/GetProductValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/ListProducts/ListProductsCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/ListProducts/ListProductsHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/ListProducts/ListProductsProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/ListProducts/ListProductsResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/ListProducts/ListProductsValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/UpdateProduct/UpdateProductCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/UpdateProduct/UpdateProductHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/UpdateProduct/UpdateProductProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/UpdateProduct/UpdateProductResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/UpdateProduct/UpdateProductValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/CreateProduct/CreateProductProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/CreateProduct/CreateProductRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/CreateProduct/CreateProductRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/CreateProduct/CreateProductResponse.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/DeleteProduct/DeleteProductProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/DeleteProduct/DeleteProductRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/DeleteProduct/DeleteProductRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/GetProduct/GetProductProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/GetProduct/GetProductRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/GetProduct/GetProductRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/GetProduct/GetProductResponse.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ListProducts/ListProductsProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ListProducts/ListProductsRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ListProducts/ListProductsRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ListProducts/ListProductsResponse.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ProductsController.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/UpdateProduct/UpdateProductProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/UpdateProduct/UpdateProductRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/UpdateProduct/UpdateProductRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/UpdateProduct/UpdateProductResponse.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Products/CreateProductHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Products/DeleteProductHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Products/GetProductHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Products/ListProductsHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Products/UpdateProductHandlerTests.cs` (novo)

### TASK-021 — API de Sales: criar, consultar e listar

Casos de uso Create, Get e List de vendas, com cópia de `CustomerName`, `BranchName`, `ProductDescription` e `UnitPrice` a partir dos cadastros, e registro de `TimeProvider.System` para o `SaleDate` em UTC.

**Por quê:** External Identities: a venda guarda a descrição copiada no momento do registro; o `TimeProvider` torna a data testável.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/Common/SaleItemResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/Common/SaleProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/Common/SaleResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleItemInput.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/GetSale/GetSaleCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/GetSale/GetSaleHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/GetSale/GetSaleValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/ApplicationModuleInitializer.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/Common/SaleItemResponse.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/Common/SaleResponse.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/Common/SaleResponseProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleItemRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/GetSale/GetSaleProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/GetSale/GetSaleRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/GetSale/GetSaleRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/ListSalesProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/ListSalesRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/ListSalesRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/ListSalesResponse.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSaleHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/GetSaleHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSalesHandlerTests.cs` (novo)

### TASK-022 — API de Sales: atualizar e excluir

Casos de uso Update (sincroniza itens por id; a cópia só é renovada quando o id referenciado muda) e Delete de vendas, com testes dos handlers.

**Por quê:** Completar o CRUD de vendas mantendo as descrições copiadas estáveis quando a referência não muda.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleItemInput.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/DeleteSale/DeleteSaleProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/DeleteSale/DeleteSaleRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/DeleteSale/DeleteSaleRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/UpdateSaleItemRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/UpdateSaleProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/UpdateSaleRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/UpdateSaleRequestValidator.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/DeleteSaleHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSaleHandlerTests.cs` (novo)

### Pendências registradas como backlog

FEAT-011, TD-006, TD-007, TD-008, TD-009, TD-010, BUG-005, BUG-007, BUG-008, BUG-009 (detalhes em `docs/work-items.json`).

## BUG-001 — GetCurrentUserId lia o id do usuário como int

**Data:** 2026-09-24 · **Commit:** `5de01b6` (PR #3, `dev` → `main`)

**Implementado:** `BaseController.GetCurrentUserId` passou a retornar `Guid` e a usar `Guid.Parse` no claim `NameIdentifier`. Teste novo em `BaseControllerTests`; para isso o projeto de testes Unit passou a referenciar o projeto WebApi.

**Por quê:** o claim `NameIdentifier` recebe `User.Id.ToString()`, que é um Guid, então o `int.Parse` lançava `FormatException` na primeira chamada. Ainda não há quem chame o método.

**Verificação:** o teste falhou com `FormatException` antes da correção e passou depois; suíte 80/80.

FILES

- `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/BaseController.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Ambev.DeveloperEvaluation.Unit.csproj` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/BaseControllerTests.cs` (novo)

## BUG-002 — Login inválido retornava 500

**Data:** 2026-09-24 · **Commit:** `1e698e5` (PR #4, `dev` → `main`)

**Implementado:** o `ValidationExceptionMiddleware` passou a capturar `UnauthorizedAccessException` e a responder 401 com `ApiResponse { success: false, message }`, no mesmo formato do 404 já existente. Teste novo em `ValidationExceptionMiddlewareTests`.

**Por quê:** o `AuthenticateUserHandler` lança `UnauthorizedAccessException` para e-mail inexistente, senha errada ou usuário inativo, e nenhum middleware tratava essa exceção, então o ASP.NET respondia 500. O `AuthController` já declarava 401 como resposta esperada.

**Verificação:** reproduzido na API (container reconstruído a partir de `dev`): e-mail inexistente e senha errada davam 500 com `UnauthorizedAccessException`. O teste falhou com a exceção escapando do middleware e passou após a correção; suíte 81/81. Na API corrigida, os dois cenários retornam 401 `{"success":false,"message":"Invalid credentials","errors":[]}` e o login correto segue com 200.

FILES

- `backend/src/Ambev.DeveloperEvaluation.WebApi/Middleware/ValidationExceptionMiddleware.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Middleware/ValidationExceptionMiddlewareTests.cs` (novo)

## BUG-007 — Item nulo na venda retornava 500

**Data:** 2026-09-24 · **Commit:** `162cb49` (PR #5, `dev` → `main`)

**Implementado:** os validadores de criação e atualização de venda (Request na WebApi e Command na Application) ganharam `RuleForEach(sale => sale.Items).NotNull()`. Nos validadores de atualização, a regra de ids de item únicos passou a aceitar item nulo e lista nula sem lançar exceção.

**Por quê:** o `ChildRules` do FluentValidation pula elementos nulos, então `"items":[null]` passava na validação e estourava `NullReferenceException` no handler (500). Na atualização, a regra de ids únicos lançava a exceção já dentro do validador. Defeito do próprio FEAT-010, que deveria ter sido pego na task.

**Verificação:** reproduzido na API (POST e PUT com `items:[null]` → 500). Os testes de item nulo e de lista nula falharam antes da correção e passam depois. Na API corrigida, os dois casos retornam 400.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequestValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/UpdateSaleRequestValidator.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSaleValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSaleValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/CreateSaleRequestValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/UpdateSaleRequestValidatorTests.cs` (novo)

## BUG-008 — `_page` muito grande estourava o offset da listagem

**Data:** 2026-09-24 · **Commit:** `162cb49` (PR #5, `dev` → `main`)

**Implementado:** o `ListAsync` dos repositórios de clientes, filiais, produtos e vendas passou a calcular o offset em `long`. Quando o offset passa do total de registros, o método devolve a página vazia com o total, sem consultar os itens. Para testar os repositórios, o projeto Integration ganhou o `PostgresFixture` previsto no plano do FEAT-001: ele cria um banco temporário por execução, aplica as migrations e apaga o banco no fim. A connection string vem do `appsettings` da WebApi ou da variável `ConnectionStrings__DefaultConnection`, sem nada fixo no código. Foram adicionados os pacotes `Microsoft.Extensions.Configuration.Json` 8.0.1 e `Microsoft.Extensions.Configuration.EnvironmentVariables` 8.0.0.

**Por quê:** `(page - 1) * size` estourava `int` para páginas enormes. O Postgres recebia um OFFSET negativo (`2201X: OFFSET must not be negative`) e a API respondia 500. Defeito do próprio FEAT-010.

**Verificação:** reproduzido na API (`_page=2147483647&_size=100` → 500 nos quatro recursos). O teste de integração falhou com `OFFSET must not be negative` e passa após a correção. Na API corrigida, os quatro recursos retornam 200 com `data: []` e o `totalCount` real.

FILES

- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/BranchRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/CustomerRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/ProductRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/Ambev.DeveloperEvaluation.Integration.csproj` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/PostgresFixture.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/ListPaginationTests.cs` (novo)

## BUG-009 — Valores monetários com mais de duas casas eram arredondados em silêncio

**Data:** 2026-09-24 · **Commit:** `162cb49` (PR #5, `dev` → `main`)

**Implementado:** regra `PrecisionScale(18, 2, true)` para o preço do produto e para os valores e totais da venda e dos itens, e `PrecisionScale(5, 2, true)` para o percentual de desconto. As regras foram aplicadas nas três camadas: Request (WebApi), Command (Application) e validadores de domínio (`ProductValidator`, `SaleValidator`, `SaleItemValidator`). Zeros à direita, como `10.10`, continuam aceitos.

**Por quê:** as colunas são `numeric(18,2)` e `numeric(5,2)`. `10.123` era devolvido na resposta, mas gravado como `10.12`. `0.001` passava no "maior que zero", virava `0.00` no banco e quebrava a CHECK constraint (500). Valores com mais de 16 dígitos inteiros estouravam a coluna. Defeito do próprio FEAT-010.

**Verificação:** reproduzido na API: produto com `0.001` → 500; produto com `10.123` → 201 respondendo `10.123` e GET mostrando `10.12`; venda com percentual `12.345` → 201. Os testes de precisão falharam antes e passam depois. Na API corrigida, os três casos retornam 400 com a mensagem do `ScalePrecisionValidator`.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Application/Products/CreateProduct/CreateProductValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/UpdateProduct/UpdateProductValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/ProductValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/SaleItemValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/SaleValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/CreateProduct/CreateProductRequestValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/UpdateProduct/UpdateProductRequestValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequestValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/UpdateSaleRequestValidator.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Products/CreateProductValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Products/UpdateProductValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Products/CreateProductRequestValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Products/UpdateProductRequestValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/ProductTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSaleValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSaleValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/CreateSaleRequestValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/UpdateSaleRequestValidatorTests.cs` (novo)

## TD-010 — Ordem estável dos itens da venda

**Data:** 2026-09-24 · **Commit:** `162cb49` (PR #5, `dev` → `main`)

**Implementado:**
- **Número da linha:** coluna `LineNumber` no `SaleItem`, com a posição do item na requisição, a partir de 1. A criação numera as linhas na ordem do comando, e o `Sale.SyncItems` renumera na atualização pela ordem recebida, para itens mantidos, movidos ou novos.
- **Leitura e resposta:** o `SaleRepository.GetByIdAsync` carrega os itens ordenados por `LineNumber`, e o `SaleProfile` monta o resultado na mesma ordem.
- **Validação e banco:** o validador de domínio exige `LineNumber > 0`, e o banco tem a CHECK `CK_SaleItems_LineNumber`.
- **Migration `AddSaleItemLineNumber`:** cria a coluna e numera as vendas já existentes pela ordem do id do item.

**Por quê:** o GET trazia os itens na ordem física do banco, diferente da resposta do POST. Ordenar pelo id deixaria as respostas iguais entre si, mas embaralhadas em relação ao que o cliente enviou. Com o número da linha, POST, PUT e GET devolvem os itens na ordem enviada.

**Verificação:**
- **Testes que falharam antes da correção:** numeração na criação, renumeração no `SyncItems`, ordem no `SaleProfile` e validação de linha 0. Voltando o repositório para ordenar por id, o teste de integração falha, com as linhas saindo como 3, 1, 2.
- **Migration:** o preenchimento das linhas foi testado num banco descartável, e depois a migration foi aplicada no banco local.
- **API:** o POST enviado como [1,2,3,4,5] volta [1,2,3,4,5] no POST e no GET. O PUT enviado como [novo,5,3,1] volta nessa ordem no PUT e no GET.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/SaleItemValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/Common/SaleProfile.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleItemConfiguration.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260924172355_AddSaleItemLineNumber.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260924172355_AddSaleItemLineNumber.Designer.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/SaleRepositoryTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/ListPaginationTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleProfileTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSaleHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs` (alterado)

## TD-007 — Testes dos CRUDs além do caminho feliz

**Data:** 2026-09-24 · **Commit:** `162cb49` (PR #5, `dev` → `main`)

**Implementado:** testes de erro e de limite para os quatro cadastros:
- **Listagem:** página 0, size 0 e size 101 nos oito validadores de listagem.
- **Ids:** id vazio e id inexistente (404) nos handlers de get, update e delete.
- **Referências da venda:** cliente, filial e produto inexistentes na criação e na atualização, e item de outra venda na atualização.
- **Venda:** lista de itens vazia, ids vazios e ids de item duplicados.
- **Produto:** descrição vazia ou com 201 caracteres.
- **WebApi:** 404 do `ValidationExceptionMiddleware` e o `Ok()` do `BaseController` sem embrulhar a resposta duas vezes (regressão do BUG-004).

Ficaram de fora os geradores Bogus, por decisão sua nesta sessão.

**Por quê:** o spec do FEAT-010 (D14) previa só o caminho feliz. A falta de testes de erro e de limite é o que deixou passar o BUG-007, o BUG-008 e o BUG-009.

**Verificação:** estes testes cobrem comportamento que já existia e passaram de primeira. Os testes que dependiam das correções estão nos itens acima. Suíte Unit 218/218, Integration 9/9 (com o TD-010). O build não tem avisos novos: os únicos são os NU1903 e o CS8604 do template.

FILES

- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/ListValidatorsTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/NotFoundHandlersTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/ListRequestValidatorsTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSaleHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSaleHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/BaseControllerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Middleware/ValidationExceptionMiddlewareTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSaleValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSaleValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/CreateSaleRequestValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/UpdateSaleRequestValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Products/CreateProductValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Products/UpdateProductValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Products/CreateProductRequestValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Products/UpdateProductRequestValidatorTests.cs` (novo)

## TD-002 — Connection string do Postgres e portas fixas no Compose

**Data:** 2026-09-24 · **Commit:** `bd8dac6` (PR #5, `dev` → `main`)

**Implementado:**
- **`appsettings.json`:** o `DefaultConnection` da WebApi trocou a string no formato SQL Server pela do Postgres do Compose (`Host=localhost;Port=5432;Database=developer_evaluation;...`).
- **`docker-compose.yml`:** as portas do host ficaram fixas nos valores padrão: Postgres 5432, MongoDB 27017, Redis 6379 e API 8080.
- **Forma de uso:** não mudou. A API, o `dotnet ef` e os testes de integração continuam lendo `ConnectionStrings:DefaultConnection`, e a variável de ambiente continua podendo sobrescrever.

**Por quê:** o provider é Npgsql, mas a string versionada era de SQL Server, e o Compose publicava o Postgres numa porta aleatória do host. Por isso, `dotnet run`, `dotnet ef` e os testes só funcionavam com a variável `ConnectionStrings__DefaultConnection` exportada na mão, com a porta do momento.

**Verificação:**
- **Sem a variável de ambiente:** Unit 218/218 e Integration 9/9. O `dotnet ef migrations list` conectou ao banco, e o `dotnet ef database update` aplicou a migration do TD-010.
- **Containers:** recriados com as portas fixas. O banco manteve os dados: 5 usuários, 2 vendas e 8 produtos antes e depois.
- **Risco conhecido:** essas portas colidem com qualquer outro projeto que use as padrão ao mesmo tempo.

FILES

- `backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.json` (alterado)
- `backend/docker-compose.yml` (alterado)


## BUG-005 — GET /api/users/{id} retornava 500

**Data:** 2026-09-24 · **Commit:** `510895d` (PR #7, `dev` → `main`)

**Implementado:**
- **WebApi `GetUserProfile`:** ganhou o mapa `GetUserResult → GetUserResponse`, que não existia. O controller chamava `Map<GetUserResponse>`, o AutoMapper lançava `AutoMapperMappingException` e o middleware devolvia 500.
- **Application `GetUserProfile`:** o mapa `User → GetUserResult` passou a preencher `Name` a partir de `Username`. Sem isso, depois de corrigido o 500, o GET respondia 200 com `name` vazio.

**Por quê:** código original do template, sem alteração nossa. O bug foi encontrado no planejamento do FEAT-010 e ficou no backlog, porque mudanças em `Users` estavam fora do escopo daquele feature. O BUG-006, do mesmo tipo, foi corrigido lá porque bloqueava o login.

**Verificação:**
- **TDD:** os dois testes de profile falharam primeiro, um com `Missing type map configuration` e o outro com `name` vazio, e passaram depois da correção. Unit 220/220, Integration 9/9, e o build não tem warnings novos.
- **API reconstruída:** antes da correção, `GET /api/users/{id}` de um usuário recém-criado retornava 500. Depois, retorna 200 com `name`, `email`, `phone`, `role` e `status` corretos. Um id desconhecido continua retornando 404.
- **Encontrado e deixado no backlog:** BUG-010. O `POST /api/users` devolve os campos do usuário vazios porque o `CreateUserResult` só tem `Id`; o banco grava os valores certos.

FILES

- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Users/GetUser/GetUserProfile.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Users/GetUser/GetUserProfile.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/GetUserProfileTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Users/GetUserProfileTests.cs` (novo)

## BUG-003 — E-mail duplicado no cadastro de usuário retornava 500

**Data:** 2026-09-24 · **Commit:** `a73858e` (PR #8, `dev` → `main`)

**Implementado:**
- **`DuplicateEntryException`** (Domain/Exceptions, herda de `DomainException`): nova exceção para entrada que repete um valor que deve ser único. O nome foi escolhido pelo usuário no lugar de "Conflict". Ela também é usada no código do produto (FEAT-013) e será usada no Document do cliente (FEAT-012).
- **`ValidationExceptionMiddleware`:** converte `DuplicateEntryException` em 409, no mesmo formato `ApiResponse` (`success: false`, `message`) do 401 e do 404.
- **`CreateUserHandler`:** para e-mail já cadastrado, lança `DuplicateEntryException` em vez de `InvalidOperationException`.

**Por quê:** nenhum middleware tratava `InvalidOperationException`, então o e-mail duplicado virava 500. Mapear `InvalidOperationException` inteira para 409 foi descartado, porque o EF e o LINQ também lançam essa exceção, e erros reais passariam a parecer conflito. O código é do template e não tinha sido alterado por nós.

**Verificação:**
- **TDD:** os testes do middleware (409) e do handler (e-mail em uso) falharam primeiro e passaram depois da correção.
- **API contra um banco descartável:** o segundo POST com o mesmo e-mail retorna 409 com "User with email ... already exists".
- **Encontrado e deixado no backlog:** BUG-011. `Users.Email` não tem índice único, então duas requisições simultâneas ainda podem gravar o mesmo e-mail.
- **Pendência para o FEAT-001:** o middleware que o plano do FEAT-001 reescreve converte `DomainException` em 422. Como `DuplicateEntryException` herda de `DomainException`, o catch dela precisa vir antes, para continuar em 409.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Domain/Exceptions/DuplicateEntryException.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Middleware/ValidationExceptionMiddleware.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserHandler.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Middleware/ValidationExceptionMiddlewareTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/CreateUserHandlerTests.cs` (alterado)

## FEAT-013 — Código único do produto

**Data:** 2026-09-24 · **Commit:** `e3ac865` (PR #8, `dev` → `main`)

**Implementado:**
- **Campo `Code`:** o produto ganhou um `Code` obrigatório, com até 50 caracteres, gravado sem espaços nas pontas e em maiúsculas. Assim, "abc-1" e "ABC-1" são o mesmo código. O campo aparece no create, update, get e list, com validação no request, no command e na entidade.
- **Duplicidade:** os handlers de create e update normalizam o código e consultam `GetByCodeAsync`. Se o código já existe (no update, em outro produto), lançam `DuplicateEntryException` e a API retorna 409. No update, manter o próprio código é permitido.
- **Banco:** a coluna `Code varchar(50) NOT NULL` ganhou o índice único `IX_Products_Code`. Se duas requisições passarem juntas pela checagem do handler, o `ProductRepository` converte a violação desse índice em `DuplicateEntryException`, então a resposta também é 409, e não 500.
- **Migration `20260924181808_AddProductCode`:** apaga todos os produtos antes de criar a coluna, como o usuário decidiu. As vendas não têm FK para produto e mantêm a descrição copiada nos itens.

**Por quê:** pedido do usuário, para impedir produtos com o mesmo código. O mesmo mecanismo de duplicidade será usado depois no Document do cliente.

**Verificação:**
- **TDD:** os testes de validação (5 lugares), dos handlers (normalização, duplicado no create, duplicado no update, próprio código) e do repositório falharam primeiro. Os testes do repositório falharam com `DbUpdateException` quando a conversão estava desativada. Unit 248/248, Integration 12/12, `has-pending-model-changes` sem mudanças, e o build não tem warnings novos.
- **Migration:** verificada num banco descartável: 2 produtos antes, 0 depois, coluna NOT NULL e índice único.
- **API contra um banco descartável:**
  - `"  beer-350 "` foi gravado como `BEER-350`;
  - código repetido no POST e no PUT para o código de outro produto retornam 409;
  - código vazio retorna 400;
  - PUT com o próprio código retorna 200;
  - GET e lista trazem `code`.
- **Pendente:** aplicar a migration no banco local, que apaga os 10 produtos. Quem aplica é o usuário.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Domain/Entities/Product.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/ProductValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/IProductRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/CreateProduct/CreateProductCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/CreateProduct/CreateProductHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/CreateProduct/CreateProductResult.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/CreateProduct/CreateProductValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/UpdateProduct/UpdateProductCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/UpdateProduct/UpdateProductHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/UpdateProduct/UpdateProductResult.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/UpdateProduct/UpdateProductValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/GetProduct/GetProductResult.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/ListProducts/ListProductsResult.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Mapping/ProductConfiguration.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/ProductRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260924181808_AddProductCode.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260924181808_AddProductCode.Designer.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/CreateProduct/CreateProductRequest.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/CreateProduct/CreateProductRequestValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/CreateProduct/CreateProductResponse.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/UpdateProduct/UpdateProductRequest.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/UpdateProduct/UpdateProductRequestValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/UpdateProduct/UpdateProductResponse.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/GetProduct/GetProductResponse.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ListProducts/ListProductsResponse.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/ProductRepositoryTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/ListPaginationTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/NotFoundHandlersTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Products/CreateProductHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Products/CreateProductValidatorTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Products/UpdateProductHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Products/UpdateProductValidatorTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/ProductTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Products/CreateProductRequestValidatorTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Products/UpdateProductRequestValidatorTests.cs` (alterado)

## Work items — remoção do campo `branch`

Removido o campo `branch` de todos os itens de `docs/work-items.json` e o bloco `meta.branchPrefixes`; o `work-items.json` da raiz foi reexportado. Desde que todo o trabalho passou a ser feito direto no `dev`, as branches `feature/`, `fix/` e `chore/` deixaram de existir e o campo ficava inconsistente: itens antigos com `feature/registry-cruds`, itens novos com `null` e o FEAT-001 apontando para uma branch que nunca será criada. O CLAUDE.md deixou de exigir o campo e marca como obsoletas as regras de `branch` da spec de rastreamento de work items. Mudança sem work item próprio, por ser só de metadados de rastreamento.

FILES
- `CLAUDE.md` (alterado)

## BUG-010 — POST /api/users devolvia os campos do usuário vazios

**Data:** 2026-09-24 · **Commit:** `2e12675` (PR #9, `dev` → `main`)

**Implementado:**
- **`CreateUserResult`:** tinha só `Id`. Ganhou `Name`, `Email`, `Phone`, `Role` e `Status`, os mesmos campos do `CreateUserResponse`.
- **Mapeamento:** o `CreateUserProfile` da Application mapeia `Name` a partir de `Username`, como o `GetUserProfile` já faz desde o BUG-005. O mapeamento da WebApi (`CreateUserResult` → `CreateUserResponse`) não mudou; ele já copiava pelo nome e passou a encontrar os campos.

**Por quê:** o 201 do cadastro de usuário trazia `name`, `email` e `phone` vazios e `role` e `status` zerados, embora o usuário fosse gravado com os valores enviados.

**Verificação:**
- **TDD:** os dois testes de profile falharam primeiro na compilação (campos inexistentes). Depois o teste da Application falhou com `Name` vazio, até o `ForMember` entrar. Unit 250/250.
- **API contra um banco descartável:** o POST retorna 201 com `name`, `email`, `phone`, `role` e `status` iguais aos enviados.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserResult.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserProfile.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Users/CreateUserProfileTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/CreateUserProfileTests.cs` (novo)

## BUG-011 — Users.Email sem índice único

**Data:** 2026-09-24 · **Commit:** `712e3fd` (PR #9, `dev` → `main`)

**Implementado:**
- **Banco:** índice único `IX_Users_Email` na coluna `Email` da tabela `Users`, pela migration `AddUserEmailUniqueIndex`. A migration só cria o índice e não mexe em dados. Um banco que já tenha e-mails repetidos faz a migration falhar, de propósito, em vez de apagar usuários sem aviso.
- **Repositório:** o `UserRepository.CreateAsync` converte a violação desse índice em `DuplicateEntryException`, igual ao que o `ProductRepository` faz com o código do produto. Assim a resposta é 409, e não 500.
- **Handler:** a checagem por `GetByEmailAsync` do BUG-003 continua. O índice cobre o caso de duas requisições passarem juntas por ela.

**Por quê:** a checagem do handler lê e depois grava, então duas requisições simultâneas com o mesmo e-mail podiam gravar dois usuários.

**Verificação:**
- **TDD:** o teste de integração falhou primeiro sem exceção (o segundo usuário era gravado). Com o índice, falhou com `DbUpdateException`. Com a conversão no repositório, passou. Integration 13/13, Unit 250/250, e o build não tem warnings novos.
- **Migration:** o `Up` gerado só cria `IX_Users_Email`. O banco local não tem e-mails repetidos.
- **API contra um banco descartável:**
  - o segundo POST com o mesmo e-mail retorna 409;
  - 20 POSTs simultâneos com um e-mail novo resultam em um 201 e dezenove 409, com um único usuário gravado.
- **Pendente:** aplicar a migration no banco local. Quem aplica é o usuário.

FILES

- `backend/src/Ambev.DeveloperEvaluation.ORM/Mapping/UserConfiguration.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/UserRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260924183026_AddUserEmailUniqueIndex.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260924183026_AddUserEmailUniqueIndex.Designer.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/UserRepositoryTests.cs` (novo)

## TD-006 — Transações explícitas nos comandos de escrita

**Data:** 2026-09-24 · **Commit:** `68c9486` (PR #10, `dev` → `main`)

**Implementado:**
- **Domain:** interface `IUnitOfWork` com `BeginTransactionAsync`, `CommitTransactionAsync` e `RollbackTransactionAsync`.
- **ORM:** `UnitOfWork` sobre o `DefaultContext` com escopo da requisição. Os repositórios usam o mesmo contexto, então o `SaveChangesAsync` de cada um entra na transação aberta sem nenhuma mudança nos repositórios. O registro é feito no `InfrastructureModuleInitializer`.
- **Application:**
  - marcador `ITransactionalCommand`;
  - `TransactionBehavior` no pipeline do MediatR: um comando marcado roda o handler dentro de uma transação. Ela recebe commit quando o handler termina bem e rollback quando ele lança exceção, que depois é relançada.
  - O rollback usa `CancellationToken.None`, para acontecer mesmo com a requisição cancelada.
  - Requisições sem o marcador passam direto.
- **Comandos marcados (14):** Create/Update/Delete de Branches, Customers, Products e Sales, mais `CreateUserCommand` e `DeleteUserCommand`. Get, List e Authenticate ficam sem transação.
- **WebApi:** o `TransactionBehavior` é registrado depois do `ValidationBehavior`, então, quando os validators forem registrados no DI, a validação vai rodar antes de abrir a transação. Por enquanto o `ValidationBehavior` não faz nada: os validators da requisição rodam no controller, antes do `Send`, e os do comando rodam dentro do handler, portanto dentro da transação, e terminam em rollback.

**Por quê:** até aqui cada escrita dependia da transação implícita de um único `SaveChangesAsync` (decisão D7 dos CRUDs). A transação explícita passa a cobrir o comando inteiro, incluindo as leituras de pré-checagem do handler. Um comando que no futuro grave mais de uma vez continua atômico.

**Verificação:**
- **TDD:** os testes falharam primeiro na compilação e depois pelo motivo esperado:
  - behavior sem Begin/Commit/Rollback;
  - 14 comandos sem o marcador;
  - `UnitOfWork` com `NotImplementedException`.
  Depois passaram. Unit 255/255 (5 novos), Integration 15/15 (2 novos: rollback descarta a gravação, commit a mantém). O build não tem warnings novos, e `has-pending-model-changes` não aponta mudanças.
- **API contra um banco descartável com `log_statement = all`:**
  - criar usuário, criar, alterar e excluir cliente, e criar produto geraram `BEGIN … COMMIT`;
  - GETs e login não abriram transação;
  - produto com código repetido retornou 409 com `ROLLBACK`;
  - 20 POSTs simultâneos de usuário com o mesmo e-mail geraram 20 `BEGIN`, 1 `COMMIT` e 19 `ROLLBACK` (um 201 e dezenove 409), com um único usuário gravado.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/IUnitOfWork.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/UnitOfWork.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Common/ITransactionalCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Common/TransactionBehavior.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Program.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/CreateBranch/CreateBranchCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/UpdateBranch/UpdateBranchCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/DeleteBranch/DeleteBranchCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/CreateCustomer/CreateCustomerCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/UpdateCustomer/UpdateCustomerCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/DeleteCustomer/DeleteCustomerCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/CreateProduct/CreateProductCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/UpdateProduct/UpdateProductCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/DeleteProduct/DeleteProductCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Users/DeleteUser/DeleteUserCommand.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Common/TransactionBehaviorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Common/TransactionalCommandsTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/UnitOfWorkTests.cs` (novo)

## FEAT-011 — Filtros e ordenação nas listagens

**Data:** 2026-09-24 · **Commit:** `0048aa2` (PR #11, `dev` → `main`) · **Tasks:** TASK-024, TASK-025, TASK-026, TASK-027

**Implementado:**
- `GET /api/customers`, `/api/branches`, `/api/products` e `/api/sales` passam a aceitar a sintaxe do `.doc/general-api.md`:
  - filtro por campo da resposta (nome do JSON, sem diferenciar maiúsculas);
  - `*` no início e/ou no fim de textos;
  - `_min<Campo>`/`_max<Campo>` em números e datas;
  - `_order="campo desc, outro"`.
- Regras de combinação e comparação:
  - campo repetido vira OR, e campos diferentes, AND;
  - texto compara sem diferenciar maiúsculas (`ILIKE` com escape de `\`, `%` e `_`);
  - números usam a cultura invariante (`10,5` é 400);
  - datas são UTC: `yyyy-MM-dd` cobre o dia inteiro, e data com hora precisa ser ISO 8601.
- Ordem e erros:
  - sem `_order`, a ordem padrão de antes continua, sempre com `Id` como último desempate;
  - qualquer parâmetro inválido volta 400, no `ApiResponse` "Validation Failed", com um código por problema: `UnknownField`, `UnknownParameter`, `InvalidWildcard`, `InvalidValue`, `InvalidRange`, `RepeatedRange`, `InvalidOrder`, `RepeatedOrder`, `TooManyValues`.
- Camadas:
  - **Domain:** `ListQuery`, `FieldFilter`, `FilterOperator` e `SortField`; o `ListAsync(ListQuery, ct)` substitui `(page, size, ct)` nos quatro repositórios.
  - **ORM:** `ListQueryExtensions` monta filtros e ordem por nome de propriedade, com valores como parâmetros SQL e o OR em árvore balanceada.
  - **Application:** os comandos de listagem levam `Filters`/`Order`.
  - **WebApi:** o `ListQueryParser` valida contra as propriedades do `List*Response`.

**Por quê:** a decisão D12 dos CRUDs (FEAT-010) adiou filtros e ordenação; o `general-api.md` os define para as listagens.

**Revisão final (revisor independente):**
- **Crítico, corrigido:** repetir uma chave umas mil vezes montava um OR tão profundo que o EF Core estourava a pilha e derrubava o processo. Agora há um limite de 50 valores por campo (`TooManyValues`), e o OR é montado em árvore balanceada.
- **Importante, corrigido:** datas não ISO passavam por causa do "T" de "GMT" (`05/09/2026 10:30 GMT` virava 9 de maio). Agora é `TryParseExact` com formatos ISO.
- **Importante, corrigido:** `9999-12-31` como dia gerava 500. Agora é 400.
- **Menores, não corrigidos:**
  - o parser roda antes do validador de página, então os dois erros saem em corpos diferentes;
  - a mensagem de `_min` sozinho é estranha;
  - números não aceitam espaços em volta;
  - o teste de correspondência não confere o mapeamento do EF.

**Verificação:**
- **TDD:** cada task falhou primeiro na compilação e depois no comportamento. Os testes de repositório e o de correspondência foram provados por mutação.
- **Totais:** Unit 326/326 (71 novos); Integration 31/31 (16 novos, no Postgres). O build não tem warnings novos, e `has-pending-model-changes` não aponta mudanças.
- **SQL gerado:** `ILIKE @__Value_0 ESCAPE '\'`, com os valores como parâmetros.
- **e2e contra um banco descartável:**
  - antes do wiring, os filtros eram ignorados e os casos de erro voltavam 200;
  - depois, todos os filtros, faixas e ordens deram o resultado esperado nos quatro endpoints;
  - os casos inválidos voltaram 400 com o código certo;
  - `name` repetido 1000 vezes voltou 400, e a API continuou respondendo.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/ListBranches/ListBranchesCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/ListBranches/ListBranchesHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/ListCustomers/ListCustomersCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/ListCustomers/ListCustomersHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/ListProducts/ListProductsCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/ListProducts/ListProductsHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/FieldFilter.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/FilterOperator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/IBranchRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/ICustomerRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/IProductRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/ListQuery.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/SortField.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/BranchRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/CustomerRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/ListQueryExtensions.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/ProductRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/ListQueryParser.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/BranchesController.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CustomersController.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ProductsController.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/ListPaginationTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/ListQueryExtensionsTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/RepositoryListQueryTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Branches/ListBranchesHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Customers/ListCustomersHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Products/ListProductsHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSalesHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/ListQueryParserTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/ListResponseFieldsTests.cs` (novo)

## FEAT-012 — Documento (CPF/CNPJ) do cliente

**Data:** 2026-09-24 · **Commit:** `7cf0d81` (PR #12, `feature/FEAT-012` → `dev`)

**Implementado:**
- **Domain:**
  - `Customer.Document`, obrigatório e único.
  - `DocumentNumber.Normalize` remove a máscara (`.`, `-`, `/` e espaços) e coloca as letras em maiúsculas.
  - `CpfValidator`: 11 dígitos, sem sequência repetida e com os dois dígitos verificadores pelo módulo 11.
  - `CnpjValidator`: 12 caracteres `0-9A-Z` seguidos de 2 dígitos, sem sequência repetida, com os verificadores calculados sobre o código ASCII − 48. Vale para o CNPJ numérico e para o alfanumérico emitido desde julho de 2026 (exemplo da Receita: `12ABC34501DE35`).
  - `DocumentValidator` escolhe CPF ou CNPJ pelo tamanho (11 ou 14). O `CustomerValidator` passou a usá-lo e também rejeita documento nulo.
- **Application:**
  - Os validadores dos comandos validam o documento já normalizado.
  - Os handlers gravam o valor normalizado e antes checam se ele já existe: duplicado vira `DuplicateEntryException` (409).
  - Na alteração, o próprio documento do cliente é aceito.
  - Os resultados de criar, consultar, listar e alterar trazem `document`.
- **ORM:**
  - Coluna `Document varchar(14) NOT NULL` com o índice único `IX_Customers_Document`.
  - `GetByDocumentAsync`, e a violação do índice vira `DuplicateEntryException`, no mesmo padrão do FEAT-013.
  - A migration `AddCustomerDocument` apaga todos os clientes antes de adicionar a coluna. As vendas continuam, porque guardam uma cópia do nome e não têm chave estrangeira.
- **WebApi:**
  - Requests, validadores das requests e responses ganharam `document`.
  - A listagem já filtra e ordena por `document` pelo FEAT-011; o filtro compara com o valor sem máscara.

**Por quê:** o cliente precisava de um identificador fiscal único, e a Receita passou a emitir CNPJ alfanumérico.

**Revisão independente:**
- Não achou problema crítico nem importante.
- Um fuzz de 300 mil documentos não deu nenhuma divergência do algoritmo oficial.
- Correções feitas a partir da revisão:
  - documento nulo passava no `CustomerValidator`; agora falha, com teste que falhou antes;
  - entraram exemplos com dígito verificador 0, provados por mutação do ramo "resto < 2";
  - os comentários de work item foram alinhados ao padrão do FEAT-013;
  - a documentação do filtro por documento avisa que o valor vai sem máscara.
- Ficou para depois: `ſ` (U+017F) vira `S` ao passar para maiúsculas. É inofensivo, porque o valor gravado continua ASCII.

**Verificação:**
- **TDD:** cada camada falhou primeiro na compilação e depois no comportamento.
- **Totais:** Unit 392/392 (66 novos); Integration 34/34 (3 novos, contra o índice real). Os testes de integração que gravam clientes direto no banco passaram a usar documentos únicos gerados por `TestDocuments`.
- **Build e modelo:** o build não tem warnings novos, e `has-pending-model-changes` não aponta mudanças.
- **Migration num banco descartável:** o banco já tinha 2 clientes e 1 venda. Depois dela: 0 clientes, a venda mantida, a coluna `varchar(14) NOT NULL` e o índice único criados.
- **API (e2e):**
  - com máscara e alfanumérico em minúsculas, o documento é gravado normalizado;
  - duplicado volta 409;
  - verificador errado, sequência repetida, tamanho errado e documento ausente voltam 400;
  - alterar para o documento de outro cliente volta 409; manter o próprio volta 200;
  - filtro e ordenação por `document` funcionam;
  - 20 criações simultâneas com o mesmo documento dão um 201, dezenove 409 e uma única linha.
- **Pendente:** aplicar a migration no banco local. Isso fica com o usuário, e ela apaga os clientes que existirem.

FILES

- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/CreateCustomer/CreateCustomerCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/CreateCustomer/CreateCustomerHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/CreateCustomer/CreateCustomerResult.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/CreateCustomer/CreateCustomerValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/GetCustomer/GetCustomerResult.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/ListCustomers/ListCustomersResult.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/UpdateCustomer/UpdateCustomerCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/UpdateCustomer/UpdateCustomerHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/UpdateCustomer/UpdateCustomerResult.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/UpdateCustomer/UpdateCustomerValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Entities/Customer.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/ICustomerRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/CnpjValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/CpfValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/CustomerValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/DocumentNumber.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/DocumentValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Mapping/CustomerConfiguration.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260924200228_AddCustomerDocument.Designer.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260924200228_AddCustomerDocument.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/CustomerRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CreateCustomer/CreateCustomerRequest.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CreateCustomer/CreateCustomerRequestValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CreateCustomer/CreateCustomerResponse.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/GetCustomer/GetCustomerResponse.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/ListCustomers/ListCustomersResponse.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/UpdateCustomer/UpdateCustomerRequest.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/UpdateCustomer/UpdateCustomerRequestValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/UpdateCustomer/UpdateCustomerResponse.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/CustomerRepositoryTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/ListPaginationTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/ListQueryExtensionsTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/RepositoryListQueryTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/TestDocuments.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/UnitOfWorkTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Customers/CreateCustomerHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Customers/CustomerCommandValidatorsTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Customers/CustomerProfilesTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Customers/UpdateCustomerHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/NotFoundHandlersTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/CustomerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Validation/CnpjValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Validation/CpfValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Validation/DocumentValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Customers/CustomerProfilesTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Customers/CustomerRequestValidatorsTests.cs` (novo)

## FEAT-016 — Logs do Serilog no MongoDB com TTL

- O Serilog passa a ser configurado pela seção `Serilog` do appsettings: níveis (EF Core e ASP.NET em Warning), console com template e filtro do `/health` via `Serilog.Expressions`. A seção `Logging`, ignorada pelo Serilog, saiu.
- Os logs também vão para uma coleção MongoDB com índice TTL. `ConnectionStrings:LogStorage` e `LogStorage:Database`/`Collection`/`ExpireAfter` são obrigatórios, e a falta de qualquer um derruba o startup com mensagem que nomeia a chave. Saíram o sink de arquivo e o desvio por `Debugger.IsAttached`.
- Corrigido o filtro do template, que descartava todo evento Warning, Error e Fatal.
- `UseSerilogRequestLogging` (antes do middleware de exceções, com `RequestId` e `RemoteIpAddress`) e `LoggingBehavior` (MediatR; Warning só com o tipo da exceção nas rejeições 4xx, Error com a exceção nos demais casos; nunca o conteúdo da requisição).
- `AuthenticateUserHandler` registra usuário inexistente, senha errada, usuário inativo e sucesso, sem e-mail nem senha.
- O `catch` do `Program.Main` relança a exceção: falhas de startup passam a aparecer e o processo sai com código diferente de 0.
- Revisão final: o destructurer de `DbUpdateException` gravava no Mongo os valores das entidades (e-mail, hash de senha, telefone) quando dois cadastros com o mesmo e-mail concorriam; a propriedade `Entries` agora é descartada. A mensagem de URL inválida do Mongo não repete mais o texto do driver, que em alguns casos trazia a senha. O `SelfLog` do Serilog passa a escrever no stderr, e com isso uma falha de gravação no Mongo aparece no console.
- Limitação aceita: se o MongoDB estiver fora do ar no primeiro envio, o sink só volta a gravar depois que a API reinicia; o console não é afetado.

FILES
- `backend/src/Ambev.DeveloperEvaluation.Common/Ambev.DeveloperEvaluation.Common.csproj` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Common/Logging/LogStorageSettings.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Common/Logging/LoggingExtension.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Common/LoggingBehavior.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Auth/AuthenticateUser/AuthenticateUserHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Program.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.json` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.Development.json` (alterado)
- `backend/docker-compose.yml` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Common/Logging/LogStorageSettingsTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/TestData/LoggerCalls.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Common/LoggingBehaviorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Auth/AuthenticateUserHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/PostgresFixture.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/MongoLogStorageFixture.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/LogStorageTests.cs` (novo)
- `CLAUDE.md` (alterado)

## FEAT-006 — Entrada assíncrona de vendas (Rebus + MongoDB)

`POST /api/sales` com o header `Prefer: respond-async` valida o corpo, gera o id da venda, envia o `CreateSaleCommand` pelo Rebus sobre o transporte MongoDB (banco `developer_evaluation_bus`) e responde `202` com o id, `Location` e `Preference-Applied`. Um handler Rebus na WebApi executa o mesmo comando pelo MediatR (mesmos `LoggingBehavior`, `TransactionBehavior` e `CreateSaleHandler`). O `CreateSaleHandler` devolve a venda já gravada quando o id informado existe, então uma reentrega não grava de novo. `ValidationException` vai direto para a fila de erro (fail fast, uma única tentativa). Sem o header, nada muda: `201` como antes e o comando sem id.

Por quê: sob milhares de inserções simultâneas o modo síncrono segura uma conexão do Postgres por requisição e esgota o pool do Npgsql (500 após 15 s). O modo assíncrono transforma o pico em fila e grava no ritmo que o banco sustenta, com a concorrência do consumidor configurada abaixo do pool (`Rebus:MaxParallelism`).

Também: volumes nomeados no compose (`postgres-data`, `mongo-data`) e o simulador de carga `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator` (laços concorrentes por perfil, modos sync/async, p50/p95/p99, vazão da execução inteira, tempo de esvaziamento da fila e contagem de requisições sem resposta).

Limitações registradas no spec (§9): a API não sobe sem MongoDB (o transporte cria o índice da fila na partida); o transporte reivindica uma mensagem no máximo 5 vezes e ela pode ficar parada fora da fila de erro; os retries não têm backoff.

Verificação: unitários 462/462, integração 44/44, e2e com Rebus + MongoDB + Postgres reais (201 síncrono; 202 com `Location`; venda gravada após a fila; cliente inexistente na fila de erro com 1 tentativa), simulador em escala pequena nos dois modos e revisão final independente.

FILES
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.csproj` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/PreferHeader.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/SaleAcceptedResponse.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/CreateSaleMessageHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/MessagingExtensions.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/MessagingSettings.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Program.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.json` (alterado)
- `backend/docker-compose.yml` (alterado)
- `backend/Ambev.DeveloperEvaluation.sln` (alterado)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/Ambev.DeveloperEvaluation.LoadSimulator.csproj` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/appsettings.json` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/ApiClient.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/Cpf.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/LoadReport.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/LoadRunner.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/Program.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/SimulatorSettings.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/SaleRepositoryTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSaleHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/PreferHeaderTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerCreateSaleTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Messaging/CreateSaleMessageHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Messaging/MessagingSettingsTests.cs` (novo)
- `CLAUDE.md` (alterado)

## TD-013 — Guia passo a passo (README_.md)

Criado `README_.md`, que vai substituir o README do repositório: pré-requisitos, subida dos contêineres com `make dev-up` (e rebuild da imagem da API), criação do schema com `dotnet ef database update`, as duas formas de rodar a API (contêiner na 8080 ou `dotnet run` na 5119, sempre uma instância só por causa da fila compartilhada), primeiras requisições de ponta a ponta com curl (usuário, login, cadastros, venda síncrona e assíncrona), referência de configuração (todas as chaves obrigatórias, override por variável de ambiente, pool do Npgsql e `Rebus:MaxParallelism`), a entrada assíncrona e a fila de erro, o teste de carga com o simulador (passo a passo, leitura do relatório, ressalvas), logs no MongoDB, testes automatizados, parar/reiniciar/zerar dados e solução de problemas.

Por quê: o README atual é o enunciado do desafio; faltava um guia de uso da plataforma depois do FEAT-006.

Verificado: a imagem da API compila com `docker compose build`; os passos de curl espelham o e2e do FEAT-006; âncoras e links relativos conferidos. Encontrado: o Swagger não tem esquema JWT configurado (registrado como TD-014 em backlog).

FILES
- `README_.md` (novo)

Trocar o README.md pelo README_.md: a decisão é sua = Nao vai trocar
Deleções de template/backend/ em stage: decida se vão para um commit próprio ou se devem ser revertidas com git restore
--staged template/. = Apagado definitivamente de dev
Backlog: TD-011 (esperar o MongoDB ficar pronto antes de subir a API), TD-012 (ignorar o Id explicitamente no mapeamento) e
TD-014 (esquema JWT no Swagger).=Mantenha em backlog
FEAT-004 (outbox): está em todo e deve reaproveitar o Rebus do FEAT-006. Antes de executar, o plano dele precisa ser
revisado com as emendas da §10 do spec do FEAT-006. = Siga
## TD-015 — Remoção da cópia original do template

Removido o diretório `template/backend/` (116 arquivos), a cópia intocada do template do desafio a partir da qual `backend/` foi criado. Nada no repositório o referencia; por decisão do responsável ele sai definitivamente de `dev`. O `CLAUDE.md` local deixou de mencioná-lo.

FILES
- `template/backend/` (removido, 116 arquivos)

## FEAT-004 — Eventos de venda via transactional outbox

Toda escrita de venda (criação, atualização e exclusão) grava seus eventos (`SaleCreated`, `SaleModified`, `SaleCancelled`, `ItemCancelled`, `SaleDeleted`) na tabela `OutboxMessages`, dentro da mesma transação aberta pelo `TransactionBehavior`: um rollback não deixa evento, um commit sempre deixa. O `OutboxWriter` recusa gravar sem transação aberta. Um relay (`OutboxRelayService` + `OutboxRelay`) lê as linhas pendentes em ordem de `Sequence` e as envia pelo Rebus, sobre o transporte MongoDB do FEAT-006, para a mesma fila `sales-intake`; o `SaleEventLogHandler` registra cada evento no log. A entrega é pelo menos uma vez, com o id da linha do outbox como message id. No caminho idempotente do FEAT-006 (id já gravado) nenhum evento é gravado.

Por quê: os eventos do README como diferencial, com garantia de que só existem para escritas confirmadas e saem na ordem em que foram gravados.

Mudança aprovada pelo responsável em relação ao spec: o relay começa o ciclo seguinte na hora quando o lote veio cheio e só espera `Outbox:PollingInterval` depois de lote parcial ou falha; sem isso o envio ficava limitado a 10 eventos/s com os valores padrão. Coberto por `OutboxRelayServiceTests` (com mutações do loop verificadas) e pelo e2e com `BatchSize=1`.

Da revisão final: `Outbox:PollingInterval` agora aceita só de 100 ms a 1 h (um número solto seria lido como dias, e acima de ~49,7 dias derrubaria a API); a `SaleDate` da venda criada é truncada em microssegundos, a precisão do PostgreSQL, para o `SaleCreated` bater com o que um `SaleModified` posterior lê.

Limitações registradas: a ordem de consumo não é garantida (consumidores em paralelo), um relay por instância, eventos reenviados viram segunda mensagem (at-least-once), linhas processadas não são limpas.

Verificação: unitários 495/495, integração 51/51, sem mudanças pendentes no modelo, e2e com Postgres + MongoDB + Rebus reais (RED com o loop do spec, GREEN com a regra do lote cheio; ordem do outbox, contagem dos eventos, `SaleCreated` único da venda enfileirada, message id = id da linha), simulador pequeno nos dois modos com o outbox esvaziado, revisão final independente.

FILES
- `README_.md` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Events/IEventPublisher.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Events/IIntegrationEvent.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Events/IntegrationEventTypes.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Events/Sales/ItemCancelled.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Events/Sales/SaleCancelled.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Events/Sales/SaleCreated.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Events/Sales/SaleDeleted.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Events/Sales/SaleModified.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Events/Sales/SaleSnapshot.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Events/Sales/SaleSnapshotItem.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/IOutbox.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Mapping/OutboxMessageConfiguration.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260925110004_AddOutboxMessages.Designer.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260925110004_AddOutboxMessages.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Outbox/IOutboxRelay.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Outbox/OutboxMessage.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Outbox/OutboxRelay.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Outbox/OutboxWriter.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/MessagingExtensions.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/MessagingSettings.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/OutboxRelayService.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/RebusEventPublisher.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/SaleEventLogHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.json` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/OutboxRelayTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/OutboxWriterTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/NotFoundHandlersTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSaleHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/DeleteSaleHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSaleHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Events/IntegrationEventTypesTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Events/SaleSnapshotTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Messaging/MessagingSettingsTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Messaging/OutboxRelayServiceTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Messaging/RebusEventPublisherTests.cs` (novo)
- `CLAUDE.md` (alterado)

## TD-005 — Limpeza do docker-compose.yml

Removidas do serviço Redis (`ambev.developerevaluation.cache`) as variáveis `MONGO_INITDB_ROOT_USERNAME`/`MONGO_INITDB_ROOT_PASSWORD`, copiadas por engano do serviço Mongo. Removido o atributo `version: '3.8'`, obsoleto no Compose v2 e que gerava um aviso em todo comando. Normalizadas a indentação dos serviços `nosql` e `cache` e os espaços sobrando no fim das linhas. Os volumes `${APPDATA}` citados no item já não existiam (o `docker-compose.override.yml` está vazio desde o commit inicial).

Verificação: a configuração resolvida por `docker compose config` antes e depois difere só nas duas variáveis removidas do Redis (serviços, nomes de container, portas e volumes idênticos) e não há mais avisos; o container do Redis foi recriado e responde `PONG`.

FILES
- `backend/docker-compose.yml` (alterado)

## TD-020 — Fluxo de branches documentado no README_.md

Adicionada a seção 15 ("Branching and pull requests") ao `README_.md`, com entrada no sumário, registrando que até o PR #11 cada mudança foi commitada direto em `dev` e promovida a `main` por um PR próprio (#3 a #11), e que a partir do PR #12 toda mudança nasce em `feature/<ITEM-ID>` ou `bugfix/<ITEM-ID>` e chega a `dev` por pull request, com `dev` indo a `main` só como release. Motivo: a revisão sênior apontou que o desvio de Git Flow no início do projeto fica visível para o avaliador; a nota o enquadra como evolução consciente do processo, sem reescrever histórico.

Na mesma sessão, `docs/evaluation-coverage.md` recebeu a correção do enquadramento de C16 (os 13 commits tiveram PR `main <- dev`, não ficaram sem PR) e os achados N5 a N10 da revisão (health checks constantes, relay do outbox sem tratamento de linha envenenada, ausência de token de concorrência em `Sale`, rejeição assíncrona invisível ao cliente, domínio anêmico e cancelamento por flag com hard delete), com referências cruzadas nas linhas C2, C9, C18, C22, R13 e R14.

FILES
- `README_.md` (alterado)


## TD-021 a TD-029 — Documentação das APIs em backend/docs

Criada a documentação das APIs em `backend/docs/`, que vai para o git junto com o código (ao contrário da `docs/` da raiz). `TEMPLATE.md` fixa a estrutura comum a todos os documentos: cabeçalho com work item e chave, Endpoints, Data model, um tópico por processo, Known limitations e See also. Também fixa as regras das chaves: tópico `API-ÁREA` e passo `API-ÁREA-NN`, nunca renumerados nem reaproveitados. As chaves aparecem nos rótulos dos diagramas mermaid, e o mesmo ponto do processo usa a mesma chave no fluxograma e no diagrama de sequência. Elas existem para um futuro console de trace com `#if DEBUG`, que vai imprimir cada chave no ponto de código que ela nomeia. Quando o ponto fica dentro de um framework (JWT, pipeline do Rebus), a chave nomeia o hook onde o trace entraria. Tópicos sem processo próprio (visão geral, catálogo de eventos, guia de operação da fila de erro) não têm chaves de passo. `INDEX.md` lista os documentos e registra os 39 tópicos com link para a seção.

`conventions.md` reúne o que é comum: autenticação e papéis, pipeline da request, transações, respostas e erros (incluindo os três formatos de erro), listagens e health checks. Auth, Users, Customers, Branches e Products têm um tópico por endpoint, cada um com fluxograma. `sales.md` documenta todos os processos internos de vendas com fluxogramas e diagramas de sequência:
- criação síncrona e via fila;
- consulta, listagem, atualização e exclusão;
- catálogo de eventos e escrita no outbox;
- loop do relay e ciclo de despacho;
- barramento Rebus sobre MongoDB, consumidor e fila de erro.

Os documentos descrevem o comportamento real, lido tópico a tópico no código, e registram as limitações conhecidas.

Verificação:
- Um verificador descartável no scratchpad, com 19 autotestes, conferiu estrutura, chaves, nomes de nós, caminhos em `Source:`, links e âncoras dos 9 documentos: 0 erros.
- O `mermaid-cli` 11 renderizou os 47 diagramas sem erro.
- Uma revisão final com contexto novo comparou os documentos com o código. Os achados corrigidos foram:
  - chaves sem ponto de código próprio;
  - a afirmação falsa de que linhas pendentes do outbox não são reprocessadas;
  - os dois formatos de 400 nas listagens;
  - caminhos de falha que faltavam no despacho e no relay;
  - a entrada do caminho da fila no SAL-CRT;
  - o 404 permanente de uma venda rejeitada pelo worker;
  - redações incorretas.

  Cada correção ganhou uma checagem que falhou antes e passou depois. A pedido do dono, também foram corrigidos os quatro ajustes menores: os campos de venda que aceitam faixa em SAL-LST-01, a ordem dos bullets de SAL-CON, a autenticação no diagrama de CMN-PIP e, no `README_.md` §8, as 5 tentativas de entrega no total (antes "retried 5 times").
- Nenhuma dessas ferramentas foi commitada.

O template de PR ganhou o item de checklist sobre `backend/docs/`. O `README_.md` aponta para `backend/docs/sales.md` (§8) e lista a pasta (§14). O `CLAUDE.md` registra que `backend/docs/` é versionada e pode ser linkada em PRs.

Na mesma sessão foram registrados no backlog:
- TD-019: remover o `UserRegisteredEvent`, que não é usado.
- BUG-012: os endpoints de users aceitam requests anônimas, e o cadastro deixa escolher qualquer papel, inclusive Admin.

FILES
- `backend/docs/TEMPLATE.md` (novo)
- `backend/docs/INDEX.md` (novo)
- `backend/docs/conventions.md` (novo)
- `backend/docs/auth.md` (novo)
- `backend/docs/users.md` (novo)
- `backend/docs/customers.md` (novo)
- `backend/docs/branches.md` (novo)
- `backend/docs/products.md` (novo)
- `backend/docs/sales.md` (novo)
- `.github/pull_request_template.md` (alterado)
- `README_.md` (alterado)
- `CLAUDE.md` (alterado, não versionado)

## BUG-012 — Endpoints de users sem autenticação e cadastro com qualquer papel

O `UsersController` não tinha `[Authorize]` e o app não define política padrão, então GET e DELETE `/api/users/{id}` rodavam sem token e o POST gravava o papel que viesse no corpo, inclusive Admin. Decisões do dono:
- o controller inteiro exige Admin ou Manager (`[Authorize(Roles = "Admin,Manager")]`, o mesmo padrão dos outros controllers); não existe mais cadastro anônimo;
- o primeiro Admin é semeado a partir da config `Seed:Admin:Username`/`Email`/`Password`/`Phone`, obrigatória como as demais (chave ausente derruba a inicialização com mensagem que a nomeia);
- como a API não migrava ao subir, ela passa a aplicar as migrations pendentes antes de escutar e só então roda o seed; assim um volume novo sobe sem passo manual;
- sem controle por papel: Admin e Manager têm os mesmos poderes, e um Manager pode criar ou apagar um Admin. Isso está registrado no `README_.md` §6 e nas limitações de `backend/docs/users.md`.

O `AdminSeeder` cria o Admin pelo `CreateUserCommand`, reaproveitando validação, hash e transação, e pula quando algum usuário já tem o e-mail configurado. O log leva o id do usuário, nunca o e-mail. Como a API agora depende do PostgreSQL pronto ao subir, o compose ganhou healthcheck do PostgreSQL (`pg_isready`), e a API espera por ele. O simulador de carga lê o `appsettings.json` da WebApi e loga como o Admin semeado, em vez de se cadastrar como Manager anônimo.

Verificação:
- Testes RED→GREEN: atributo de autorização do controller, leitura das configs do seed e o seeder (cria quando o e-mail está livre, não cria quando já existe).
- Smoke contra um banco descartável que ainda não existia, com fila, logs e porta isolados da instância em uso:
  - migração do zero e Admin semeado;
  - 13 verificações HTTP (401 anônimo, 403 Customer, 200/201 Admin e Manager);
  - reinício sem duplicar o Admin;
  - simulador com 6/6 vendas 201;
  - config ausente derrubando a inicialização.
  Tudo foi desmontado ao final.
- Suítes: Unit 508/508, Integration 51/51; verificador das docs com 0 erros e os diagramas renderizados.

`backend/docs` ganhou o tópico USR-SED (seed do administrador) e teve papéis e erros atualizados em users, CMN-AUT e INDEX. O `README_.md` mudou em §4 (schema e administrador automáticos), §6 (login como admin, sem cadastro anônimo, aviso de ausência de controle por papel), §7 (chaves `Seed:Admin`), §9 (login do simulador), §12 e §13. O `CLAUDE.md` registra as novas chaves e a dependência do PostgreSQL na inicialização.

FILES
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Users/UsersController.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Seeding/AdminSeedSettings.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Seeding/AdminSeeder.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Seeding/SeedingExtensions.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Program.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.json` (alterado)
- `backend/docker-compose.yml` (alterado)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/ApiClient.cs` (alterado)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/Program.cs` (alterado)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/SimulatorSettings.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Users/UsersControllerAuthorizationTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Seeding/AdminSeedSettingsTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Seeding/AdminSeederTests.cs` (novo)
- `backend/docs/users.md` (alterado)
- `backend/docs/conventions.md` (alterado)
- `backend/docs/INDEX.md` (alterado)
- `README_.md` (alterado)
- `CLAUDE.md` (alterado, não versionado)

## FEAT-017 — Console de trace e trace por passo

Cada chave de passo documentada em `backend/docs/` agora tem uma chamada `StepTrace.Step` na linha de código que ela nomeia. Um console de desenvolvedor hospeda a API no próprio processo e imprime um passo por linha: hora, thread, chave (as duas, quando a linha é compartilhada), título, os valores que decidiram o caminho, arquivo e linha. Exemplo: `17:48:57.538726  T022  SAL-CRT-04 CMN-PIP-10  Validate the command  presetId=null valid=True errors=0  CreateSaleHandler.cs:74`.

O que foi implementado:
- `Common/Tracing`: o helper `StepTrace` (métodos `[Conditional("DEBUG")]`, silencioso sem sink), `StepEvent`, `SharedPoint` e a tabela `StepKeys`. Em Release as chamadas somem na compilação.
- 286 chamadas `StepTrace.Step` em `backend/src`, cobrindo as 234 chaves de passo documentadas: controllers, handlers, behaviors do MediatR, repositórios, outbox, relay, seed e health checks.
- Hooks só de trace, registrados sob `#if DEBUG`:
  - WebApi: middleware, action filter, result filter, os passos de entrada e saída do pipeline do Rebus e o decorator do error handler do Rebus;
  - ORM: um interceptor de comandos do EF Core;
  - os eventos do JWT bearer em `AuthenticationExtension`.
- `SaleEventIds` no Domain extrai o id da venda de qualquer evento de venda, para as linhas do barramento.
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole` substitui o `LoadSimulator`, com dois comandos:
  - `l` é o simulador de carga, movido sem mudança de comportamento;
  - `t` é o console de trace, com onze cenários mais `all`: `conventions`, `auth`, `users`, `customers`, `branches`, `products`, `sale-create`, `sale-async`, `sale-update`, `sale-delete`, `sale-list`.
- O comando `t`:
  - pede confirmação (ou `--yes`);
  - apaga o banco PostgreSQL de `ConnectionStrings:DefaultConnection` e o banco MongoDB da fila de `ConnectionStrings:MessageBus`, sem tocar no banco de logs;
  - hospeda a API com `WebApplicationFactory`, faz login como o Admin semeado e roda os cenários;
  - espera os passos assíncronos (worker, relay, consumidor) pelos valores `saleId`, `messageId` e `rowId`;
  - termina com as chaves vistas e, em `all`, as chaves documentadas não exercitadas.
- Configurações novas do console: `Trace:AppLogMinimumLevel`, `Trace:WaitTimeout` e `Trace:MaintenanceDatabase`, todas obrigatórias e sem fallback.
- O teste `StepKeyCoverageTests` falha quando uma chave documentada não tem chamada ou quando uma chamada usa chave não documentada. `StepKeysTests` confere a tabela contra os tipos de comando da Application.

Por quê: depurar um fluxo sem IDE e ver numa só janela a request, o worker do Rebus, o relay do outbox e o consumidor de eventos, com a mesma chave usada nos diagramas de `backend/docs`.

Decisões registradas ao longo do trabalho (cada uma com o custo se estiver errada):
- `StepKeys` compila também em Release (tabela de dados, sem comportamento) e é permitida no check de Release. Custo: uma classe de dados a mais em Release.
- O teste de cobertura conta só chaves entre aspas (`"XXX-YYY-NN"`), então comentário XML nunca mascara uma chamada ausente. Custo: uma chave escrita em forma incomum (string interpolada) aparece como ausente, que é a direção segura.
- CMN-PIP-06 (envio ao MediatR) sai numa linha só, logo depois que o `Send` retorna; SAL-CRT-03 é a exceção documentada, antes do envio. Custo: o leitor vê PIP-06 depois do handler; dá para acrescentar linhas avulsas depois.
- Task 4 (spike): a API inteira roda sob `WebApplicationFactory`, com migrations, seed, bus do Rebus e relay. A raiz de conteúdo padrão falhou com `DirectoryNotFoundException`, porque o fallback do factory é a pasta da solução mais o nome do assembly. `TraceHost` usa `UseSolutionRelativeContentRoot("src/Ambev.DeveloperEvaluation.WebApi")`, que sobe a árvore até a `.sln` e não depende do diretório atual. Custo: nenhum (API documentada do factory).
- CMN-AUT-06 lê `HttpContext.User`: o `JwtBearerHandler` nunca preenche `ForbiddenContext.Principal`, e a linha do 403 saía com `userId=null`. Há teste pelas `JwtBearerOptions` registradas.
- A linha de catch de CMN-PIP-13 não imprime o erro, porque CMN-RSP-05 (`handled=false`) já imprime o tipo da exceção no mesmo caminho de 500. Custo: nenhum.
- Os títulos seguem o rótulo do diagrama (por exemplo CMN-PIP-05 "AutoMapper maps the request to a command").
- Todo id de venda nas linhas SAL-* se chama `saleId`, porque o `StepWaiter` casa por esse nome. Custo: nenhum.
- G13 (o código ganha):
  - `DeleteSaleHandler` mantém o retorno real `DeleteSaleResult`;
  - em 401/403, CMN-AUT-04 não imprime, porque a autorização encerra a request antes do MVC.
- Rebus 8.9.4: assinaturas conferidas no XML do pacote e no `Rebus.dll` descompilado:
  - `PipelineStepInjector.OnSend`/`OnReceive`;
  - `Decorate<IErrorHandler>`;
  - `IncomingStepContext`/`OutgoingStepContext` com `DestinationAddresses`;
  - `ExceptionInfo` como record;
  - `RetryStrategySettings.MaxDeliveryAttempts`.

  Ajustes:
  - o `messageId` é lido com `Headers.GetValueOrDefault`, porque `GetMessageId()` lança sem o header e quebraria o dead-letter de uma mensagem sem id;
  - guardas de null extras garantem que um erro só de trace nunca vire retry.

  `attempt`/`final` pressupõem o modo `ErrorHandlerMode.Immediately` (padrão) e o error tracker em memória. Custo: se o modo mudar, essas linhas enganam.
- Execuções ao vivo só em bancos descartáveis (`trace_console_check` e `trace_console_check_bus`), nunca nos de desenvolvimento, porque apagar banco é decisão do dono. O `TraceCommand` repassa à API hospedada todos os pares `--Chave=valor` da linha de comando. Custo: nenhum. A execução contra os bancos de desenvolvimento fica com o dono.
- `EnsureDeleted`: o `EnsureDeletedAsync` do Npgsql EF Core roda `pg_terminate_backend` em toda sessão antes do drop (visto ao vivo: matou uma sessão `pg_sleep`). Isso forçaria o drop e derrubaria as conexões de uma API rodando. A limpeza agora abre uma `NpgsqlConnection` ao banco de manutenção (`Pooling = false`) e roda `DROP DATABASE IF EXISTS` sem `FORCE`. Com o banco em uso, o PostgreSQL responde 55006, e o console imprime o comando para parar o serviço `ambev.developerevaluation.webapi` e sai com 1. Custo: nenhum para a segurança.
- O nome do banco de manutenção veio para a config (`Trace:MaintenanceDatabase`) em vez da constante `"postgres"`.
- Configurações do trace obrigatórias, sem fallback. Falha ao subir o host ou no login vira uma linha e saída 1.
- O console lê as mesmas fontes da API, na ordem da API: appsettings, appsettings.Development e user secrets da WebApi, depois variáveis de ambiente. Em seguida vêm o próprio `appsettings.json` e a linha de comando. Ele repassa ao host as connection strings e as credenciais `Seed:Admin` resolvidas, para que a limpeza, o login e a API usem sempre os mesmos bancos e o mesmo admin.
- Corpos impressos mascaram `password`, `token` e, numa senha rejeitada, `attemptedValue` e `formattedMessagePlaceholderValues` como `***`. Um corpo que parece JSON (começa com `{` ou `[`) mas não é lido, inclusive com chave duplicada, sai como `[unparsed body]`.
- Um cenário que falha imprime `!!! scenario <nome> failed: ...`, a execução segue e termina com saída 1.
- Aceitos na implementação:
  - enums enviados como número (a API não tem conversor de enum para string);
  - sufixo aleatório nos nomes de clientes e filiais e nos códigos de produto, para que cenários repetidos não colidam (409).
- Faltas esperadas no `all` (`ScenarioCatalog.ExpectedMisses`), registradas no TD-030 (backlog); nenhuma outra chave ficou sem exercício:
  - SAL-CRT-06: comando da fila entregue de novo;
  - SAL-OBW-02: escrita no outbox fora de transação;
  - SAL-RLY-07: ciclo do relay com falha;
  - SAL-DSP-07: despacho com falha;
  - SAL-BUS-06: entrega repetida (só falhas de validação são provocadas, e elas falham rápido);
  - USR-SED-06: seed pulado (o banco é apagado, então o admin sempre é criado).
- Itens que ficam para depois (decisão da revisão final; custo se estiver errado: pequeno):
  - `TransactionBehavior` importa tipos de venda só para as chaves de commit;
  - o `appsettings.json` do console sobrescreve o de mesmo nome na pasta de build do projeto Unit;
  - variáveis de ambiente não sobrescrevem o `appsettings.json` do próprio console (só a linha de comando);
  - SAL-GET-01, SAL-DEL-01 e SAL-LST-02 registram só `valid`, como nas tabelas, enquanto os equivalentes dos cadastros também registram `errors`; CMN-TXN-05 diz `threw=True` quando quem lança é o commit;
  - casos de borda do barramento: `OperationCanceledException` no desligamento imprime SAL-ASY-08/SAL-BUS-06 embora o Rebus só devolva a mensagem; `handled` é marcado quando `next()` retorna; SAL-DSP-03 lança em Debug para um payload JSON `null` (mesmo caminho de catch);
  - lacunas de teste nos hooks (`modelValid`/`willRun`, 415, bordas do `FirstLine` do interceptor, caminhos assíncronos) e detalhes de estilo (doc do `StepKeys.Entry`, namespace em bloco de `AuthenticationExtension`, `RegistryScenario.CreateBody` público só para teste, `HostReady` não volátil).

Revisão final (branch inteira): nenhum item crítico e dois importantes. (1) `StepTrace.Format` mantinha as quebras de linha das mensagens do FluentValidation, e SAL-ASY-06, SAL-ASY-08 e SAL-BUS-05 saíam em duas linhas. (2) SAL-BUS-07 imprimia o header bruto do Rebus (tipo com assembly) e o wrapper `System.AggregateException, mscorlib`. A última rodada corrigiu os dois e os menores marcados para antes do PR:
- `Format` troca quebras de linha por espaço em strings e exceções e formata dentro do `try`, então um `ToString()` que lança descarta o evento. Um enumerável que não é `ICollection` imprime o nome do tipo e nunca é enumerado, então um `IQueryable` não roda;
- SAL-BUS-07 imprime nomes simples (`eventType=CreateSaleCommand error=AggregateException`). O tipo interno não fica acessível de forma estruturada: o retry step do Rebus sempre monta o wrapper, e os tipos internos existem só como texto livre em `Details`. SAL-BUS-05 e SAL-BUS-06 já imprimem o tipo real;
- os passos do Rebus, o error handler e o interceptor retornam na hora com o sink null (spec §5.3);
- `message.Items?.Count` em SAL-ASY-05, e `SaleEventIds` devolve null para um `Sale` null;
- `Redact` mascara também `formattedMessagePlaceholderValues` e troca o corpo ilegível por `[unparsed body]`;
- `SaleAsyncScenario` imprime `!!!` quando SAL-BUS-01 não foi visto; `HeldMessage` e a mensagem de limpeza parcial vão para stderr;
- README §9: a execução continua gravando o log da API no banco de logs, e o comando interativo cita o prompt `Continue? (y/n)`.

Verificação da revisão final: build Debug e Release sem erros, Unit 610/610, Integration 51/51, e `t all` em bancos descartáveis com saída 0, `===== no unexpected misses`, cada evento numa linha e nenhuma senha ou token na saída. Os bancos descartáveis foram apagados.

Verificação:
- Suítes: Unit 610/610 (102 testes novos; eram 508), com os 8 casos de `StepKeyCoverageTests` verdes, e Integration 51/51.
- Execuções ao vivo, sempre em bancos descartáveis:
  - cada cenário com os status esperados e sem segredos na saída;
  - `t all` terminou com `===== no unexpected misses` (228 chaves vistas, 6 faltas esperadas);
  - banco em uso: saída 1 e sessão intacta;
  - config ausente, PostgreSQL inacessível e `Jwt:SecretKey` vazio: uma linha e saída 1.

  Os bancos descartáveis foram apagados ao final.
- Check de Release:
  - `dotnet build -c Release` sem erros;
  - um verificador descartável, fora do repositório, lê os metadados dos assemblies: WebApi 0 tipos `.Tracing`, ORM 0, Common só `StepTrace`, `StepEvent`, `SharedPoint`, `StepKeys` e `StepKeys+Entry`;
  - a string `StepTrace` não aparece nos dlls Release de Application, WebApi, ORM e Domain;
  - `t all --yes` em Release recusa com `The trace command needs a Debug build: ...` e saída 1, sem apagar nada.

`backend/docs`: `TEMPLATE.md` (regra das chaves com o console e o teste de cobertura) e `INDEX.md` (introdução e link para o console). O `README_.md` ganhou a §9 "Trace console". As seções seguintes foram renumeradas de §10 a §16, com as âncoras. O simulador agora é o comando `l` do console, e a árvore do repositório foi atualizada. O `CLAUDE.md` ganhou os comandos `l` e `t` e a convenção de chamada por chave documentada.

FILES
- `backend/src/Ambev.DeveloperEvaluation.Application/Auth/AuthenticateUser/AuthenticateUserHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/CreateBranch/CreateBranchHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/DeleteBranch/DeleteBranchHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/GetBranch/GetBranchHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/ListBranches/ListBranchesHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Branches/UpdateBranch/UpdateBranchHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Common/LoggingBehavior.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Common/TransactionBehavior.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/CreateCustomer/CreateCustomerHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/DeleteCustomer/DeleteCustomerHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/GetCustomer/GetCustomerHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/ListCustomers/ListCustomersHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Customers/UpdateCustomer/UpdateCustomerHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/CreateProduct/CreateProductHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/DeleteProduct/DeleteProductHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/GetProduct/GetProductHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/ListProducts/ListProductsHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Products/UpdateProduct/UpdateProductHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/GetSale/GetSaleHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Users/DeleteUser/DeleteUserHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Users/GetUser/GetUserHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Common/Ambev.DeveloperEvaluation.Common.csproj` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Common/HealthChecks/HealthChecksExtension.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Common/Security/AuthenticationExtension.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Common/Security/JwtTokenGenerator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Common/Tracing/StepEvent.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Common/Tracing/StepKeys.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Common/Tracing/StepTrace.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Common/Validation/ValidationBehavior.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Events/Sales/SaleEventIds.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Outbox/OutboxRelay.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Outbox/OutboxWriter.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/BranchRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/CustomerRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/ProductRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Tracing/StepTraceCommandInterceptor.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/ListQueryParser.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Auth/AuthController.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/BranchesController.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CustomersController.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ProductsController.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Users/UsersController.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/CreateSaleMessageHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/MessagingExtensions.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/OutboxRelayService.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/RebusEventPublisher.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/SaleEventLogHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Middleware/ValidationExceptionMiddleware.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Program.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Seeding/AdminSeedSettings.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Seeding/AdminSeeder.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Seeding/SeedingExtensions.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Tracing/StepTraceActionFilter.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Tracing/StepTraceErrorHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Tracing/StepTraceIncomingStep.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Tracing/StepTraceMiddleware.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Tracing/StepTraceOutgoingStep.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Tracing/StepTraceResultFilter.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Ambev.DeveloperEvaluation.Unit.csproj` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Common/Security/AuthenticationExtensionTraceTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Common/Tracing/StepKeyCoverageTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Common/Tracing/StepKeysTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Common/Tracing/StepTraceCollection.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Common/Tracing/StepTraceTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/DevConsole/CommandLineTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/DevConsole/ConsoleSinkTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/DevConsole/DatabaseWipeTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/DevConsole/ScenarioCatalogTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/DevConsole/ScenarioContextTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/DevConsole/StepWaiterTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/DevConsole/TraceCommandTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Events/SaleEventIdsTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/ORM/Tracing/StepTraceCommandInterceptorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Tracing/StepTraceActionFilterTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Tracing/StepTraceErrorHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Tracing/StepTraceIncomingStepTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Tracing/StepTraceMiddlewareTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Tracing/StepTraceOutgoingStepTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Tracing/StepTraceResultFilterTests.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Ambev.DeveloperEvaluation.DevConsole.csproj` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/CommandLine.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/ConsoleConfiguration.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Cpf.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Load/ApiClient.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Load/LoadCommand.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Load/LoadReport.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Load/LoadRunner.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Load/SimulatorSettings.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Program.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/ConsoleSink.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/DatabaseWipe.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/IScenario.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/ScenarioCatalog.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/ScenarioContext.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/AuthScenario.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/ConventionsScenario.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/RegistryScenario.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/SaleAsyncScenario.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/SaleCreateScenario.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/SaleDeleteScenario.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/SaleFixtures.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/SaleListScenario.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/SaleUpdateScenario.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/UsersScenario.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/StepWaiter.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/TraceCommand.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/TraceHost.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/appsettings.json` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/Ambev.DeveloperEvaluation.LoadSimulator.csproj` (removido)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/ApiClient.cs` (removido)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/Cpf.cs` (removido)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/LoadReport.cs` (removido)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/LoadRunner.cs` (removido)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/Program.cs` (removido)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/SimulatorSettings.cs` (removido)
- `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/appsettings.json` (removido)
- `backend/docs/INDEX.md` (alterado)
- `backend/docs/TEMPLATE.md` (alterado)
- `backend/Ambev.DeveloperEvaluation.sln` (alterado)
- `README_.md` (alterado)
- `CLAUDE.md` (alterado, não versionado)

## TD-031 — README: conferir uma execução do trace e apagar os bancos descartáveis

O §9 (Trace console) do `README_.md` mostrava o comando com bancos descartáveis, mas não dizia como saber se a execução deu certo nem como remover os bancos depois. Agora o comando redireciona a saída para `trace.txt` e imprime o código de saída, seguido de `tail -3` e de `grep '^!!!'`. Uma frase define a execução boa: `exit 0`, última linha `===== no unexpected misses` e nenhuma linha `!!!`. Em seguida vêm os dois comandos que apagam `trace_scratch` (PostgreSQL) e `trace_scratch_bus` (MongoDB), mais o `rm trace.txt`.

Verificação: os dois comandos de limpeza rodaram como escritos contra os nomes `trace_scratch` e `trace_scratch_bus` (inexistentes), com saída 0 e sem tocar nos bancos de desenvolvimento.

FILES
- `README_.md` (alterado)

## FEAT-001 — Matriz de políticas de desconto (caminho B)

As regras de desconto do README deixam de estar fixas no código: cada item de venda é precificado pela política de desconto em vigor na data da venda, e a política fica registrada no próprio item.

O que foi implementado:
- Políticas de desconto append-only (`DiscountPolicy` com `DiscountTier`): escopo por produto, por filial, pelos dois ou padrão (nenhum); vigência `[ValidFrom, ValidTo)`; máximo de unidades por produto; faixas de quantidade, cada uma com o seu teto de percentual. Uma mudança de regra é uma política nova, nunca uma edição.
- O resolvedor (`DiscountPolicyResolver`): escopo mais específico, depois `ValidFrom` mais recente, depois `CreatedAt` mais recente (e o id como desempate final).
- Migração `20260925233259_AddDiscountPolicies`: tabelas `DiscountPolicies` e `DiscountTiers`, com checks e índice, e a semente da política padrão com as regras do README (4 a 9 unidades 10%, 10 a 20 unidades 20%, no máximo 20 por produto, nenhum desconto abaixo de 4).
- Migração `20260925234012_AddSaleItemDiscountSnapshot`: colunas `RequestedDiscountPercentage`, `DiscountPolicyId` e `DiscountCeilingPercentage` em `SaleItems`; os itens existentes apontam para a política padrão, com o percentual gravado como teto.
- `Sale.ApplyDiscounts` calcula pelo total de cada produto somado entre as linhas da venda e grava o snapshot em cada item: política, teto, percentual aplicado e os valores calculados (`DiscountAmount`, `TotalAmount` do item e da venda). Os valores calculados não são mais lidos do corpo da requisição.
- Os três códigos de erro 400 nos handlers de venda (`QuantityLimitExceeded`, `DiscountAboveAllowed`, `NoDiscountPolicy`), na linha que causou a violação, pelo `ValidationExceptionMiddleware` existente.
- Os três endpoints de políticas: `POST /api/discount-policies` (Admin, Manager), `GET /api/discount-policies/{id}` e `GET /api/discount-policies` (qualquer usuário autenticado).
- O documento `backend/docs/discount-policies.md` (DSC-CRT, DSC-GET, DSC-LST), os passos SAL-CRT-14..16 e SAL-UPD-18..20 em `backend/docs/sales.md`, cada chave com a sua chamada `StepTrace.Step`.
- Os cenários `discount-policy` e `sale-discount` do console de trace.

Por quê: as regras de desconto do README passam a ser dados gerenciados pela API, rastreáveis em cada item, sem reescrever o middleware (emendas A13 a A22).

Decisões tomadas durante a execução:
- `ListQueryParser` passou a aceitar campos `Guid?`, e a lista de políticas usa `DiscountPolicyListFields` como tipo de campos (não é uma resposta; A11 mantido).
- Um item enviado já cancelado recebe a política do produto sem desconto, e um item cancelado já precificado mantém o snapshot. Na alteração, uma linha existente enviada como cancelada leva só o flag: o handler usa o produto gravado para a checagem de desconto e para o evento `ItemCancelled`.
- `NoDiscountPolicy` é verificado também em linhas canceladas, porque todo item gravado aponta para uma política.
- O clamp de `ApplyDiscounts` só é alcançável em teste de domínio: os handlers rejeitam antes, com 400.
- Os 400 dos request validators das rotas novas mantêm o array cru do FluentValidation, como todos os controllers do FEAT-010 (A12); a unificação ficou no TD-034.
- Nenhum passo documentado precisou mudar de posição pela regra G13: toda chave nova tem a sua chamada na linha que o documento descreve.
- Emenda A23 (revisão final, 2026-09-25): as respostas expõem `requestedDiscountPercentage` por item, e as quantidades são somadas como inteiros de 64 bits, então um total acima de `int.MaxValue` é `QuantityLimitExceeded`, sem overflow.
- A rodada de correções da revisão final adicionou o teste de desserialização do payload antigo do outbox, o código de erro `ValidToNotAfterValidFrom` na regra de `ValidTo`, o desempate pelo id no texto de SAL-CRT-14 e DSC-CRT-04, o item de compatibilidade retroativa em SAL-CRT-01, a asserção da troca de filial na alteração e a correção de comentários desatualizados.

Itens que ficam para depois (backlog, pai FEAT-001): TD-032 (editar uma política), TD-034 (corpo único para os 400 dos request validators), TD-035 (defaults das colunas do snapshot e limite dos valores calculados), TD-036 (helper do bloco de criar Customer e fazer login no console), TD-037 (ajustes de testes e docs da revisão). TD-033 (colunas de auditoria) continua no backlog, sem pai.

Verificação:
- Unit 740/740 (130 testes novos; eram 610) e Integration 58/58 (7 novos: `DiscountPolicyRepositoryTests` com 6 e `SaleItemDiscountSnapshotMigrationTests` com 1; eram 51).
- `dotnet ef migrations has-pending-model-changes`: nenhuma mudança pendente.
- `t all --yes` em bancos descartáveis (`trace_scratch` e `trace_scratch_bus`), rodado de novo depois da rodada de correções: saída 0, última linha `===== no unexpected misses` (247 chaves vistas, 6 faltas esperadas), nenhuma linha `!!!`, e as 19 chaves novas vistas.

FILES
- `README_.md` (alterado)
- `backend/docs/INDEX.md` (alterado)
- `backend/docs/conventions.md` (alterado)
- `backend/docs/sales.md` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/Common/SaleItemResult.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/Common/SaleResult.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleItemInput.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesResult.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleItemInput.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Common/Tracing/StepKeys.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Events/Sales/SaleSnapshot.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Events/Sales/SaleSnapshotItem.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Validation/SaleItemValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/ApplicationModuleInitializer.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/DefaultContext.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleItemConfiguration.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/ListQueryParser.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/Common/SaleItemResponse.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/Common/SaleResponse.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleItemRequest.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequest.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequestValidator.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/ListSales/ListSalesResponse.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/UpdateSaleItemRequest.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/UpdateSaleRequest.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/UpdateSale/UpdateSaleRequestValidator.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/OutboxWriterTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/PostgresFixture.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSaleHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSaleValidatorTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleProfileTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSaleHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSaleValidatorTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Common/Tracing/StepKeyCoverageTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Common/Tracing/StepKeysTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/DevConsole/ScenarioCatalogTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Events/SaleSnapshotTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/ListQueryParserTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/ListResponseFieldsTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/ListRequestValidatorsTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/CreateSaleRequestValidatorTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerCreateSaleTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/UpdateSaleRequestValidatorTests.cs` (alterado)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Load/ApiClient.cs` (alterado)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/ScenarioCatalog.cs` (alterado)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/SaleCreateScenario.cs` (alterado)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/SaleFixtures.cs` (alterado)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/SaleUpdateScenario.cs` (alterado)
- `backend/docs/discount-policies.md` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/Common/DiscountPolicyProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/Common/DiscountPolicyResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/Common/DiscountTierInput.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/Common/DiscountTierResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/CreateDiscountPolicy/CreateDiscountPolicyCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/CreateDiscountPolicy/CreateDiscountPolicyHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/CreateDiscountPolicy/CreateDiscountPolicyValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/GetDiscountPolicy/GetDiscountPolicyCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/GetDiscountPolicy/GetDiscountPolicyHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/GetDiscountPolicy/GetDiscountPolicyValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/ListDiscountPolicies/ListDiscountPoliciesCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/ListDiscountPolicies/ListDiscountPoliciesHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/ListDiscountPolicies/ListDiscountPoliciesResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/ListDiscountPolicies/ListDiscountPoliciesValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/Common/SaleDiscountLine.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/Sales/Common/SaleDiscountRules.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Entities/DiscountPolicy.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/IDiscountPolicyRepository.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Services/DiscountPolicyResolver.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/ValueObjects/DiscountDecision.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Domain/ValueObjects/DiscountTier.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Mapping/DiscountPolicyConfiguration.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260925233259_AddDiscountPolicies.Designer.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260925233259_AddDiscountPolicies.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260925234012_AddSaleItemDiscountSnapshot.Designer.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260925234012_AddSaleItemDiscountSnapshot.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/DiscountPolicyRepository.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/CreateDiscountPolicy/CreateDiscountPolicyProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/CreateDiscountPolicy/CreateDiscountPolicyRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/CreateDiscountPolicy/CreateDiscountPolicyRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/CreateDiscountPolicy/DiscountTierRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/DiscountPoliciesController.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/GetDiscountPolicy/GetDiscountPolicyProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/GetDiscountPolicy/GetDiscountPolicyRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/GetDiscountPolicy/GetDiscountPolicyRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/ListDiscountPolicies/DiscountPolicyListFields.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/ListDiscountPolicies/ListDiscountPoliciesProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/ListDiscountPolicies/ListDiscountPoliciesRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/ListDiscountPolicies/ListDiscountPoliciesRequestValidator.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/DiscountPolicyRepositoryTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/SaleItemDiscountSnapshotMigrationTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/DiscountPolicies/CreateDiscountPolicyHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/DiscountPolicies/CreateDiscountPolicyValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/DiscountPolicies/GetDiscountPolicyHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/DiscountPolicies/ListDiscountPoliciesHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleDiscountRulesTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/DiscountPolicyTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleDiscountTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/TestData/DiscountPolicyTestData.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Events/SaleEventPayloadTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Services/DiscountPolicyResolverTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/ValueObjects/DiscountTierTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/DiscountPolicies/CreateDiscountPolicyRequestValidatorTests.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/DiscountPolicyScenario.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/SaleDiscountScenario.cs` (novo)

## TD-032 — Desabilitar políticas de desconto

**O que foi implementado.** A única operação de escrita depois da criação de uma política: `POST /api/discount-policies/disable` (Admin e Manager) recebe `{ "ids": [...] }` e marca cada política com `DisabledAt` (UTC, nulo = ativa). É tudo ou nada: um id inexistente responde 404 nomeando os ids que faltam e nada é gravado; todas recebem o mesmo instante, truncado a microssegundos; desabilitar de novo não muda nada. `IsInEffectAt` e `GetApplicableAsync` passam a exigir `DisabledAt` nulo, então uma política desabilitada sai da resolução para vendas novas e para o recálculo de vendas antigas, sem alterar os snapshots já gravados. `GET /{id}` e a listagem mostram `disabledAt`; a listagem esconde as desabilitadas salvo `includeDisabled=true` (parâmetro dedicado, porque o parser de filtros não expressa "é nulo"; valor inválido responde 400). Migração `AddDiscountPolicyDisabledAt` com uma coluna nula. Tópico `DSC-DIS` em `backend/docs/discount-policies.md` com seis chaves rastreadas, `INDEX.md`, notas em `sales.md` (SAL-CRT-14 e SAL-UPD-18), e os cenários `discount-policy` (404, 403, 200, listagem sem e com `includeDisabled`, 400 em valor inválido) e `sale-discount` (desabilitar a política do produto e ver a venda seguinte voltar ao desconto padrão) do console.

**Por quê.** A TD-032 previa edição; o dono decidiu que basta desabilitar, sem reversão, o que preserva o modelo somente-acréscimo da FEAT-001 (emenda A24). Desabilitar a política padrão sem substituta faz as vendas responderem 400 `NoDiscountPolicy`, que antes era inalcançável.

**Decisões tomadas durante a execução.** `UpdateAsync` do repositório marca como modificada só a linha da política quando ela foi lida sem rastreamento, porque `Update` na entidade inteira falha pela chave sombra das faixas; as faixas nunca são reescritas. `includeDisabled` é removido da query antes do `ListQueryParser` para não virar filtro de campo. O console não desabilita a política padrão, porque os cenários de venda seguintes dependem dela. Os pontos menores da revisão (disparo concorrente sobrescrevendo o primeiro instante, parsing de `includeDisabled` sem teste, erros não combinados, dado de teste incomum) foram para a TD-037.

**Testes.** Unitários 760/760 (20 novos); integração 61/61 (4 novos); `t all --yes` em bancos descartáveis: exit 0, `===== no unexpected misses`, as seis chaves `DSC-DIS` vistas. Build sem aviso novo e sem mudança de modelo pendente.

**Ambiente.** O Docker Desktop foi removido durante a sessão; o compose passou a rodar no contexto `orbstack`, com volumes novos.

FILES
- `backend/docs/INDEX.md` (alterado)
- `backend/docs/discount-policies.md` (alterado)
- `backend/docs/sales.md` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/Common/DiscountPolicyResult.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/CreateDiscountPolicy/CreateDiscountPolicyCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/DisableDiscountPolicies/DisableDiscountPoliciesCommand.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/DisableDiscountPolicies/DisableDiscountPoliciesHandler.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/DisableDiscountPolicies/DisableDiscountPoliciesResult.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/DisableDiscountPolicies/DisableDiscountPoliciesValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/ListDiscountPolicies/ListDiscountPoliciesCommand.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/ListDiscountPolicies/ListDiscountPoliciesHandler.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Common/Tracing/StepKeys.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Entities/DiscountPolicy.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/IDiscountPolicyRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Mapping/DiscountPolicyConfiguration.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260926132350_AddDiscountPolicyDisabledAt.Designer.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/20260926132350_AddDiscountPolicyDisabledAt.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Migrations/DefaultContextModelSnapshot.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/DiscountPolicyRepository.cs` (alterado)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/DisableDiscountPolicies/DisableDiscountPoliciesProfile.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/DisableDiscountPolicies/DisableDiscountPoliciesRequest.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/DisableDiscountPolicies/DisableDiscountPoliciesRequestValidator.cs` (novo)
- `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/DiscountPoliciesController.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Integration/DiscountPolicyRepositoryTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Common/TransactionalCommandsTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/DiscountPolicies/DisableDiscountPoliciesHandlerTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/DiscountPolicies/DisableDiscountPoliciesValidatorTests.cs` (novo)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Application/DiscountPolicies/ListDiscountPoliciesHandlerTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Common/Tracing/StepKeysTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/DiscountPolicyTests.cs` (alterado)
- `backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/DiscountPolicies/DisableDiscountPoliciesRequestValidatorTests.cs` (novo)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/DiscountPolicyScenario.cs` (alterado)
- `backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/SaleDiscountScenario.cs` (alterado)

## FEAT-018 — Corpo de erro do general-api

Toda resposta 4xx ou 5xx da API passou a ter o corpo `{ type, error, detail }` do `.doc/general-api.md`: `type` é a categoria (`ValidationError`, `ResourceNotFound`, `AuthenticationError`, `AuthorizationError`, `MethodNotAllowed`, `DuplicateEntry`, `UnsupportedMediaType`, `ServerError`, `HttpError`), `error` é o código da primeira falha (o `ErrorCode` do FluentValidation na validação, `InvalidBody` no model state, o próprio `type` nos demais) e `detail` é uma string com um array JSON de mensagens, uma por falha, nunca vazio. O envelope de sucesso `{ success, message, data }` não mudou. Fecha a linha A7 da avaliação (N11) e absorve a TD-034.

Um único tipo, `ErrorResponse` em `WebApi/Common`, monta o corpo; quatro produtores o usam e nenhum handler ou controller constrói erro:

- `ValidationExceptionMiddleware`: os quatro `catch` existentes (400, 404, 401 do login, 409) passaram ao corpo novo, e o quinto deixou de relançar: registra a exceção no log (`Error`, método e caminho) e responde 500 `ServerError` com texto fixo, sem mensagem nem stack. Se a resposta já começou, relança.
- `BaseController.BadRequest(ValidationResult)`: novo helper para os validadores de request; os 28 `BadRequest(validationResult.Errors)` dos sete controllers viraram `BadRequest(validationResult)`, e os 45 atributos `ProducesResponseType(typeof(ApiResponse), 4xx)` viraram `typeof(ErrorResponse)`. `BadRequest(string)` e `NotFound(string)` também produzem o corpo novo.
- `ModelStateErrorResponse.Create`, registrado como `InvalidModelStateResponseFactory` do `[ApiController]`: JSON malformado e valores de query que não vinculam (`_page=x`), um elemento `chave: mensagem` por erro. `SuppressMapClientErrors = true` deixa o 415 e afins sem `ProblemDetails`.
- `StatusCodeErrorResponse.Create`, registrado via `UseStatusCodePages`: preenche todo 4xx/5xx que sairia sem corpo: 401 e 403 do JWT (o `WWW-Authenticate` é preservado), 404 de rota, 405, 415. Os `JwtBearerEvents` (só Debug) não foram tocados.

Duas decisões durante a execução: `ErrorResponse.JsonOptions` usa `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` (o spec dizia encoder padrão) porque o teste de fumaça mostrou o mesmo corpo com `"` num caminho e `\"` no outro; e o array interno é serializado com as mesmas opções, para apóstrofos das mensagens do FluentValidation não virarem `'`.

Trace: CMN-RSP-03 e CMN-RSP-04 saíram do `StepTraceResultFilter` para seus produtores (`ModelStateErrorResponse` e `BaseController`); o filtro ignora resultados cujo valor é `ErrorResponse`. Chaves novas CMN-RSP-10 (500) e CMN-RSP-11 (status sem corpo). `conventions.md` CMN-RSP reescrito com uma só forma de erro e a tabela do catálogo; notas de CMN-PIP-03 e CMN-LST-07 corrigidas; as limitações "três formas de erro" e "500 sem envelope" removidas. `README_.md` §6 ganhou um exemplo de erro real. CMN-RSP-10 entrou nos misses esperados do console (TD-030).

Revisão final (revisor independente) e rodada de correção: (1) cada elemento de `detail` da validação passou a ser `propriedade: mensagem` quando a falha nomeia uma propriedade, porque as regras filhas do FluentValidation (`Items[1].Quantity`) não citam a linha na mensagem e o cliente perdia essa informação que a lista crua antiga trazia; (2) `BadHttpRequestException` do Kestrel (413 corpo grande, 400 chunked inválido) mantém o status e recebe o corpo do catálogo em vez de virar 500 logado; (3) teste unitário pinando que o `WWW-Authenticate` sobrevive ao corpo do 401; (4) comentários de work item que faltavam. Menores adiados sem item: desconexão do cliente logada como erro no catch-all, os quatro `catch` tipados sem o filtro `HasStarted`, fallback do model state com a mensagem crua da exceção (mantido de propósito: a mensagem do leitor JSON ajuda o cliente e não é sensível), CMN-RSP-01 duas vezes no trace do 415.

Verificação: Unit 788/788 (19 testes novos: `ErrorResponseTests`, `ModelStateErrorResponseTests`, `StatusCodeErrorResponseTests`, `ErrorResponseTracingTests`, middleware e `BaseController` reescritos), Integration 62/62, build Release sem erros e só os dois avisos pré-existentes, `t all` nos bancos scratch com exit 0 e `no unexpected misses` (CMN-RSP-11 visto 7 vezes). Fumaça por curl cobriu 400 (validador, regra filha em `Items[1].Quantity`, regra de desconto, JSON malformado, `_page` inválido, `_order` desconhecido), 401 (sem token e login errado), 403, 404 (entidade e rota), 405, 413 e 415.

Também nesta sessão, sem work item por decisão do owner: o PostgreSQL do compose passou a ser publicado na porta 5433 do host (5432 dentro do contêiner) e o Redis na 6380 (6379 dentro), com `appsettings.json`, `README_.md` e `CLAUDE.md` atualizados; o fixture dos testes de integração lê o `appsettings.json` da WebApi e não mudou, e nada em `src/` usa o Redis.

FILES
- README_.md (alterado)
- backend/docker-compose.yml (alterado)
- backend/docs/conventions.md (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Common/ErrorResponse.cs (novo)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Common/ModelStateErrorResponse.cs (novo)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Common/StatusCodeErrorResponse.cs (novo)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Common/BaseController.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Auth/AuthController.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/BranchesController.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CustomersController.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/DiscountPoliciesController.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ProductsController.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Users/UsersController.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Middleware/ValidationExceptionMiddleware.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Program.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Tracing/StepTraceResultFilter.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.json (alterado)
- backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/ErrorResponseTests.cs (novo)
- backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/ModelStateErrorResponseTests.cs (novo)
- backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/StatusCodeErrorResponseTests.cs (novo)
- backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/BaseControllerTests.cs (alterado)
- backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Middleware/ValidationExceptionMiddlewareTests.cs (alterado)
- backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Tracing/ErrorResponseTracingTests.cs (novo)
- backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Tracing/StepTraceResultFilterTests.cs (alterado)
- backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/ScenarioCatalog.cs (alterado)

## FEAT-003 — Read model de vendas no MongoDB

`GET /api/sales/{id}` e `GET /api/sales` passaram a ler de uma coleção do MongoDB (`developer_evaluation_read.sales`) alimentada pelos eventos de venda que já saem pelo outbox transacional. O PostgreSQL continua sendo o único banco de escrita e a fonte da verdade; a projeção carrega a venda inteira, incluindo os campos de snapshot de desconto de cada item. Fecha as linhas C3 e C11 da avaliação (MongoDB sem dado de domínio) e absorve a TD-018.

O que foi construído:

- `ORM/ReadModel`: `ReadModelSettings` (chaves `ConnectionStrings:ReadModel`, `ReadModel:Database`, `ReadModel:Collection`, sem default, falha no startup nomeando a chave), `SaleDocument`/`SaleItemDocument` (o `SaleSnapshot` sob os nomes dos DTOs de resposta, porque o `ListQueryParser` e o `ApplyFilters` trabalham por nome de propriedade, mais `Version` e `IsDeleted`; `Guid` com `[BsonGuidRepresentation(Standard)]` em cada membro, pois o driver 3.x não tem representação padrão; `decimal` como `Decimal128`), `MongoLike` (traduz o padrão ILIKE que o parser já produz para uma expressão regular ancorada e case-insensitive, `%` → `.*`, `_` → `.`, escapes preservados) e `SaleReadStore`.
- `ISaleReadStore` em `Domain/Repositories`: `UpsertAsync` e `MarkDeletedAsync` devolvem `bool` (false = evento mais antigo que o documento), `GetAsync` (null para ausente ou tombstone) e `ListAsync`.
- Guarda de versão: cada escrita é uma operação no servidor com filtro `_id == id && Version < sequence` e upsert. Quando o filtro não casa, o servidor recusa a inserção do segundo `_id` com `DuplicateKey`; como esse erro também acontece quando dois handlers inserem o primeiro documento da mesma venda ao mesmo tempo (`SaleCreated` e `SaleModified` no mesmo lote, paralelismo 20), o store repete a mesma escrita uma vez sem upsert e devolve `MatchedCount > 0`. `SaleDeleted` vira tombstone (`IsDeleted` + `Version`), então um `SaleModified` atrasado não ressuscita a venda.
- O relay envia o `Sequence` da linha do outbox como cabeçalho Rebus `outbox-sequence` (`IEventPublisher.PublishAsync` ganhou o parâmetro). `SaleProjectionHandler` (`WebApi/Messaging`) consome os cinco eventos: upsert para `SaleCreated`/`SaleModified`, tombstone para `SaleDeleted`, nada para `SaleCancelled`/`ItemCancelled` (o `SaleModified` gravado junto já carrega o estado). Mensagem sem o cabeçalho falha com mensagem que o nomeia e vai para a fila de erro, em vez de ser aplicada com versão inventada. `SaleEventLogHandler` passou a injetar `IMessageContext` (TD-018), o que permitiu o primeiro teste unitário dele.
- `GetSaleHandler` e `ListSalesHandler` leem do store; `SaleProfile` e `ListSalesProfile` ganharam os mapas `SaleSnapshot → SaleResult/ListSalesItem` e `SaleSnapshotItem → SaleItemResult` (`SaleId`/`ItemId` → `Id`). `ISaleRepository.ListAsync` e `SaleRepository.ListAsync` foram removidos com os testes de integração que só eles exercitavam. `ApplyFilters` ganhou uma sobrecarga com o tradutor de `Like`; a antiga continua com o `ILike` do Npgsql, então os outros quatro repositórios não mudaram.
- `Outbox:PollingInterval` caiu de 5 s para 500 ms, em vez de um sinal do commit para o relay: custo de duas consultas por segundo ao índice parcial vazio, janela de leitura invisível para quem clica no Swagger. Sem índices na coleção (a unicidade de `SaleNumber` é do PostgreSQL).
- Console `t`: `ConnectionStrings:ReadModel` e `ReadModel:Database` são encaminhados à API hospedada; `DatabaseWipe` derruba também o banco do read model; `sale-async` e `sale-list` esperam `SAL-PRJ-01` antes de ler; `SAL-PRJ-02` entrou nos misses esperados (TD-030).
- Docs: seção `SAL-PRJ` em `sales.md` (chaves `SAL-PRJ-01` e `SAL-PRJ-02`), overview, data model, `SAL-GET`, `SAL-LST`, `SAL-CON`, `SAL-DSP` e limitações atualizados; `conventions.md` CMN-LST cita o store; `INDEX.md`; `README_.md` (§1, §6, §7, §8, §9, §11) e `CLAUDE.md`.

Decisões durante a execução (ledger): o `NotFoundHandlersTests` também construía o `GetSaleHandler` e passou ao store substituído; o plano contava 25 testes na tarefa 1 e são 24.

Verificação (após a correção): Unit 825/825, Integration 69/69 (compose PostgreSQL e MongoDB), build Release com 0 erros e os dois warnings pré-existentes, `t all` contra `trace_scratch`/`trace_scratch_bus`/`trace_scratch_read` com `exit 0`, `no unexpected misses`, `SAL-PRJ-01` visto 26 vezes; no `sale-async` o primeiro `GET` responde 404 e o segundo 200 vindo do Mongo; o `sale-list` responde totais 3, 2 e 3; a coleção scratch mostra `_id` como UUID padrão, `Version` como `Long` e um tombstone.

Revisão final (revisor independente) e rodada de correção: sem críticos; um importante corrigido: a lista rodava sem collation, então `_order=customerName` devolvia ordem de bytes (maiúsculas antes das minúsculas, acentuados depois do "z"), diferente da ordem `en_US.utf8` que o PostgreSQL dava; o store passou a consultar com a collation `en` do MongoDB e um teste de integração pina a ordem `acme, Álvaro, Beta, zeta` (emenda A8 da spec). Também corrigidos os comentários de work item que faltavam e a contagem de misses esperados no `README_.md` §9. Menores adiados sem item: `Like` difere do ILIKE em nomes com quebra de linha (`\z` e `Singleline` resolveriam); um filtro `saleDate` exato copiado da resposta do POST (microssegundos) não casa o valor BSON (milissegundos); `IMongoClient` registrado como instância em vez de fábrica; a contagem da lista carrega o `$sort`; um `t` scratch sem `--ReadModel:Database` derruba o banco de leitura de desenvolvimento; falta nota de operação para drenar a fila antes de implantar (mensagens sem o cabeçalho vão para a fila de erro).

FILES

- backend/docker-compose.yml (alterado)
- backend/docs/INDEX.md (alterado)
- backend/docs/conventions.md (alterado)
- backend/docs/sales.md (alterado)
- backend/src/Ambev.DeveloperEvaluation.Application/Sales/Common/SaleProfile.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.Application/Sales/GetSale/GetSaleHandler.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesHandler.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesProfile.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.Domain/Events/IEventPublisher.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleReadStore.cs (novo)
- backend/src/Ambev.DeveloperEvaluation.Domain/Repositories/ISaleRepository.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/InfrastructureModuleInitializer.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.ORM/Ambev.DeveloperEvaluation.ORM.csproj (alterado)
- backend/src/Ambev.DeveloperEvaluation.ORM/Outbox/OutboxRelay.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.ORM/ReadModel/MongoLike.cs (novo)
- backend/src/Ambev.DeveloperEvaluation.ORM/ReadModel/ReadModelSettings.cs (novo)
- backend/src/Ambev.DeveloperEvaluation.ORM/ReadModel/SaleDocument.cs (novo)
- backend/src/Ambev.DeveloperEvaluation.ORM/ReadModel/SaleItemDocument.cs (novo)
- backend/src/Ambev.DeveloperEvaluation.ORM/ReadModel/SaleReadStore.cs (novo)
- backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/ListQueryExtensions.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/RebusEventPublisher.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/SaleEventLogHandler.cs (alterado)
- backend/src/Ambev.DeveloperEvaluation.WebApi/Messaging/SaleProjectionHandler.cs (novo)
- backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.json (alterado)
- backend/tests/Ambev.DeveloperEvaluation.Integration/ListPaginationTests.cs (alterado)
- backend/tests/Ambev.DeveloperEvaluation.Integration/MongoReadModelFixture.cs (novo)
- backend/tests/Ambev.DeveloperEvaluation.Integration/OutboxRelayTests.cs (alterado)
- backend/tests/Ambev.DeveloperEvaluation.Integration/RepositoryListQueryTests.cs (alterado)
- backend/tests/Ambev.DeveloperEvaluation.Integration/SaleReadStoreTests.cs (novo)
- backend/tests/Ambev.DeveloperEvaluation.Unit/Application/NotFoundHandlersTests.cs (alterado)
- backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/GetSaleHandlerTests.cs (alterado)
- backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSalesHandlerTests.cs (alterado)
- backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/ListSalesProfileTests.cs (novo)
- backend/tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleProfileTests.cs (alterado)
- backend/tests/Ambev.DeveloperEvaluation.Unit/DevConsole/DatabaseWipeTests.cs (alterado)
- backend/tests/Ambev.DeveloperEvaluation.Unit/DevConsole/TraceCommandTests.cs (alterado)
- backend/tests/Ambev.DeveloperEvaluation.Unit/ORM/ReadModel/MongoLikeTests.cs (novo)
- backend/tests/Ambev.DeveloperEvaluation.Unit/ORM/ReadModel/ReadModelSettingsTests.cs (novo)
- backend/tests/Ambev.DeveloperEvaluation.Unit/ORM/ReadModel/SaleDocumentTests.cs (novo)
- backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Messaging/RebusEventPublisherTests.cs (alterado)
- backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Messaging/SaleEventLogHandlerTests.cs (novo)
- backend/tests/Ambev.DeveloperEvaluation.Unit/WebApi/Messaging/SaleProjectionHandlerTests.cs (novo)
- backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/DatabaseWipe.cs (alterado)
- backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/ScenarioCatalog.cs (alterado)
- backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/SaleAsyncScenario.cs (alterado)
- backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/SaleListScenario.cs (alterado)
- backend/tools/Ambev.DeveloperEvaluation.DevConsole/Trace/TraceCommand.cs (alterado)
- CLAUDE.md (alterado)
- README_.md (alterado)

## TD-038 — Layout do repositório na raiz: src/, tests/, README.md

A solução .NET saiu de `backend/` e foi para a raiz, para o repositório seguir o `.doc/project-structure.md` do desafio (`src/`, `tests/` e `README.md` na raiz). Fecha as linhas S1 e S2 da avaliação.

O que mudou:

- `git mv` de todo o conteúdo versionado de `backend/` para a raiz: `src/`, `tests/`, `tools/`, `docs/`, a `.sln`, os arquivos de compose, os dois `Dockerfile`, `.dockerignore`, `.editorconfig`, scripts de cobertura, `launchSettings.json` e o `.dcproj`. Os caminhos relativos dentro da solução não mudaram, então `.sln`, `.csproj`, Dockerfile e compose continuam iguais.
- A pasta local `docs/` virou `docs-private/` (continua fora do git), e `backend/docs/` (documentação da API, versionada) virou `docs/`. O `CLAUDE.md` e a memória foram ajustados: a regra de nunca citar a pasta local em PR agora fala de `docs-private/`, e `docs/` pode ser citada.
- `README.md` (enunciado) virou `CHALLENGE.md`, e `README_.md` (o guia) virou `README.md`. Os links "Back to README" do `.doc/` apontam para `CHALLENGE.md`; os links `../../README_.md` e `../../.doc/` da documentação da API perderam um nível.
- `docker-compose.yml` fixa `name: backend`: o nome do projeto compose vinha da pasta, e sem isso o compose na raiz não enxergaria os contêineres e volumes `backend_*` já existentes. `.dockerignore` ganhou `docs-private`, `.superpowers`, `.claude` e `.idea`, já que o contexto de build agora é a raiz.
- `Makefile` sem `cd backend`; guia sem os `cd backend`, caminhos `backend/...` e árvore do §15 redesenhada; template de PR com `docs/`. Três textos no código citavam `backend/docs` ou "from backend" (`StepTrace`, `StepKeyCoverageTests`, `DatabaseWipe.HeldMessage`), com `TD-038` no comentário.
- Sobras não versionadas: `backend/trace.txt` foi para a raiz, as configurações do Rider (`.idea.Ambev.DeveloperEvaluation`) foram para `.idea/`, e `bin/`, `obj/` e `.DS_Store` foram apagados.

Verificação: build limpo (após apagar todos os `bin/` e `obj/`) com 0 erros e os avisos antigos NU1903 e CS8604, Unit 825/825 (o `StepKeyCoverageTests` prova que `docs/` é achada a partir da `.sln`), Integration 69/69, `docker compose build` da API pela raiz com contexto de 2,87 MB, `docker compose ps` pela raiz enxergando os quatro contêineres em execução, `make -n dev-up` sem `cd`.

FILES

Renomeados sem alteração de conteúdo (600 arquivos): todo o conteúdo versionado de `backend/src/`, `backend/tests/` e `backend/tools/` para `src/`, `tests/` e `tools/`, mais `backend/{.editorconfig, Ambev.DeveloperEvaluation.sln, Dockerfile, coverage-report.bat, coverage-report.sh, docker-compose.dcproj, docker-compose.override.yml, launchSettings.json}` para a raiz.

Renomeados e alterados:

- .dockerignore (alterado, de backend/.dockerignore)
- docker-compose.yml (alterado, de backend/docker-compose.yml)
- docs/INDEX.md (alterado, de backend/docs/INDEX.md)
- docs/TEMPLATE.md (alterado, de backend/docs/TEMPLATE.md)
- docs/auth.md (alterado, de backend/docs/auth.md)
- docs/branches.md (alterado, de backend/docs/branches.md)
- docs/conventions.md (alterado, de backend/docs/conventions.md)
- docs/customers.md (alterado, de backend/docs/customers.md)
- docs/discount-policies.md (alterado, de backend/docs/discount-policies.md)
- docs/products.md (alterado, de backend/docs/products.md)
- docs/sales.md (alterado, de backend/docs/sales.md)
- docs/users.md (alterado, de backend/docs/users.md)
- src/Ambev.DeveloperEvaluation.Common/Tracing/StepTrace.cs (alterado, de backend/src/...)
- tests/Ambev.DeveloperEvaluation.Unit/Common/Tracing/StepKeyCoverageTests.cs (alterado, de backend/tests/...)
- tools/Ambev.DeveloperEvaluation.DevConsole/Trace/DatabaseWipe.cs (alterado, de backend/tools/...)
- CHALLENGE.md (novo, de README.md)
- README.md (alterado, conteúdo de README_.md)
- README_.md (removido)

Alterados no lugar:

- .doc/auth-api.md (alterado)
- .doc/carts-api.md (alterado)
- .doc/frameworks.md (alterado)
- .doc/general-api.md (alterado)
- .doc/overview.md (alterado)
- .doc/products-api.md (alterado)
- .doc/project-structure.md (alterado)
- .doc/tech-stack.md (alterado)
- .doc/users-api.md (alterado)
- .github/pull_request_template.md (alterado)
- Makefile (alterado)
- CLAUDE.md (alterado, não versionado)

## TD-039 — Agregado Sale rico: sem setters públicos, regra de desconto no Domain

`Sale` e `SaleItem` deixaram de ser sacos de propriedades. Fecha a lacuna N9 (linha C2 da avaliação): setters públicos e checagem de regra na Application.

O que mudou:

- `Sale`: todos os setters viraram `private set`; `Items` é uma lista só de leitura sobre o campo `_items`, que o EF mapeia com acesso por campo (mesmo padrão do `DiscountPolicy`); construtor privado para o EF. A venda nasce por `Sale.Create(...)`, alimentada por registros `SaleLine` (novo, `Domain/Entities`), e o PUT a altera por `ChangeCustomer`, `ChangeBranch`, `SetCancelled` e `SyncItems`, que agora recebe `SaleLine`. `ApplyDiscounts` não mudou.
- `SaleItem`: setters privados; só a `Sale` cria ou altera itens, pelos membros internos `From`, `Update` (a antiga `CopyValues`), `Renumber` e `Price` (o antigo `Sale.Price`).
- `SaleDiscountRules` e `SaleDiscountLine` foram de `Application/Sales/Common` para `Domain/Services` sem mudar lógica, códigos nem mensagens. Os handlers continuam chamando a checagem antes de montar a venda, então o 400 com uma entrada por linha é o mesmo; o `ApplyDiscounts` segue recusando estado inválido sozinho.
- `CreateSaleHandler` e `UpdateSaleHandler` não atribuem mais campos da entidade. Ordem dos passos e chaves de trace iguais.
- Testes: o helper `Persisted` (Unit e Integration) monta entidades como o EF carrega do banco, para os testes que precisam de estado gravado (ids e número vindos do banco, valores de uma precificação anterior), sem abrir setter no domínio. `SaleTests` reescrito sobre `SaleLine`, com cinco testes novos (fábrica, os três métodos do PUT e um teste de arquitetura que falha se `Sale` ou `SaleItem` tiverem setter público). Teste de integração novo: venda gravada, carregada pelo repositório, alterada por `SyncItems` (item mantido e alterado, removido, adicionado), salva e relida.
- `docs/sales.md`: as duas linhas `Source:` apontam para `Domain/Services/SaleDiscountRules.cs`.

Revisão final (revisor independente): nenhum Critical ou Important. Corrigidos três Minor: o teste de integração passou a provar que o item mantido é o mesmo registro (verificado por mutação: com o `SyncItems` recriando o item, o teste falha), o comentário de work item foi para o membro `Configure` e a reordenação de usings introduzida pela conversão foi desfeita. Adiados: `Sale.Create` não recusa linha com `ItemId` ou cancelada; `Persisted.Set` aceita `null` em tipo por valor e conversões de alargamento.

Verificação: build com 0 erros e só os avisos antigos (NU1903, CS8604), Unit 830/830, Integration 70/70, `dotnet ef migrations has-pending-model-changes` sem mudanças, `t all` contra `trace_scratch`/`trace_scratch_bus`/`trace_scratch_read` com `exit 0`, `no unexpected misses` e todas as chaves de create e update vistas.

FILES

- docs/sales.md (alterado)
- src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs (alterado)
- src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleHandler.cs (alterado)
- src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs (alterado)
- src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs (alterado)
- src/Ambev.DeveloperEvaluation.Domain/Entities/SaleLine.cs (novo)
- src/Ambev.DeveloperEvaluation.Domain/Services/SaleDiscountLine.cs (alterado, movido de src/Ambev.DeveloperEvaluation.Application/Sales/Common/SaleDiscountLine.cs)
- src/Ambev.DeveloperEvaluation.Domain/Services/SaleDiscountRules.cs (alterado, movido de src/Ambev.DeveloperEvaluation.Application/Sales/Common/SaleDiscountRules.cs)
- src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs (alterado)
- tests/Ambev.DeveloperEvaluation.Integration/ListQueryExtensionsTests.cs (alterado)
- tests/Ambev.DeveloperEvaluation.Integration/Persisted.cs (novo)
- tests/Ambev.DeveloperEvaluation.Integration/SaleRepositoryTests.cs (alterado)
- tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/CreateSaleHandlerTests.cs (alterado)
- tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleProfileTests.cs (alterado)
- tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/UpdateSaleHandlerTests.cs (alterado)
- tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleDiscountTests.cs (alterado)
- tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs (alterado)
- tests/Ambev.DeveloperEvaluation.Unit/Domain/Events/SaleSnapshotTests.cs (alterado)
- tests/Ambev.DeveloperEvaluation.Unit/Domain/Services/SaleDiscountRulesTests.cs (alterado, movido de tests/Ambev.DeveloperEvaluation.Unit/Application/Sales/SaleDiscountRulesTests.cs)
- tests/Ambev.DeveloperEvaluation.Unit/TestData/Persisted.cs (novo)

## BUG-013, TD-014 — Location nos 201 e JWT no Swagger

Fecha os dois pontos da linha C9 da avaliação que um avaliador vê ao usar a API: o `Location` vazio dos creates e o Swagger sem como enviar o token. A falta de token de concorrência na venda (N7) ficou fora, por decisão do dono: registrada como TD-040 no backlog.

O que mudou:

- BUG-013: os seis creates (usuários, clientes, filiais, produtos, políticas de desconto e vendas) respondiam `Created(string.Empty, ...)`. Agora respondem `CreatedAtAction(nameof(GetX), new { id = response.Id }, ...)`, o mesmo padrão do 202 da venda assíncrona com `AcceptedAtAction`: o `Location` aponta para o GET por id do recurso criado. Status, corpo e contrato iguais. Na venda síncrona, um GET imediato no `Location` pode dar 404 por até `Outbox:PollingInterval`, como o 202 já documentado.
- TD-014: `SwaggerExtensions` (novo, `WebApi/Common`) registra o Swagger com o esquema HTTP `bearer` (formato JWT) e o requisito global; `Program.cs` chama `AddSwaggerWithJwtBearer`. O Swagger UI ganha o botão Authorize e envia o token. Endpoints anônimos continuam respondendo sem token.
- `README.md`: o aviso de que o Swagger não envia o token virou a instrução do Authorize; a linha do create síncrono na tabela de venda cita o `Location`.
- Testes: `CreatedLocationTests` (cinco creates apontam para a ação GET com o id criado), `SalesControllerCreateSaleTests` (o create síncrono agora verifica o `Location`), `SwaggerExtensionsTests` (esquema e requisito).

Verificação: build com 0 erros e só os avisos antigos, Unit 837/837, Integration 70/70, `t all` contra os bancos scratch com `exit 0` e `no unexpected misses`. API real nos bancos scratch (porta 5199): o `swagger.json` traz `securitySchemes.Bearer` e `security: [{Bearer: []}]`; `POST /api/branches` respondeu 201 com `Location: http://localhost:5199/api/Branches/<id>`, e o GET nesse endereço respondeu 200.

FILES

- README.md (alterado)
- docs/conventions.md (alterado)
- src/Ambev.DeveloperEvaluation.WebApi/Common/SwaggerExtensions.cs (novo)
- src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/BranchesController.cs (alterado)
- src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CustomersController.cs (alterado)
- src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/DiscountPoliciesController.cs (alterado)
- src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ProductsController.cs (alterado)
- src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs (alterado)
- src/Ambev.DeveloperEvaluation.WebApi/Features/Users/UsersController.cs (alterado)
- src/Ambev.DeveloperEvaluation.WebApi/Program.cs (alterado)
- tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/SwaggerExtensionsTests.cs (novo)
- tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/CreatedLocationTests.cs (novo)
- tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/SalesControllerCreateSaleTests.cs (alterado)

## TD-004, TD-008, TD-009, TD-012, TD-019, TD-041 — Build sem avisos e sobras do template

Fecha a parte visível da linha C19 da avaliação: os dois avisos de build e quatro sobras do template. O resto da dívida `could` fica no backlog rastreado; o `TD-003` (ValidationBehavior sem validators em DI) ficou de fora.

O que mudou:

- TD-008: o alerta GHSA-rvv3-g6hj-g44x do AutoMapper 13.0.1 (sem limite de profundidade: um grafo aninhado muito fundo estoura a pilha) foi suprimido com `NuGetAuditSuppress` no projeto Application, com a justificativa no próprio `.csproj`: nenhum tipo mapeado referencia a si mesmo, então a entrada não aninha. A correção real é a versão 15.1.1, que exige chave de licença e muda a API de configuração; a supressão deve sair quando o pacote for atualizado.
- TD-041 (N12, novo): `JwtTokenGenerator` passava `Jwt:SecretKey` sem checagem de nulo (CS8604, código do template). Uma chave ausente agora lança `InvalidOperationException` nomeando a chave, como as outras configurações obrigatórias.
- TD-012: `CreateSaleProfile` ignora `CreateSaleCommand.Id` explicitamente, então um campo futuro no request não deixa o cliente escolher o id da venda.
- TD-004: `WebApiModuleInitializer` registrava de novo `AddControllers` e `AddHealthChecks`, que o `Program.cs` já registra; a classe e a chamada no `DependencyResolver` foram removidas.
- TD-009: `WebApi/Mappings/CreateUserRequestProfile.cs` duplicava o mapeamento de `CreateUserProfile`; removido, com a pasta vazia.
- TD-019: `Domain/Events/UserRegisteredEvent.cs`, do template e sem nenhuma referência, removido.
- `CLAUDE.md`: a linha de DI não cita mais o `WebApiModuleInitializer`.

Verificação: build do zero com 0 erros e 0 avisos, Unit 839/839 (testes novos: chave JWT ausente e configuração válida do `CreateSaleProfile`, ambos vistos falhando antes), Integration 70/70, `t all` contra os bancos scratch com `exit 0`, `no unexpected misses` e as chaves de health check e de criação de usuário vistas.

FILES

- src/Ambev.DeveloperEvaluation.Application/Ambev.DeveloperEvaluation.Application.csproj (alterado)
- src/Ambev.DeveloperEvaluation.Common/Security/JwtTokenGenerator.cs (alterado)
- src/Ambev.DeveloperEvaluation.Domain/Events/UserRegisteredEvent.cs (removido)
- src/Ambev.DeveloperEvaluation.IoC/DependencyResolver.cs (alterado)
- src/Ambev.DeveloperEvaluation.IoC/ModuleInitializers/WebApiModuleInitializer.cs (removido)
- src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleProfile.cs (alterado)
- src/Ambev.DeveloperEvaluation.WebApi/Mappings/CreateUserRequestProfile.cs (removido)
- tests/Ambev.DeveloperEvaluation.Unit/Common/Security/JwtTokenGeneratorTests.cs (novo)
- tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Sales/CreateSaleProfileTests.cs (novo)
- CLAUDE.md (alterado, não versionado)

## TD-042 — Geradores Bogus para os testes de Customer, Branch, Product e Sale

Fecha a lacuna N1 da avaliação (linhas C12 e T10): até aqui `Faker<T>` só aparecia nos três arquivos de teste de User herdados do template, e os testes de Customer, Branch, Product e Sale montavam os dados à mão.

- Novos geradores em `Unit/Domain/Entities/TestData/`, no mesmo formato do `UserTestData`: `CustomerTestData` (nome de pessoa e CPF válido sem pontuação, via `Bogus.Extensions.Brazil`), `BranchTestData` (nome de cidade), `ProductTestData` (código alfanumérico, nome de produto, preço em centavos).
- `SaleTestData`: como `Sale` não tem setter público desde o TD-039, o `Faker<SaleLine>` gera as linhas (um produto por linha, 1 a 20 unidades, dentro do máximo da política do README, sem desconto pedido) e o cabeçalho sai de um `Faker`; a venda nasce por `Sale.Create`.
- `CustomerTests`, `BranchTests` e `ProductTests` usam os geradores; nos casos inválidos só o campo sob teste é sobrescrito. `SaleTests` ganhou um caso: venda gerada, precificada pela política do README, passa na validação e o total é a soma dos itens (mutação com 21 unidades faz o teste falhar).
- `DiscountPolicy` mantém o `DiscountPolicyTestData` determinístico, porque seus testes conferem as faixas exatas do README. Testes de handler e de integração seguem com dados fixos, pois conferem valores exatos.
- Verificação: build da solução com 0 avisos; Unit 840 de 840; os testes convertidos rodaram 20 vezes sem falha.

FILES
- `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/TestData/CustomerTestData.cs` (novo)
- `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/TestData/BranchTestData.cs` (novo)
- `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/TestData/ProductTestData.cs` (novo)
- `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/TestData/SaleTestData.cs` (novo)
- `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/CustomerTests.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/BranchTests.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/ProductTests.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs` (alterado)

## TD-030 — Console de trace: as oito chaves que nenhum cenário exercitava

A execução completa do console de trace (`t all`) listava oito chaves documentadas como "expected misses": caminhos de falha e de reentrega que nenhum cenário provocava. Agora um cenário novo, `failures`, o último do catálogo, exercita todas, e a lista de misses esperados fica vazia: a execução completa termina com `documented keys not seen: 0`.

- Seis chaves saem de gatilhos reais, sem mexer na API: USR-SED-06 (o `AdminSeeder` roda de novo e o admin já existe), SAL-OBW-02 (`IOutbox.EnqueueAsync` fora de transação), SAL-CRT-06 (o mesmo `CreateSaleCommand` de uma venda já gravada é reenviado pelo bus), SAL-PRJ-02 (o `ProcessedAt` da linha `SaleCreated` é zerado e o relay reenvia um evento que a projeção já tem), SAL-DSP-07 (uma linha de tipo não registrado no outbox, apagada em seguida para destravar o relay) e SAL-BUS-06 (um `SaleCancelled` enviado direto ao bus, sem o cabeçalho de sequência do relay; a projeção lança `InvalidOperationException`, que não é fail-fast, e o Rebus tenta 5 vezes até a fila de erro).
- Duas chaves exigem falha injetada, e ela existe só no host do trace (`ConfigureTestServices` no `TraceHost`): `TraceFaults` arma falhas de disparo único; `FaultyOutboxRelay` envolve o `IOutboxRelay` e faz um ciclo falhar (SAL-RLY-07); `FailingRequestFilter`, um filtro MVC global, faz uma action lançar exceção não tratada, que vira 500 `ServerError` (CMN-RSP-10). Nada disso toca `src/`.
- Tentativa descartada: uma mensagem sem handler para o SAL-BUS-06; o Rebus trata "sem handler" como fail-fast (saiu SAL-BUS-05), então o gatilho virou o evento sem cabeçalho de sequência.
- `ScenarioContext` passou a expor `Services` (o provider do host) e `Faults`; `TraceCommand` e os testes que constroem o contexto foram ajustados. README: lista de cenários e o texto sobre misses esperados.
- Verificação: build com 0 avisos; Unit 844 de 844 (3 testes novos das falhas injetadas, 1 do catálogo, vistos falhando antes); `t all` nas bases de rascunho com exit 0, 263 chaves vistas, 0 faltando, nenhum `!!!`; `t failures` sozinho também com exit 0.

FILES
- `tools/Ambev.DeveloperEvaluation.DevConsole/Trace/TraceFaults.cs` (novo)
- `tools/Ambev.DeveloperEvaluation.DevConsole/Trace/FaultyOutboxRelay.cs` (novo)
- `tools/Ambev.DeveloperEvaluation.DevConsole/Trace/FailingRequestFilter.cs` (novo)
- `tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/FailuresScenario.cs` (novo)
- `tools/Ambev.DeveloperEvaluation.DevConsole/Trace/TraceHost.cs` (alterado)
- `tools/Ambev.DeveloperEvaluation.DevConsole/Trace/ScenarioContext.cs` (alterado)
- `tools/Ambev.DeveloperEvaluation.DevConsole/Trace/ScenarioCatalog.cs` (alterado)
- `tools/Ambev.DeveloperEvaluation.DevConsole/Trace/TraceCommand.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/DevConsole/TraceFaultsTests.cs` (novo)
- `tests/Ambev.DeveloperEvaluation.Unit/DevConsole/ScenarioCatalogTests.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/DevConsole/TraceCommandTests.cs` (alterado)
- `README.md` (alterado)

## TD-043 — Revisão cega: correções pontuais, passo de política por escopo no guia e testes funcionais de vendas

Correções confirmadas a partir de uma revisão independente, feita por um agente sem contexto sobre `dev` em `38abe5d`. O F05 ficou de fora por projeto: as regras do desafio são a política padrão semeada, e políticas por produto ou filial podem ter limite e faixas próprios (FEAT-001).

- **Testes funcionais (N2, F14).** `tests/Ambev.DeveloperEvaluation.Functional` deixa de ser só um `.csproj`. O `SalesApiFixture` sobe a API inteira com `WebApplicationFactory` em bancos descartáveis (Postgres, fila, read model e logs; servidor e credenciais vêm do appsettings da API, só o nome do banco muda) e os apaga no fim. `SalesBusinessRulesTests` prova por HTTP, com valores escritos à mão: 1 e 3 unidades sem desconto, 4 e 9 com 10%, 10 e 20 com 20%, 21 numa linha ou em duas (12+9) com 400 `QuantityLimitExceeded`, desconto pedido abaixo do teto e no teto aplicado, acima (10,01%, ou 1% com 3 unidades) com 400 `DiscountAboveAllowed`, cada produto no próprio total, nomes e preço copiados do catálogo, cancelamento de linha reprecificando as outras e uma política só de um produto com limite e faixa próprios enquanto outro produto segue a padrão. Confere nas respostas de `POST` e `PUT`, não no `GET`, que lê o read model com atraso. Duas mutações provaram que a suíte pega erro: limite `>= 20` e soma trocada pela maior linha.
- **F04.** Desativar uma política travava para sempre o PUT das vendas anteriores, inclusive só cancelar, porque o resolver ignorava a política desativada qualquer que fosse a data da venda e uma política nova não pode começar no passado. Agora `DiscountPolicy.IsInEffectAt` e `GetApplicableAsync` tratam a desativação como fim da vigência: a política vale para vendas com data anterior a `DisabledAt`. Testes: unitário (antes e no instante da desativação), integração (política desativada depois da data é retornada) e funcional (`SalesPolicyDisableTests`, em fixture própria porque desativa a padrão: a venda continua cancelável). DSC-DIS-05, SAL-CRT-14 e SAL-UPD-18 atualizados.
- **F11.** Comentários que citavam âncoras de spec fora do repositório (spec section 3.5, spec section 4, A2, A11, A14, A15, A17, D5, D9, D10) agora dizem a regra; "README rules/tiers/policy" virou regras do desafio ou política padrão (`ReadmeTiers` → `ChallengeTiers`).
- **F06 (só documentação).** A seção CMN-RSP de `docs/conventions.md` agora diz onde o corpo de erro difere do `.doc/general-api.md`, como o INDEX prometia.
- **F18.** Removidos o registro duplicado de `IJwtTokenGenerator`, os helpers `GetCurrentUserId`/`GetCurrentUserEmail` do `BaseController` (sem chamador e lançando `NullReferenceException`; isso aposenta a correção do BUG-001, que era código morto desde o template) com o teste do primeiro, e o `Dockerfile` da raiz, duplicado do que o compose usa. O `docker-compose.dcproj`, o `launchSettings.json` da raiz e o override vazio ficam: são o conjunto de launch do Visual Studio.
- **F20.** `Program.cs` checa `ConnectionStrings:DefaultConnection` e falha citando a chave; antes o erro vinha do Npgsql ("Host can't be null"). `StartupConfigurationTests` prova isso e manda o log fatal para um banco de logs descartável.
- **Trace.** O cenário `sale-discount` passou a percorrer os mesmos casos dos testes funcionais (fronteiras 3, 4, 9, 10 e 20; dois produtos; 21 numa linha e em duas; teto; cancelamento; política da `water`) e ganhou o caso do F04: depois de desativar a política da `water`, uma venda feita sob ela continua editável e mantém 5%. Cada caso imprime se o desconto ou o status bateu, e uma divergência vira linha `!!!`. Com a correção do F04 revertida, o trace acusou `!!! line 2 expected 5%, got 0%`.
- **Guia.** O passo 4 do README ganhou o bloco "Discount rules per product and branch": cria uma política para o produto do passo 3 (máximo 50, 30% a partir de 12, `validFrom` 10 s à frente, com o comando `date` de macOS e Linux) e vende 30 unidades com ela; "README rule" virou "challenge rule". O §12 lista a suíte funcional. Nada semeado além do que já existia.
- **Ocorrências.** O `Faker.Person` do Bogus é a mesma pessoa durante a vida do `Faker`, o que repetia o CPF e dava 409; cada cliente usa um `Person` novo. As primeiras execuções do teste de startup gravaram duas linhas "Application terminated unexpectedly" no banco de logs de dev (`developer_evaluation_logs`); o teste foi corrigido e essas linhas expiram pelo TTL de 1 dia.
- **Verificação.** Build com 0 avisos; Unit 844 (+1 da desativação, -1 do helper removido), Integração 71 (+1), Funcional 19 (novo); `has-pending-model-changes` sem mudanças (nenhuma migration; só comentários em duas migrations); `t all` nas bases de rascunho com 0 chaves faltando; nenhum banco `functional_*` sobrou.

FILES
- `tests/Ambev.DeveloperEvaluation.Functional/SalesApiFixture.cs` (novo)
- `tests/Ambev.DeveloperEvaluation.Functional/SalesBusinessRulesTests.cs` (novo)
- `tests/Ambev.DeveloperEvaluation.Functional/SalesPolicyDisableTests.cs` (novo)
- `tests/Ambev.DeveloperEvaluation.Functional/StartupConfigurationTests.cs` (novo)
- `tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj` (alterado)
- `Dockerfile` (removido)
- `tools/Ambev.DeveloperEvaluation.DevConsole/Trace/Scenarios/SaleDiscountScenario.cs` (alterado)
- `README.md` (alterado)
- `docs/conventions.md` (alterado)
- `docs/discount-policies.md` (alterado)
- `docs/sales.md` (alterado)
- `src/Ambev.DeveloperEvaluation.Domain/Entities/DiscountPolicy.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.Domain/Services/SaleDiscountRules.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/Common/DiscountPolicyResult.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.Application/DiscountPolicies/CreateDiscountPolicy/CreateDiscountPolicyHandler.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleHandler.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.Common/Security/AuthenticationExtension.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.ORM/Mapping/DiscountPolicyConfiguration.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.ORM/Migrations/20260925233259_AddDiscountPolicies.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.ORM/Migrations/20260925234012_AddSaleItemDiscountSnapshot.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.ORM/Repositories/DiscountPolicyRepository.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.WebApi/Common/BaseController.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/CreateDiscountPolicy/CreateDiscountPolicyProfile.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.WebApi/Features/DiscountPolicies/DisableDiscountPolicies/DisableDiscountPoliciesProfile.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.WebApi/Program.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Integration/DiscountPolicyRepositoryTests.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/DiscountPolicyTests.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/SaleTests.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/TestData/DiscountPolicyTestData.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/Domain/Entities/TestData/SaleTestData.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/Domain/Services/SaleDiscountRulesTests.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/Domain/ValueObjects/DiscountTierTests.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Common/BaseControllerTests.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Messaging/MessagingSettingsTests.cs` (alterado)

## TD-011 — Healthcheck do MongoDB antes de a API subir

Triagem da segunda revisão cega (commit `db09d88`): só o achado F02 foi considerado relevante para o desafio, porque `make dev-up` é o primeiro passo do avaliador e, com o volume `mongo-data` vazio, a API podia ficar `exited` enquanto o MongoDB inicializava (o transporte Rebus cria o índice da fila na subida). Os demais achados ficam no backlog ou são decisões de design.

O que foi feito:
- `docker-compose.yml`: o serviço MongoDB ganhou `healthcheck` (`mongosh --quiet --host "$(hostname)" --eval "db.adminCommand('ping').ok"`, intervalo 2s, timeout 5s, 30 tentativas, no mesmo padrão do PostgreSQL) e a API passou a depender dele com `condition: service_healthy`. O ping usa o hostname do container, não `localhost`: no volume novo, o entrypoint da imagem roda um `mongod` temporário ligado só em `127.0.0.1:27017` para criar o usuário root (conferido em `docker-entrypoint.sh`, linhas 306-307), e um ping em `localhost` daria saudável antes do servidor real subir.
- `README.md`: o passo 3 da §3 diz que a API espera os healthchecks do PostgreSQL e do MongoDB; a linha de troubleshooting "API container `exited`" da §14 saiu.
- `docs/sales.md`: SAL-BUS-01 e a limitação conhecida citam o healthcheck do compose; `dotnet run` continua precisando do MongoDB no ar.

Verificação: `docker compose config --quiet` válido; o comando do healthcheck rodado dentro do container `ambev_developer_evaluation_nosql` retorna `1` com saída 0, e saída 1 numa porta sem servidor; `StepKeyCoverageTests` 9/9. A subida a frio com volume novo não foi reproduzida (exige recriar containers).

FILES
- `docker-compose.yml` (alterado)
- `README.md` (alterado)
- `docs/sales.md` (alterado)

## TD-044 — README dividido em guias indexados

Achado F16 da segunda revisão cega (commit `db09d88`): o README de 620 linhas misturava o caminho rápido (clonar, `make dev-up`, logar, criar uma venda) com capítulos de teste de carga, trace console, outbox e inspeção de fila. A crítica do resumo, de mecanismos somados, também foi tratada, documentando cada mecanismo como escolha deliberada.

O que foi feito:
- `README.md` (241 linhas): o que roda onde, pré-requisitos, subida com make, schema e administrador, como rodar a API, a primeira venda em três passos (login, cadastros, venda com o exemplo de erro), testes automatizados, layout e o índice dos guias (§9).
- `docs/guide/`: o conteúdo restante movido sem reescrita (só títulos, links e numeração de passos mudaram): `walkthrough.md` (usuários e papéis, políticas por produto e filial, 202 assíncrono, filtros), `configuration.md`, `architecture.md` (entrada assíncrona, eventos, inspeção da fila e do read model), `trace-console.md`, `load-test.md`, `observability.md`, `operations.md` (parar/resetar e troubleshooting) e `contributing.md`.
- `architecture.md` ganhou a seção nova "Mechanisms and why they exist": para outbox transacional, Rebus sobre MongoDB, read model no MongoDB, entrada assíncrona, step tracing, developer console e motor de políticas de desconto, o que faz, por que existe, qual `.doc/` o respalda e qual o custo. Texto revisado em PT-BR e aprovado antes de gravar.
- Links para âncoras antigas do README corrigidos em `docs/INDEX.md`, `docs/TEMPLATE.md`, `docs/sales.md` e `docs/conventions.md`; em `docs/discount-policies.md`, "the README rules" virou "the challenge rules".

Verificação: script de links relativos e âncoras nos 19 arquivos Markdown (README e `docs/`), 0 quebrados, com um arquivo de prova confirmando que o script detecta link e âncora ruins; script confirmando que toda linha não vazia do README antigo existe no README novo ou nos guias (as 27 diferenças são títulos, índice e números de passo); `StepKeyCoverageTests` e o trace console leem só `docs/*.md` do nível de cima, então os guias não entram nas chaves; Unit 844/844.

FILES
- `README.md` (alterado)
- `docs/guide/walkthrough.md` (novo)
- `docs/guide/configuration.md` (novo)
- `docs/guide/architecture.md` (novo)
- `docs/guide/trace-console.md` (novo)
- `docs/guide/load-test.md` (novo)
- `docs/guide/observability.md` (novo)
- `docs/guide/operations.md` (novo)
- `docs/guide/contributing.md` (novo)
- `docs/INDEX.md` (alterado)
- `docs/TEMPLATE.md` (alterado)
- `docs/sales.md` (alterado)
- `docs/conventions.md` (alterado)
- `docs/discount-policies.md` (alterado)

## Release dev → main — teste de implantação a frio (TD-011, TD-044)

Antes do PR de release, com o Docker sem containers, volumes nem imagens (as base inclusive; o dono limpou tudo), `git clone -b dev` (`696e43e`) numa pasta temporária e `make dev-up`: stack construída e no ar em 90 s, API iniciada só depois dos healthchecks do PostgreSQL e do MongoDB, 0 restarts, `/health/live` 200 no primeiro segundo. Todos os blocos `bash` do README §6 e de `docs/guide/walkthrough.md` rodaram como escritos (venda 18,00 pela política padrão, erro `DiscountAboveAllowed` idêntico ao do README, política do produto com 30 unidades a 30% = 105,00, 202 seguido de 404 a 200, filtros). As três suítes compiladas do clone passaram (844 / 71 / 19) e nenhum banco descartável sobrou. Isso fecha a verificação que ficou pendente no TD-011. A stack de dev agora roda sobre volumes novos, com os dados desse teste.

FILES
- nenhum (verificação)

## FEAT-019 — Diagnóstico em Debug e stack de debug

O que foi feito:
- `TraceBuffer` (WebApi/Tracing): buffer circular em memória que vira o `StepTrace.Sink` quando `Diagnostics:Trace:Enabled` é `true`; cada evento recebe um cursor crescente e a leitura devolve o que veio depois de um cursor. Uma flag `AsyncLocal` (`Muted`) faz o buffer ignorar eventos, e o `DiagnosticsTraceMuteMiddleware`, primeiro do pipeline, liga essa flag nas requisições a `/api/diagnostics`, para a leitura do trace não se gravar no próprio trace.
- `DiagnosticsSettings`: chaves `Diagnostics:Trace:Enabled` e `Diagnostics:Trace:Capacity`, obrigatórias (chave ausente ou inválida derruba o startup nomeando a chave). `appsettings.json`: `false`/`5000`; `appsettings.Development.json`: `true`. O DevConsole força `false` nas host settings da API hospedada, porque o buffer substituiria o `ConsoleSink`.
- `OutboxInspector` (ORM/Outbox): leitura sem tracking das linhas do outbox depois de uma `Sequence` (até 500) e do `head`.
- `DiagnosticsController`: `GET /api/diagnostics` (anônimo, `{ traceEnabled }`), `GET /api/diagnostics/outbox?after=` e `GET /api/diagnostics/trace?after=` (Admin), sem MediatR. Sem `after` devolvem só o `head`; `after` negativo ou não numérico dá 400 no corpo general-api. `payload` do outbox sai como JSON (PascalCase, como gravado).
- Tudo o que é novo em `src/` está dentro de `#if DEBUG`: em Release as rotas não existem (404), conferido no container.
- `docker-compose.debug.yml` + `make debug-up`: a API buildada em Debug com tag própria (`ambevdeveloperevaluationwebapi-debug`), trace ligado e o Admin do seed definido por âncoras YAML (a FEAT-020 reusa). `debug-up` e `dev-up` trocam o container da API um do outro.
- Documentação: `docs/guide/validation-ui.md` (novo), linhas das chaves em `docs/guide/configuration.md`, README §3/§8/§9.

Por quê: base da UI de validação guiada (FEAT-020), que precisa provar o outbox e mostrar o trace pelo navegador.

Verificação: Unit 865/865, Integration 74/74, Functional 30/30 (11 novos de diagnóstico, numa coleção xUnit não paralela porque o sink é estático); build Release sem warnings. Ao vivo: `make debug-up` → status `{"traceEnabled":true}`, outbox `{"head":3,"items":[]}`, trace com chaves AUT/CMN, `swagger.json` 200; o SQL da leitura de pendentes do relay é `SELECT o."Id", o."OccurredAt", o."Payload", o."ProcessedAt", o."Sequence", o."Type"` (prefixo que a FEAT-020 usa para esconder ciclos ociosos); `make dev-up` recriou o container com a imagem Release e `/api/diagnostics` voltou 404. `dotnet test` da Unit em Release acusa 13 falhas pré-existentes do `StepTraceTests` (pressupõem Debug), registradas como TD-045 no backlog. O comando `/cleanup-policy-doc` não está instalado nesta máquina; a edição do CLAUDE.md foi revisada à mão (duas linhas, sem duplicação).

Revisão final (revisor independente): pronto para merge com ajustes. Corrigido: `docs/guide/load-test.md` passou a pedir `--Diagnostics:Trace:Enabled=false` no `dotnet run` da medição (o Development liga o buffer e distorceria os números); `TraceBuffer.ReadAfter(long.MaxValue)` estourava `cursor + 1` e devolvia tudo (teste `Given_LargestCursor_When_Reading_Then_NothingReturned` RED→GREEN, Unit 866/866); o guia agora avisa que o trace guarda os ciclos ociosos do relay (~7 eventos a cada 500 ms, uns 6 minutos na capacidade padrão). Adiados como menores: head e itens lidos em locks separados, teste do mute cobrir também CMN-AUT/SQL, `debug-up` não carregar o `docker-compose.override.yml`, e o literal `/api/diagnostics` repetido no middleware e no controller.

FILES
- `src/Ambev.DeveloperEvaluation.WebApi/Tracing/TraceBuffer.cs` (novo)
- `src/Ambev.DeveloperEvaluation.WebApi/Tracing/DiagnosticsTraceMuteMiddleware.cs` (novo)
- `src/Ambev.DeveloperEvaluation.WebApi/Features/Diagnostics/DiagnosticsSettings.cs` (novo)
- `src/Ambev.DeveloperEvaluation.WebApi/Features/Diagnostics/DiagnosticsResponses.cs` (novo)
- `src/Ambev.DeveloperEvaluation.WebApi/Features/Diagnostics/DiagnosticsController.cs` (novo)
- `src/Ambev.DeveloperEvaluation.WebApi/Features/Diagnostics/DiagnosticsExtensions.cs` (novo)
- `src/Ambev.DeveloperEvaluation.WebApi/Program.cs` (alterado)
- `src/Ambev.DeveloperEvaluation.WebApi/appsettings.json` (alterado)
- `src/Ambev.DeveloperEvaluation.WebApi/appsettings.Development.json` (alterado)
- `src/Ambev.DeveloperEvaluation.ORM/Outbox/OutboxInspector.cs` (novo)
- `tools/Ambev.DeveloperEvaluation.DevConsole/Trace/TraceCommand.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Tracing/TraceBufferTests.cs` (novo)
- `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Tracing/DiagnosticsTraceMuteMiddlewareTests.cs` (novo)
- `tests/Ambev.DeveloperEvaluation.Unit/WebApi/Features/Diagnostics/DiagnosticsSettingsTests.cs` (novo)
- `tests/Ambev.DeveloperEvaluation.Unit/DevConsole/TraceCommandTests.cs` (alterado)
- `tests/Ambev.DeveloperEvaluation.Integration/OutboxInspectorTests.cs` (novo)
- `tests/Ambev.DeveloperEvaluation.Functional/DiagnosticsTests.cs` (novo)
- `tests/Ambev.DeveloperEvaluation.Functional/SalesApiFixture.cs` (alterado)
- `docker-compose.debug.yml` (novo)
- `Makefile` (alterado)
- `README.md` (alterado)
- `docs/guide/validation-ui.md` (novo)
- `docs/guide/configuration.md` (alterado)
- `docs/guide/load-test.md` (alterado)

## FEAT-020 — UI de validação guiada em Angular

O que foi feito:
- `tools/validation-ui/`: app Angular 22 (standalone, signals, zoneless, Vitest), fora do `.sln`. Login com o Admin do seed pré-preenchido a partir de `/config.json` (gerado de variáveis de ambiente), selo Debug/Release/unreachable pelo `GET /api/diagnostics`, e 18 cenários executáveis um a um ou em "Run all" estritamente sequencial: D1–D8 (regras de desconto da política padrão), P1–P3 (política por produto, produto+filial, política desativada) e O1–O7 (outbox, relay, read model, venda assíncrona rejeitada).
- Cada cenário cria o próprio cliente, filial e produto, lê os cursores do outbox e do trace no início e mostra: fluxo plano (nós acendem só com evidência de resposta; decisões com a resposta comprovada), passos esperado × obtido com requisição e resposta, e o painel de trace (ciclos ociosos do relay escondidos). O read model é consultado até um predicado sobre o corpo valer (nunca um 200 qualquer); `validFrom` usa o relógio da API pelo cabeçalho `Date`.
- TASK-094 (pedido do dono após o aceite): cada nó mostra acima do texto as chaves documentadas (`SAL-CRT-15` etc.), os passos ganharam a coluna **Key** (chaves do nó, ou o tópico das chamadas: `SAL-CRT`, `SAL-UPD`, `DSC-CRT`…), e a chave fica verde quando aparece no trace da execução. `ValidationUiKeyTests` (Unit) falha se a UI usar uma chave que não existe em `docs/*.md` (provado trocando uma chave por `SAL-GET-99`).
- Container: `tools/validation-ui/Dockerfile` (node:24-alpine → nginx:stable-alpine), nginx repassa `/api/` para `API_UPSTREAM` resolvendo o nome a cada 10 s (sobrevive à recriação da API) e repassa o `Date` da API; script de entrada aborta nomeando a variável que falta. Serviço `ambev.developerevaluation.ui` (porta 4280) em `docker-compose.debug.yml`, com o Admin vindo das mesmas âncoras do seed da API; `make debug-up` sobe e imprime a UI. Modo dev: `npm start` com `proxy.conf.mjs` (`API_URL`) e `scripts/write-config.mjs`.
- Conjuntos de eventos conferidos no código antes das verificações: create → SaleCreated; update → SaleModified, depois ItemCancelled por item cancelado, depois SaleCancelled (UpdateSaleHandler:198-210); delete → SaleDeleted. Igual à tabela do plano.

Por quê: dar ao avaliador uma forma conduzida, só com cliques, de ver as regras de negócio e o outbox funcionando, com a prova de cada decisão na tela.

Verificação: Vitest 55/55; build Angular limpo; .NET build 0 warnings, Unit 867/867. Aceite pelo container (`make debug-up`, http://localhost:4280): Run all → ✅ 18 ❌ 0; O7 mostra no trace a rejeição do worker (SAL-CRT-15 failures=1, "exceeds maximum of 20"); `make dev-up` → selo "API: Release · diagnostics unavailable", banner e os 18 ▶ + Run all desabilitados; container sem `UI_LOGIN_EMAIL` sai com código 1 nomeando a variável.

Revisão final (revisor independente): pronto para merge com ajustes, todos aplicados. nginx resolvia a API uma vez só (provado recriando a API em outro IP com o antigo ocupado: UI continuou 200); o trace de um cenário mostrava envios do anterior (o runner agora espera o relay processar os eventos do cenário anterior; teste RED→GREEN e, ao vivo, o trace do D2 sem SAL-DSP/SAL-PRJ); linhas expandidas passavam de um cenário a outro (teste RED→GREEN); ordem dos nós Policy → Over max; comentários de work item em membros não exportados; texto do guia; `Date` da API repassado pelo nginx. Decisão: o `.dockerignore` da raiz não terminava com quebra de linha, e a entrada nova tinha grudado na anterior — separado.

FILES
- `.gitignore` (alterado)
- `.dockerignore` (alterado)
- `docker-compose.debug.yml` (alterado)
- `Makefile` (alterado)
- `README.md` (alterado)
- `docs/guide/validation-ui.md` (alterado)
- `docs/guide/configuration.md` (alterado)
- `tests/Ambev.DeveloperEvaluation.Unit/Common/Tracing/ValidationUiKeyTests.cs` (novo)
- `tools/validation-ui/.dockerignore` (novo)
- `tools/validation-ui/.editorconfig` (novo)
- `tools/validation-ui/.gitignore` (novo)
- `tools/validation-ui/.prettierrc` (novo)
- `tools/validation-ui/Dockerfile` (novo)
- `tools/validation-ui/README.md` (novo)
- `tools/validation-ui/angular.json` (novo)
- `tools/validation-ui/nginx/10-validation-ui.sh` (novo)
- `tools/validation-ui/nginx/config.json.template` (novo)
- `tools/validation-ui/nginx/default.conf.template` (novo)
- `tools/validation-ui/package-lock.json` (novo)
- `tools/validation-ui/package.json` (novo)
- `tools/validation-ui/proxy.conf.mjs` (novo)
- `tools/validation-ui/public/favicon.ico` (novo)
- `tools/validation-ui/scripts/write-config.mjs` (novo)
- `tools/validation-ui/src/app/app.config.ts` (novo)
- `tools/validation-ui/src/app/app.routes.ts` (novo)
- `tools/validation-ui/src/app/app.ts` (novo)
- `tools/validation-ui/src/app/core/api/api-client.spec.ts` (novo)
- `tools/validation-ui/src/app/core/api/api-client.ts` (novo)
- `tools/validation-ui/src/app/core/auth/auth-guard.ts` (novo)
- `tools/validation-ui/src/app/core/auth/auth-interceptor.spec.ts` (novo)
- `tools/validation-ui/src/app/core/auth/auth-interceptor.ts` (novo)
- `tools/validation-ui/src/app/core/auth/session.ts` (novo)
- `tools/validation-ui/src/app/core/config/app-config.spec.ts` (novo)
- `tools/validation-ui/src/app/core/config/app-config.ts` (novo)
- `tools/validation-ui/src/app/core/diagnostics/diagnostics.spec.ts` (novo)
- `tools/validation-ui/src/app/core/diagnostics/diagnostics.ts` (novo)
- `tools/validation-ui/src/app/features/login/login-page.ts` (novo)
- `tools/validation-ui/src/app/features/runner/flow-strip.spec.ts` (novo)
- `tools/validation-ui/src/app/features/runner/flow-strip.ts` (novo)
- `tools/validation-ui/src/app/features/runner/runner-page.ts` (novo)
- `tools/validation-ui/src/app/features/runner/runner.spec.ts` (novo)
- `tools/validation-ui/src/app/features/runner/runner.ts` (novo)
- `tools/validation-ui/src/app/features/runner/scenario-detail.ts` (novo)
- `tools/validation-ui/src/app/features/runner/scenario-list.ts` (novo)
- `tools/validation-ui/src/app/features/runner/step-table.spec.ts` (novo)
- `tools/validation-ui/src/app/features/runner/step-table.ts` (novo)
- `tools/validation-ui/src/app/features/runner/trace-filter.spec.ts` (novo)
- `tools/validation-ui/src/app/features/runner/trace-filter.ts` (novo)
- `tools/validation-ui/src/app/features/runner/trace-panel.ts` (novo)
- `tools/validation-ui/src/app/scenarios/catalog.ts` (novo)
- `tools/validation-ui/src/app/scenarios/discount/rejection.ts` (novo)
- `tools/validation-ui/src/app/scenarios/discount/requested-discount.ts` (novo)
- `tools/validation-ui/src/app/scenarios/discount/split-lines.ts` (novo)
- `tools/validation-ui/src/app/scenarios/discount/tier.ts` (novo)
- `tools/validation-ui/src/app/scenarios/doc-keys.spec.ts` (novo)
- `tools/validation-ui/src/app/scenarios/doc-keys.ts` (novo)
- `tools/validation-ui/src/app/scenarios/fixtures.spec.ts` (novo)
- `tools/validation-ui/src/app/scenarios/fixtures.ts` (novo)
- `tools/validation-ui/src/app/scenarios/outbox-events.spec.ts` (novo)
- `tools/validation-ui/src/app/scenarios/outbox-events.ts` (novo)
- `tools/validation-ui/src/app/scenarios/outbox/change-sale.ts` (novo)
- `tools/validation-ui/src/app/scenarios/outbox/create-sale.ts` (novo)
- `tools/validation-ui/src/app/scenarios/outbox/delete-sale.ts` (novo)
- `tools/validation-ui/src/app/scenarios/outbox/outbox-steps.ts` (novo)
- `tools/validation-ui/src/app/scenarios/outbox/rejected-async.ts` (novo)
- `tools/validation-ui/src/app/scenarios/policies/branch-policy.ts` (novo)
- `tools/validation-ui/src/app/scenarios/policies/disabled-policy.ts` (novo)
- `tools/validation-ui/src/app/scenarios/policies/policy-steps.ts` (novo)
- `tools/validation-ui/src/app/scenarios/policies/product-policy.ts` (novo)
- `tools/validation-ui/src/app/scenarios/run-context.spec.ts` (novo)
- `tools/validation-ui/src/app/scenarios/run-context.ts` (novo)
- `tools/validation-ui/src/app/scenarios/scenario.ts` (novo)
- `tools/validation-ui/src/app/scenarios/server-clock.spec.ts` (novo)
- `tools/validation-ui/src/app/scenarios/server-clock.ts` (novo)
- `tools/validation-ui/src/app/shared/poll-until.spec.ts` (novo)
- `tools/validation-ui/src/app/shared/poll-until.ts` (novo)
- `tools/validation-ui/src/index.html` (novo)
- `tools/validation-ui/src/main.ts` (novo)
- `tools/validation-ui/src/styles.css` (novo)
- `tools/validation-ui/tsconfig.app.json` (novo)
- `tools/validation-ui/tsconfig.json` (novo)
- `tools/validation-ui/tsconfig.spec.json` (novo)

## TASK-095 — Demonstração do cadastro da matriz de descontos na UI

Nova página `/matrix` ("Discount matrix" na barra do topo) da UI de validação. Ao abrir, cria cliente, filial e produto próprios; toda política cadastrada ali fica restrita a esse produto, então os 18 cenários e as demais vendas não são afetados. Mostra a política padrão e as políticas cadastradas (#1, #2...), um formulário (máximo por produto e três faixas de/até/%, início = relógio da API + 2 s) e a verificação: para cada quantidade informada faz uma venda real e mostra a política que precificou, o teto, o desconto e o total, ou o erro (`QuantityLimitExceeded`, etc.). Por quê: permitir ao tester validar um conjunto de configurações de desconto de forma simples, sem reimplementar a regra no front (o resultado vem da API). Escopo reduzido a pedido (sem escopos de filial, sem nome de política, sem desativação). Verificado ao vivo: #1 (5–9 → 15%, 10–30 → 25%) e depois #2 (10–30 → 30%) — a mais recente vence; 31 unidades → 400 com máximo 30. Durante a verificação, um salto do relógio da VM do OrbStack (log da API com horário voltando ~1,5 s) fez uma checagem cair no Default; não é defeito do código.

FILES
- tools/validation-ui/src/app/features/matrix/matrix.ts (novo)
- tools/validation-ui/src/app/features/matrix/matrix.spec.ts (novo)
- tools/validation-ui/src/app/features/matrix/matrix-page.ts (novo)
- tools/validation-ui/src/app/features/matrix/matrix-page.spec.ts (novo)
- tools/validation-ui/src/app/app.routes.ts (alterado)
- tools/validation-ui/src/app/app.ts (alterado)
- tools/validation-ui/src/styles.css (alterado)
- docs/guide/validation-ui.md (alterado)

## TD-046 — README: decisão de não usar Redis e onde caberia o cache de políticas

Registrado no README (§1) que o Redis sobe com a stack mas não é usado por decisão, e onde um cache se aplicaria: na matriz de descontos, como decorator de `IDiscountPolicyRepository.GetApplicableAsync` (a única leitura por trás do `DiscountPolicyResolver`), e não estendendo o resolver, que é classe concreta do Domain e não deve conhecer cache. O cache guardaria todas as políticas, inclusive desativadas (a edição reprecifica pela data original), e seria limpo por DSC-CRT e DSC-DIS; IMemoryCache para uma instância, Redis para várias (FEAT-008, mantido como `wont`). Por quê: deixar a decisão e o ponto de extensão explícitos para quem avaliar o projeto.

FILES
- README.md (alterado)

## TD-047 — Tela da matriz citada no índice de guias do README e no README da UI

O índice de guias do README (§9) descrevia o `validation-ui.md` só com os 18 cenários e as rotas de diagnóstico, e o `tools/validation-ui/README.md` dizia apenas que o app roda os cenários contra uma API em Debug. Os dois passaram a citar a tela Discount matrix (TASK-095), que cadastra políticas e as verifica com vendas reais e funciona também com a API em Release. Por quê: revisão da documentação após a TASK-095 e o TD-046.

FILES
- README.md (alterado)
- tools/validation-ui/README.md (alterado)

## TD-048 — Log de implementação publicado como IMPLEMENTACAO.md

Cópia deste log de implementação na raiz do repositório com o nome `IMPLEMENTACAO.md`, para que quem avalia o projeto leia no GitHub o histórico das mudanças: o que foi feito em cada item, por quê e quais arquivos foram tocados. O log de trabalho continua local; a cópia na raiz é refeita a partir dele quando for atualizada.

FILES
- IMPLEMENTACAO.md (novo)

## Release v1.0.1 — dev → main (PRs #36 a #40)

PR #41 (`dev` → `main`, merge commit `ee05e58`) levou ao `main` o diagnóstico em Debug (FEAT-019), a UI de validação guiada (FEAT-020), a tela da matriz de descontos (TASK-095) e os ajustes de documentação (TD-046, TD-047, TD-048). Tag anotada `v1.0.1` no merge commit. Verificado antes, no `dev` em `004160e`: build Debug e Release com 0 avisos, Unit 867, Integration 74, Functional 30, UI 67, nenhuma mudança de modelo pendente. O teste de implantação a frio da release v1.0.0 não foi repetido: o stack do `make dev-up` não mudou nessas PRs.

FILES
- nenhum (release)

## TD-049 — coverlet.msbuild nos projetos de teste Integration e Functional

Os scripts `coverage-report.sh`/`.bat` coletam cobertura com `/p:CollectCoverage=true`, que depende do pacote `coverlet.msbuild`. Só o projeto Unit o referenciava, então o relatório cobria apenas os testes unitários: Integration e Functional rodavam sem gerar `coverage.cobertura.xml` (reproduzido antes da mudança). Os dois passaram a referenciar `coverlet.msbuild` 6.0.2, com os mesmos `IncludeAssets`/`PrivateAssets` do `coverlet.collector` ao lado. Verificado: com o pacote, cada projeto gera o arquivo (Integration 74 testes, 58,69% de linhas; Functional 30 testes, 70,14%), e a solução compila sem avisos. Os outros pontos dos scripts (vírgulas do `/p:Exclude` sem escape no `.sh`, `pause` no `.sh`, build em Release redundante) ficaram fora, a pedido.

FILES
- tests/Ambev.DeveloperEvaluation.Integration/Ambev.DeveloperEvaluation.Integration.csproj (alterado)
- tests/Ambev.DeveloperEvaluation.Functional/Ambev.DeveloperEvaluation.Functional.csproj (alterado)
