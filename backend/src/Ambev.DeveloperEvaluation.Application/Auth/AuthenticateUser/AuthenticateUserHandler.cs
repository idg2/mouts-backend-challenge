using System.Threading;
using System.Threading.Tasks;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Common.Tracing;
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

        // Work item: TASK-035 (FEAT-016), TASK-048 (FEAT-017)
        public async Task<AuthenticateUserResult> Handle(AuthenticateUserCommand request, CancellationToken cancellationToken)
        {
            StepTrace.Step("CMN-PIP-10", "Handler validates the command and runs the use case", [("request", nameof(AuthenticateUserCommand))]);
            StepTrace.Step("AUT-LGN-03", "Find the user by e-mail", [("hasEmail", !string.IsNullOrEmpty(request.Email))]);
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            StepTrace.Step("AUT-LGN-04", "User found?", [("found", user != null), ("userId", user?.Id)]);

            // The e-mail is never logged; RequestId and the request log's RemoteIpAddress identify the attempt.
            if (user == null)
            {
                _logger.LogWarning("Authentication failed: unknown user");
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var matches = _passwordHasher.VerifyPassword(request.Password, user.Password);
            StepTrace.Step("AUT-LGN-05", "Password matches the BCrypt hash?", [("matches", matches), ("userId", user.Id)]);
            if (!matches)
            {
                _logger.LogWarning("Authentication failed: wrong password for user {UserId}", user.Id);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var activeUserSpec = new ActiveUserSpecification();
            StepTrace.Step("AUT-LGN-06", "User is Active?", [("active", activeUserSpec.IsSatisfiedBy(user)), ("status", user.Status), ("userId", user.Id)]);
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
