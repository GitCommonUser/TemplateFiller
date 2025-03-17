using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemplateFiller.Data;
using TemplateFiller.Models;
using TemplateFiller.ViewModels;
using TemplateFiller.Services;

namespace TemplateFiller.Controllers;

public class TemplateController : Controller
{
    private readonly TemplateFillerContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogService _logService;

    public TemplateController(TemplateFillerContext context, IWebHostEnvironment environment, ILogService logService)
    {
        _context = context;
        _environment = environment;
        _logService = logService;
    }
    public async Task<IActionResult> Index()
    {
        var templates = await _context.Templates.ToListAsync();
        return View(templates);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTemplateViewModel model)
    {
        if (ModelState.IsValid)
        {
            if (model.File.ContentType != "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                ModelState.AddModelError("File", "Файл должен быть в формате .docx !");
                return View(model);
            }

            var fileName = $"template_{DateTime.Now:yyyyMMddHHmmss}.docx";
            var filePath = Path.Combine(_environment.WebRootPath, "uploads", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            var template = new Template
            {
                Name = model.Name,
                FilePath = Path.Combine("uploads", fileName)
            };

            _context.Templates.Add(template);
            await _context.SaveChangesAsync();

            await _logService.LogAsync($"Создан шаблон {template.Name}");

            return RedirectToAction("Index");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var template = await _context.Templates.FindAsync(id);
        if (template == null)
        {
            return NotFound();
        }

        var filePath = Path.Combine(_environment.WebRootPath, template.FilePath);
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        _context.Templates.Remove(template);
        await _context.SaveChangesAsync();

        await _logService.LogAsync($"Удален шаблон {template.Name}");

        return RedirectToAction("Index");
    }

    public IActionResult Details(int id)
    {
        var template = _context.Templates.Find(id);
        if (template == null)
        {
            return NotFound();
        }

        var filePath = Path.Combine(_environment.WebRootPath, template.FilePath);

        string fileContent = "";
        if (System.IO.File.Exists(filePath))
        {
            using (var doc = Xceed.Words.NET.DocX.Load(filePath))
            {
                foreach (var paragraph in doc.Paragraphs)
                {
                    fileContent += paragraph.Text + "\n"; 
                }
            }
        }
        else
        {
            fileContent = "Файл шаблона не найден !";
        }

        ViewData["TemplateName"] = template.Name;
        ViewData["FileContent"] = fileContent;

        return View();
    }


}