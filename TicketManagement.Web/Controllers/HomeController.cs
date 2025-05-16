using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Web.Models;
using TicketManagement.Web.Services;

namespace TicketManagement.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAuthService _authService;

    public HomeController(ILogger<HomeController> logger, IAuthService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    public IActionResult Index()
    {
        // Redirect to login if not authenticated
        if (!_authService.IsAuthenticated())
            return RedirectToAction("Login", "Account");

        // Redirect based on user role
        if (_authService.IsAdmin())
            return RedirectToAction("Index", "Admin");
        else
            return RedirectToAction("Index", "User");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
