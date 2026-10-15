using AssetIQ.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AssetIQ.Web.Controllers;

public class SettingsController : Controller
{
    public IActionResult Index()
    {
        ViewData["ActiveModule"] = "settings";
        ViewData["PageTitle"] = "Settings";
        return View("ModulePlaceholder", ModulePageFactory.Create("settings"));
    }
}
