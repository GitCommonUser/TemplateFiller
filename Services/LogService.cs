using TemplateFiller.Data;
using TemplateFiller.Models;

namespace TemplateFiller.Services;

public class LogService : ILogService
{
    private readonly TemplateFillerContext _context;

    public LogService(TemplateFillerContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string action)
    {
        var log = new Log
        {
            Action = action,
            Date = DateTime.Now 
        };

        _context.Logs.Add(log);
        await _context.SaveChangesAsync();
    }
}