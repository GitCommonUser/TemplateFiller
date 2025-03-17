using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using TemplateFiller.Data;
using TemplateFiller.Models;
using TemplateFiller.ViewModels;
using TemplateFiller.Services;

namespace TemplateFiller.Controllers;

public class HomeController : Controller
{
    private readonly TemplateFillerContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogService _logService;

    public HomeController(TemplateFillerContext context, IWebHostEnvironment environment, IConfiguration configuration, ILogService logService)
    {
        _context = context;
        _environment = environment;
        _configuration = configuration;
        _logService = logService;
    }
    public async Task<IActionResult> Index()
    {
        var documents = await _context.Documents.ToListAsync();
        return View(documents);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        var filePath = Path.Combine(_environment.WebRootPath, document.FilePath);
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        await _logService.LogAsync($"Удален документ {document.Name}");

        return RedirectToAction("Index");
    }

    public IActionResult Details(int id)
    {
        var document = _context.Documents.Find(id);
        if (document == null)
        {
            return NotFound();
        }

        var filePath = Path.Combine(_environment.WebRootPath, document.FilePath);

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
            fileContent = "Файл документа не найден !";
        }

        ViewData["DocumentName"] = document.Name;
        ViewData["FileContent"] = fileContent;

        return View();
    }

    public IActionResult Create()
    {
        var model = new CreateDocumentViewModel
        {
            Templates = _context.Templates.ToList(), 
            Variables = new Dictionary<string, string>() 
        };

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SelectedTemplateId,DocumentViewModelName,Variables")] CreateDocumentViewModel model)
    {

        ModelState.Remove("Templates");

        if (model.SelectedTemplateId == null)
        {
            ModelState.AddModelError("SelectedTemplateId", "Выберите шаблон !");
        }



        if (ModelState.IsValid)
        {
            var template = _context.Templates.Find(model.SelectedTemplateId);
            if (template == null)
            {
                ModelState.AddModelError("SelectedTemplateId", "Шаблон не найден.");
                model.Templates = _context.Templates.ToList();
                return View(model);
            }


            var variables = ExtractVariablesFromTemplate(Path.Combine(_environment.WebRootPath, template.FilePath));
            foreach (var variable in variables)
            {
                if (string.IsNullOrEmpty(model.Variables[variable]))
                {
                    ModelState.AddModelError("Variables", $"Поле '{variable}' обязательно для заполнения.");
                }
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine('2');
                model.Templates = _context.Templates.ToList();
                return View(model);
            }

            var templatePath = Path.Combine(_environment.WebRootPath, template.FilePath);
            if (!System.IO.File.Exists(templatePath))
            {
                ModelState.AddModelError("", "Файл шаблона не найден.");
                model.Templates = _context.Templates.ToList();
                return View(model);
            }

            var newDocumentPath = await CreateDocumentFromTemplate(templatePath, model.Variables);

            var document = new Document
            {
                Name = model.DocumentViewModelName, 
                FilePath = newDocumentPath,
                CreationDate = DateTime.Now
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            await _logService.LogAsync($"Создан документ {document.Name}");

            return RedirectToAction("Index", "Home");
        }

        model.Templates = _context.Templates.ToList();
        return View(model);
    }

    [HttpGet]
    public IActionResult Send(int id)
    {
        var document = _context.Documents.Find(id);
        if (document == null)
        {
            return NotFound();
        }

        var model = new SendDocumentViewModel
        {
            DocumentId = document.Id 
        };


        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(SendDocumentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var document = _context.Documents.Find(model.DocumentId);
        if (document == null)
        {
            return NotFound();
        }

        var filePath = Path.Combine(_environment.WebRootPath, document.FilePath);

        if (!System.IO.File.Exists(filePath))
        {
            ModelState.AddModelError("", "Файл документа не найден.");
            return View(model);
        }

        try
        {
            await SendEmailAsync(model.Email, document.Name, filePath);

            await _logService.LogAsync($"Документ {document.Name} отправлен по адресу {model.Email}");

            return RedirectToAction("SendSuccess");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Ошибка при отправке email: {ex.Message}");
            return View(model);
        }
    }

    private async Task SendEmailAsync(string email, string documentName, string filePath)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("TemplateFiller", "no-reply@templatefiller.com"));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = $"Документ: {documentName}";

        var body = new TextPart("plain")
        {
            Text = $"Вам отправлен документ: {documentName}."
        };

        var attachment = new MimePart("application", "octet-stream")
        {
            Content = new MimeContent(System.IO.File.OpenRead(filePath)),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
            FileName = Path.GetFileName(filePath)
        };

        var multipart = new Multipart("mixed");
        multipart.Add(body);
        multipart.Add(attachment);

        message.Body = multipart;

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(smtpSettings["Server"], int.Parse(smtpSettings["Port"]), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpSettings["Username"], smtpSettings["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }




    private List<string> ExtractVariablesFromTemplate(string templatePath)
    {
        var variables = new List<string>();
        using (var doc = Xceed.Words.NET.DocX.Load(Path.Combine(_environment.WebRootPath, templatePath)))
        {
            foreach (var paragraph in doc.Paragraphs)
            {
                var matches = Regex.Matches(paragraph.Text, @"\$\{(\w+)\}");
                foreach (Match match in matches)
                {
                    if (!variables.Contains(match.Groups[1].Value))
                    {
                        variables.Add(match.Groups[1].Value);
                    }
                }
            }
        }
        return variables;
    }


    private async Task<string> CreateDocumentFromTemplate(string templatePath, Dictionary<string, string> variables)
    {
        var newFileName = $"document_{DateTime.Now:yyyyMMddHHmmss}.docx";
        var newFilePath = Path.Combine(_environment.WebRootPath, "documents", newFileName);

        using (var doc = Xceed.Words.NET.DocX.Load(templatePath))
        {
            foreach (var paragraph in doc.Paragraphs)
            {
                foreach (var variable in variables)
                {
                    paragraph.ReplaceText($"${{{variable.Key}}}", variable.Value);
                }
            }

            doc.SaveAs(newFilePath);
        }

        return Path.Combine("documents", newFileName);
    }

    public IActionResult GetTemplateVariables(int templateId)
    {
        var template = _context.Templates.Find(templateId);
        if (template == null)
        {
            return NotFound();
        }

        var variables = ExtractVariablesFromTemplate(Path.Combine(_environment.WebRootPath, template.FilePath));
        return Json(variables);
    }


}