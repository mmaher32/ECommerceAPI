using Asp.Versioning;
using Asp.Versioning.Builder;
using ECommerce.API.Extensions;
using ECommerce.API.Filters;
using ECommerce.API.Models;
using ECommerce.API.Security;
using ECommerce.UseCases.Users.Commands.ChangePassword;
using ECommerce.UseCases.Users.Commands.ForgotPassword;
using ECommerce.UseCases.Users.Commands.Login;
using ECommerce.UseCases.Users.Commands.Logout;
using ECommerce.UseCases.Users.Commands.Refresh;
using ECommerce.UseCases.Users.Commands.Register;
using ECommerce.UseCases.Users.Commands.ResetPassword;
using ECommerce.UseCases.Users.Dtos;
using ECommerce.UseCases.Users.Queries.GetCurrentUser;
using MediatR;

namespace ECommerce.API.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints,
        ApiVersionSet apiVersionSet)
    {
        var group = endpoints
            .MapGroup("/api/v{version:apiVersion}/users")
            .WithTags("Users")
            .WithApiVersionSet(apiVersionSet)
            .HasApiVersion(new ApiVersion(1, 0))
            .AddEndpointFilter<AuditEndpointFilter>();

        group.MapPost("/register", async (
            RegisterRequest request,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new RegisterCommand(request.Email, request.Password, request.DisplayName),
                cancellationToken);

            return result.FromResult(httpContext, "Registered successfully");
        })
        .RequireRateLimiting(RateLimitPolicies.Auth)
        .WithSummary("Registers a new user")
        .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/login", async (
            LoginRequest request,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new LoginCommand(request.Email, request.Password),
                cancellationToken);

            return result.FromResult(httpContext, "Logged in successfully");
        })
        .RequireRateLimiting(RateLimitPolicies.Auth)
        .WithSummary("Signs a user in")
        .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", async (
            RefreshRequest request,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new RefreshCommand(request.RefreshToken), cancellationToken);

            return result.FromResult(httpContext, "Token refreshed successfully");
        })
        .RequireRateLimiting(RateLimitPolicies.Auth)
        .WithSummary("Exchanges a refresh token for a new token pair")
        .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", async (
            RefreshRequest request,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new LogoutCommand(request.RefreshToken), cancellationToken);

            return result.FromResult(httpContext, "Logged out successfully");
        })
        .RequireAuthorization()
        .WithSummary("Revokes the supplied refresh token");

        group.MapGet("/me", async (
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = httpContext.User.GetUserId();

            if (userId.IsFailure)
                return userId.Problem(httpContext);

            var result = await sender.Send(
                new GetCurrentUserQuery(userId.Value), cancellationToken);

            return result.FromResult(httpContext, "User retrieved successfully");
        })
        .RequireAuthorization()
        .WithSummary("Returns the signed-in user")
        .Produces<ApiResponse<UserResponse>>(StatusCodes.Status200OK);

        group.MapPost("/change-password", async (
            ChangePasswordRequest request,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = httpContext.User.GetUserId();

            if (userId.IsFailure)
                return userId.Problem(httpContext);

            var result = await sender.Send(
                new ChangePasswordCommand(
                    userId.Value, request.CurrentPassword, request.NewPassword),
                cancellationToken);

            return result.FromResult(httpContext, "Password changed successfully");
        })
        .RequireAuthorization()
        .RequireRateLimiting(RateLimitPolicies.Auth)
        .WithSummary("Changes the signed-in user's password");

        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ForgotPasswordCommand(request.Email), cancellationToken);

            return result.FromResult(
                httpContext,
                "If that address is registered, a reset link has been sent.");
        })
        .RequireRateLimiting(RateLimitPolicies.Auth)
        .WithSummary("Starts a password reset")
        .WithDescription("Always succeeds, so the response cannot be used to discover registered addresses.");

        group.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ResetPasswordCommand(request.Email, request.Token, request.NewPassword),
                cancellationToken);

            return result.FromResult(httpContext, "Password reset successfully");
        })
        .RequireRateLimiting(RateLimitPolicies.Auth)
        .WithSummary("Completes a password reset");

        return endpoints;
    }
}
