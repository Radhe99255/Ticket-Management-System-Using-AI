using Microsoft.AspNetCore.Mvc;
using TicketManagement.Web.Models;
using TicketManagement.Web.Services;

namespace TicketManagement.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If already authenticated, redirect to appropriate dashboard
            if (_authService.IsAuthenticated())
            {
                if (_authService.IsAdmin())
                    return RedirectToAction("Index", "Admin");
                else
                    return RedirectToAction("Index", "User");
            }
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _authService.LoginAsync(model);
                
                if (result)
                {
                    if (_authService.IsAdmin())
                        return RedirectToAction("Index", "Admin");
                    else
                        return RedirectToAction("Index", "User");
                }
                
                ModelState.AddModelError("", "Invalid email or password");
            }
            
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            // If already authenticated, redirect to appropriate dashboard
            if (_authService.IsAuthenticated())
            {
                if (_authService.IsAdmin())
                    return RedirectToAction("Index", "Admin");
                else
                    return RedirectToAction("Index", "User");
            }
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _authService.RegisterAsync(model);
                
                if (result)
                {
                    return RedirectToAction("Index", "User");
                }
                
                ModelState.AddModelError("", "Registration failed. Email may already be in use.");
            }
            
            return View(model);
        }

        [HttpPost]
        public IActionResult Logout()
        {
            _authService.Logout();
            return RedirectToAction("Login", "Account");
        }
    }
} 