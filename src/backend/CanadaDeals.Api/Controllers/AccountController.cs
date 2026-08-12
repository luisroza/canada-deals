using CanadaDeals.Api.Contracts;
using CanadaDeals.Api.Services;
using CanadaDeals.Infrastructure.Email;
using CanadaDeals.Infrastructure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("api/v1/account")]
public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAntiforgery antiforgery,
    IWebHostEnvironment environment,
    IOptions<TransactionalEmailOptions> emailOptions,
    AccountConfirmationEmailService confirmationEmail,
    ILogger<AccountController> logger) : ControllerBase
{
    [HttpGet("antiforgery")]
    [ProducesResponseType<AntiforgeryTokenResponse>(StatusCodes.Status200OK)]
    public ActionResult<AntiforgeryTokenResponse> AntiforgeryToken()
    {
        Response.Headers.CacheControl = "no-store";
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new AntiforgeryTokenResponse(tokens.RequestToken!));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("authentication")]
    [ProducesResponseType<AccountMutationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim();
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = email, UserName = email };
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            logger.LogWarning("Account registration was rejected.");
            if (result.Errors.Any(x => x.Code.StartsWith("Duplicate", StringComparison.Ordinal)))
                return BadRequest(new ProblemDetails { Title = "Account creation failed", Detail = "Unable to create an account with these details." });

            foreach (var error in result.Errors)
                ModelState.AddModelError(nameof(request.Password), error.Description);
            return ValidationProblem(ModelState);
        }

        var authenticated = false;
        if ((environment.IsDevelopment() || environment.IsEnvironment("Test")) && emailOptions.Value.AutoConfirmDevelopmentAccounts)
        {
            var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmation = await userManager.ConfirmEmailAsync(user, confirmationToken);
            if (!confirmation.Succeeded)
                throw new InvalidOperationException("Development account confirmation failed.");

            await signInManager.SignInAsync(user, isPersistent: false);
            authenticated = true;
        }
        else
        {
            await confirmationEmail.SendAsync(user, HttpContext.RequestAborted);
        }

        logger.LogInformation("Account {UserId} was created. Development confirmation: {DevelopmentConfirmation}.", user.Id, authenticated);
        var message = authenticated
            ? "Account created and signed in for this Development/Test environment."
            : "Account created. Confirm your email before signing in.";
        return StatusCode(StatusCodes.Status201Created, new AccountMutationResponse(message, authenticated));
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("authentication")]
    [ProducesResponseType<EmailConfirmationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<EmailConfirmationResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request)
    {
        var result = await confirmationEmail.ConfirmAsync(request.UserId, request.Code);
        return result switch
        {
            AccountConfirmationResult.Confirmed => Ok(new EmailConfirmationResponse("CONFIRMED", "Your email is confirmed. You can now sign in.")),
            AccountConfirmationResult.AlreadyConfirmed => Ok(new EmailConfirmationResponse("ALREADY_CONFIRMED", "Your email is already confirmed. You can sign in.")),
            _ => BadRequest(new EmailConfirmationResponse("INVALID_OR_EXPIRED", "This confirmation link is invalid or has expired."))
        };
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("authentication")]
    [ProducesResponseType<AccountMutationResponse>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is { EmailConfirmed: false })
            await confirmationEmail.SendAsync(user, HttpContext.RequestAborted);

        logger.LogInformation("An account confirmation resend request was accepted.");
        return Accepted(new AccountMutationResponse("If an unconfirmed account exists for that address, a confirmation email has been sent.", false));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("authentication")]
    [ProducesResponseType<AccountMutationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await signInManager.PasswordSignInAsync(request.Email.Trim(), request.Password, isPersistent: false, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            logger.LogWarning("Account sign-in failed.");
            return Unauthorized(new ProblemDetails { Title = "Sign-in failed", Detail = "Invalid email or password." });
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        logger.LogInformation("Account {UserId} signed in.", user?.Id);
        return Ok(new AccountMutationResponse("Signed in.", true));
    }

    [HttpPost("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userId = userManager.GetUserId(User);
        await signInManager.SignOutAsync();
        logger.LogInformation("Account {UserId} signed out.", userId);
        return NoContent();
    }

    [HttpGet("me")]
    [AllowAnonymous]
    [ProducesResponseType<AccountSessionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AccountSessionResponse>> Me()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Ok(new AccountSessionResponse(false, null, false));

        var user = await userManager.GetUserAsync(User);
        return Ok(new AccountSessionResponse(true, User.Identity.Name, user?.EmailConfirmed == true));
    }
}
