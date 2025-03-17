using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using TemplateFiller.Models;

namespace TemplateFiller.ViewModels
{
    public class SendDocumentViewModel
{
    public int DocumentId { get; set; } 

    [Required(ErrorMessage = "Введите email адрес !")]
    [EmailAddress(ErrorMessage = "Некорректный email адрес !")]
    public string Email { get; set; } 
}

}