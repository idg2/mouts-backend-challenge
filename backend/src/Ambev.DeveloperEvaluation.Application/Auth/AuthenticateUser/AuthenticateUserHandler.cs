using System.Threading;
using System.Threading.Tasks;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Specifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser
{
    public class AuthenticateUserHandler : IRequestHandler<AuthenticateUserCommand, AuthenticateUserResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        // Work item: TASK-035 (FEAT-016)
        private readonly ILogger<AuthenticateUserHandler> _logger;

        // Work item: TASK-035 (FEAT-016)
        public AuthenticateUserHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            ILogger<AuthenticateUserHandler> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _logger = logger;
        }

        // Work item: TASK-035 (FEAT-016)
        public async Task<AuthenticateUserResult> Handle(AuthenticateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            // The e-mail is never logged; RequestId and the request log's RemoteIpAddress identify the attempt.
            if (user == null)
            {
                _logger.LogWarning("Authentication failed: unknown user");
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            if (!_passwordHasher.VerifyPassword(request.Password, user.Password))
            {
                _logger.LogWarning("Authentication failed: wrong password for user {UserId}", user.Id);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var activeUserSpec = new ActiveUserSpecification();
            if (!activeUserSpec.IsSatisfiedBy(user))
            {
                _logger.LogWarning("Authentication failed: user {UserId} is not active", user.Id);
                throw new UnauthorizedAccessException("User is not active");
            }

            var token = _jwtTokenGenerator.GenerateToken(user);
            _logger.LogInformation("User {UserId} authenticated", user.Id);

            return new AuthenticateUserResult
            {
                Token = token,
                Email = user.Email,
                Name = user.Username,
                Role = user.Role.ToString()
            };
        }
    }
}
