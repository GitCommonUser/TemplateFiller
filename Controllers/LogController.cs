
using Microsoft.AspNetCore.Mvc;
using TemplateFiller.Data;
using Microsoft.EntityFrameworkCore;

namespace TemplateFiller.Controllers;

public class LogController : Controller
{
    private readonly TemplateFillerContext _context;

    public LogController(TemplateFillerContext context)
    {
        _context = context;
    }
    public async Task<IActionResult> Index()
    {
        var logs = await _context.Logs.OrderByDescending(l => l.Date).ToListAsync();
        return View(logs);
    }

    [HttpPost]
    public async Task<IActionResult> ClearLogs()
    {
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Logs");
        return RedirectToAction("Index");
    }
}