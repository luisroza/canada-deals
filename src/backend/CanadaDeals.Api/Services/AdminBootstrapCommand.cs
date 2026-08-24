using System.ComponentModel.DataAnnotations;
using CanadaDeals.Api.Security;
using CanadaDeals.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CanadaDeals.Api.Services;

public static class AdminBootstrapCommand
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        Console.Write("Owner admin email: ");
        var email = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
            throw new InvalidOperationException("A valid owner admin email is required.");

        var password = ReadConfirmedPassword();

        if (!await roles.RoleExistsAsync(AdminAccess.OwnerRole))
        {
            var roleResult = await roles.CreateAsync(new IdentityRole<Guid>(AdminAccess.OwnerRole));
            EnsureSucceeded(roleResult, "Owner admin role creation failed.");
        }

        var existingOwners = await users.GetUsersInRoleAsync(AdminAccess.OwnerRole);
        if (existingOwners.Any(existing => !string.Equals(existing.NormalizedEmail, users.NormalizeEmail(email), StringComparison.Ordinal)))
            throw new InvalidOperationException("A different owner administrator is already configured. No changes were made.");

        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { Id = Guid.NewGuid(), Email = email, UserName = email, EmailConfirmed = true };
            EnsureSucceeded(await users.CreateAsync(user, password), "Owner admin account creation failed.");
        }
        else
        {
            var resetToken = await users.GeneratePasswordResetTokenAsync(user);
            EnsureSucceeded(await users.ResetPasswordAsync(user, resetToken, password), "Owner admin password update failed.");
            if (!user.EmailConfirmed)
            {
                var confirmationToken = await users.GenerateEmailConfirmationTokenAsync(user);
                EnsureSucceeded(await users.ConfirmEmailAsync(user, confirmationToken), "Owner admin email confirmation failed.");
            }
        }

        if (!await users.IsInRoleAsync(user, AdminAccess.OwnerRole))
            EnsureSucceeded(await users.AddToRoleAsync(user, AdminAccess.OwnerRole), "Owner admin authorization failed.");

        EnsureSucceeded(await users.UpdateSecurityStampAsync(user), "Owner admin security-stamp update failed.");
        Console.WriteLine($"Owner administrator configured for user ID {user.Id}. Existing sessions were invalidated.");
        cancellationToken.ThrowIfCancellationRequested();
    }

    public static async Task ResetPasswordAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (!await roles.RoleExistsAsync(AdminAccess.OwnerRole))
            throw new InvalidOperationException("No owner administrator is configured. Run --bootstrap-owner-admin first.");

        var existingOwners = await users.GetUsersInRoleAsync(AdminAccess.OwnerRole);
        if (existingOwners.Count == 0)
            throw new InvalidOperationException("No owner administrator is configured. Run --bootstrap-owner-admin first.");
        if (existingOwners.Count != 1)
            throw new InvalidOperationException("More than one owner administrator is configured. Password reset was refused; review the database roles manually.");

        var owner = existingOwners[0];
        Console.WriteLine($"Resetting the password for owner administrator user ID {owner.Id}.");
        var password = ReadConfirmedPassword();

        var resetToken = await users.GeneratePasswordResetTokenAsync(owner);
        EnsureSucceeded(await users.ResetPasswordAsync(owner, resetToken, password), "Owner admin password reset failed.");
        EnsureSucceeded(await users.UpdateSecurityStampAsync(owner), "Owner admin security-stamp update failed.");

        Console.WriteLine("Owner administrator password reset completed. Existing sessions were invalidated.");
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static string ReadConfirmedPassword()
    {
        Console.Write("New owner admin password (input is hidden): ");
        var password = ReadSecret();
        Console.WriteLine();
        Console.Write("Confirm new owner admin password (input is hidden): ");
        var confirmation = ReadSecret();
        Console.WriteLine();

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("A password is required.");
        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
            throw new InvalidOperationException("The passwords do not match. No changes were made.");

        return password;
    }

    private static string ReadSecret()
    {
        var characters = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (characters.Count > 0) characters.RemoveAt(characters.Count - 1);
                continue;
            }
            if (!char.IsControl(key.KeyChar) && characters.Count < 128) characters.Add(key.KeyChar);
        }
        return new string(characters.ToArray());
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (result.Succeeded) return;
        var details = string.Join(" ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"{message} {details}");
    }
}
