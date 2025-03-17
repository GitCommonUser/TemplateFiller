namespace TemplateFiller.Services;

public interface ILogService
{
    Task LogAsync(string action);
}