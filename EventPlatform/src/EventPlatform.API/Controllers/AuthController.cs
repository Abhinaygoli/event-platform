using static System.Net.Mime.MediaTypeNames;
using EventPlatform.Application.DTOs.Auth;
using EventPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
namespace EventPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _config;
        public AuthController(IAuthService authService, IConfiguration config)
        {
            _authService = authService;
            _config = config;
        }

        /// <summary>
        /// Register a new user.
        /// Returns: access token in JSON body.
        /// Sets: refresh token as HttpOnly cookie (invisible to JS).
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var (response, refreshToken) = await _authService.RegisterAsync(dto);
                SetRefreshTokenCookie(refreshToken);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Login existing user.
        /// Returns: access token in JSON body.
        /// Sets: refresh token as HttpOnly cookie (invisible to JS).
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var (response, refreshToken) = await _authService.LoginAsync(dto);
                SetRefreshTokenCookie(refreshToken);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Silently refresh the access token.
        /// Reads refresh token from HttpOnly cookie — browser sends
        /// it automatically, JS never touches it.
        /// Returns: new access token in JSON.
        /// Sets: new rotated refresh token cookie.
        /// No request body needed.
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            // Read from cookie — not from body
            var refreshToken = Request.Cookies["ep_refresh_token"];

            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new { message = "No refresh token cookie found." });

            try
            {
                var (response, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken);
                SetRefreshTokenCookie(newRefreshToken); // rotated — new cookie
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                DeleteRefreshTokenCookie(); // clear invalid cookie
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ── Logout ────────────────────────────────────────────────
        /// <summary>
        /// Logout: revokes refresh token in DB + deletes cookie.
        /// Even if attacker captured the cookie value it no longer works.
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["ep_refresh_token"];

            if (!string.IsNullOrEmpty(refreshToken))
                await _authService.RevokeRefreshTokenAsync(refreshToken);

            DeleteRefreshTokenCookie();
            return Ok(new { message = "Logged out successfully." });
        }

        // ── Cookie helpers ────────────────────────────────────────
        private void SetRefreshTokenCookie(string refreshToken)
        {
            var envFromConfig = _config["ASPNETCORE_ENVIRONMENT"];
            var envFromEnvVar = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isProd = string.Equals(envFromConfig, "Production", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(envFromEnvVar, "Production", StringComparison.OrdinalIgnoreCase);

            // Safely parse refresh expiry days from configuration (default to 7 days)
            double refreshExpiryDays = 7;
            var refreshExpiryValue = _config["JwtSettings:RefreshExpiryDays"];
            if (!string.IsNullOrEmpty(refreshExpiryValue) && double.TryParse(refreshExpiryValue, out var parsedDays))
            {
                refreshExpiryDays = parsedDays;
            }

            Response.Cookies.Append("ep_refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,   // JS cannot read this — prevents XSS theft
                Secure = isProd, // HTTPS only in production
                SameSite = isProd
                    ? SameSiteMode.None  // required for cross-origin on Render
                    : SameSiteMode.Lax,  // relaxed for localhost development
                Expires = DateTime.UtcNow.AddDays(refreshExpiryDays),
                Path = "/api/auth"   // cookie only sent to /api/auth/* routes
            });
        }

        private void DeleteRefreshTokenCookie()
        {
            var envFromConfig = _config["ASPNETCORE_ENVIRONMENT"];
            var envFromEnvVar = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isProd = string.Equals(envFromConfig, "Production", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(envFromEnvVar, "Production", StringComparison.OrdinalIgnoreCase);

            // Setting an expired date deletes the cookie immediately in the browser
            Response.Cookies.Delete("ep_refresh_token", new CookieOptions
            {
                HttpOnly = true,
                Secure = isProd,
                SameSite = isProd ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/api/auth"
            });
        }
    }
}
