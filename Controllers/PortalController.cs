using Graduation_Project_Backend.Extensions;
using Graduation_Project_Backend.Filters;
using Graduation_Project_Backend.Service.Auth;
using Graduation_Project_Backend.Service.Common;
using Graduation_Project_Backend.Service.Portal;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [Route("portal")]
    public sealed class PortalController : Controller
    {
        private readonly IPortalAuthService _portalAuthService;

        public PortalController(IPortalAuthService portalAuthService)
        {
            _portalAuthService = portalAuthService;
        }

        [HttpGet("login")]
        public IActionResult Login([FromQuery] string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new PortalLoginRequest());
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(PortalLoginRequest request, [FromForm] string? returnUrl, CancellationToken cancellationToken)
        {
            try
            {
                PortalLoginResult result = await _portalAuthService.LoginAsync(request, cancellationToken);
                Response.Cookies.Append(PortalConstants.SessionCookieName, result.SessionId, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return Redirect(IsAdmin(result.Role) ? "/admin" : "/manager");
            }
            catch (AuthException ex)
            {
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["ErrorMessage"] = ex.Message;
                return View(request);
            }
            catch (ApiException ex)
            {
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["ErrorMessage"] = ex.Message;
                return View(request);
            }
        }

        [HttpGet("register-admin")]
        public IActionResult RegisterAdmin()
            => View(new PortalRegisterAdminRequest());

        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin(PortalRegisterAdminRequest request, CancellationToken cancellationToken)
        {
            try
            {
                PortalLoginResult result = await _portalAuthService.RegisterAdminAsync(request, cancellationToken);
                Response.Cookies.Append(PortalConstants.SessionCookieName, result.SessionId, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                return Redirect("/admin");
            }
            catch (AuthException ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View(request);
            }
            catch (ApiException ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View(request);
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            string? sessionId = Request.Cookies[PortalConstants.SessionCookieName];
            await _portalAuthService.LogoutAsync(sessionId, cancellationToken);
            Response.Cookies.Delete(PortalConstants.SessionCookieName);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet("account")]
        [PortalAuthorize(PortalConstants.AdminRole, PortalConstants.ManagerRole)]
        public async Task<IActionResult> Account(CancellationToken cancellationToken)
        {
            Guid userId = HttpContext.GetCurrentUserSession().UserId;
            return View(await _portalAuthService.GetAccountAsync(userId, cancellationToken));
        }

        [HttpPost("account")]
        [PortalAuthorize(PortalConstants.AdminRole, PortalConstants.ManagerRole)]
        public async Task<IActionResult> Account(PortalAccountRequest request, CancellationToken cancellationToken)
        {
            try
            {
                Guid userId = HttpContext.GetCurrentUserSession().UserId;
                await _portalAuthService.UpdateAccountAsync(userId, request, cancellationToken);
                TempData["SuccessMessage"] = "Account updated successfully.";
                return RedirectToAction(nameof(Account));
            }
            catch (AuthException ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View(request);
            }
            catch (ApiException ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View(request);
            }
        }

        [HttpGet("denied")]
        public IActionResult Denied()
            => View();

        private static bool IsAdmin(string role)
            => string.Equals(role, PortalConstants.AdminRole, StringComparison.OrdinalIgnoreCase);
    }
}
