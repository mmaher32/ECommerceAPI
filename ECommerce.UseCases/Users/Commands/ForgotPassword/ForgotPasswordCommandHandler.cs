using ECommerce.Domain.Shared;
using ECommerce.UseCases.Shared.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Users.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IIdentityService identityService,
    ILogger<ForgotPasswordCommandHandler> logger) :
    IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var tokenResult = await identityService.GeneratePasswordResetTokenAsync(
            request.Email, cancellationToken);

        if (tokenResult.IsFailure)
        {
            // Deliberately swallowed. Returning the failure would tell the caller which
            // addresses are registered -- account enumeration. Log it, answer success.
            logger.LogInformation(
                "Password reset requested for an address that cannot receive one. {ErrorCode}",
                tokenResult.Error!.Code);

            return Result.Success();
        }

        // There is no email service, so this log is the only place the token is visible.
        // Debug level on purpose: a reset token in production logs lets anyone with log
        // access take over the account. appsettings.Development.json enables Debug for
        // this class only, so it shows on the local console and nowhere else.
        logger.LogDebug(
            "Password reset token for {Email}: {Token}", request.Email, tokenResult.Value);

        return Result.Success();
    }
}
