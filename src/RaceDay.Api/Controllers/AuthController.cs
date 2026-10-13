using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;
    private readonly IConfiguration _config;

    public AuthController(RaceDayDbContext db, IPasswordHasher<AppUser> hasher, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _config = config;
    }

    /// <summary>Registers a new account as an Organiser or a Participant.</summary>
    /// <remarks>
    /// Body: fullName, email, password (min 8 chars), role ("Organiser" or "Participant"), optional phoneNumber.
    /// Organisers must also supply the organiserCode issued by RaceDay, so that anyone cannot simply
    /// self-register as an Organiser. The password is hashed before it is stored.
    /// </remarks>
    /// <response code="201">Account created. Returns the new user (without password).</response>
    /// <response code="400">Validation failed (missing fields, bad email, short password, invalid role).</response>
    /// <response code="403">Organiser code missing or incorrect.</response>
    /// <response code="409">Email address is already registered.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        if (!Roles.IsValid(request.Role))
            return BadRequest(new ErrorResponse("Role must be 'Organiser' or 'Participant'."));

        if (request.Role == Roles.Organiser)
        {
            var expected = _config["Registration:OrganiserCode"];
            if (string.IsNullOrEmpty(expected) || request.OrganiserCode != expected)
                return Forbidden("A valid organiser code is required to register as an Organiser.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new ErrorResponse("An account with this email already exists."));

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Role = request.Role,
            PhoneNumber = request.PhoneNumber
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, UserResponse.From(user));
    }

    /// <summary>Logs in and starts a session.</summary>
    /// <remarks>
    /// Body: email and password. On success the server sets a session cookie that carries the user's
    /// id and role; it is sent automatically on all following requests.
    /// </remarks>
    /// <response code="200">Logged in. Returns the user including their role.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Email == email);

        if (user == null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ErrorResponse("Invalid email or password."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(UserResponse.From(user));
    }

    /// <summary>Logs out and ends the current session.</summary>
    /// <response code="204">Session ended.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
