using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using TemplateFiller.Models;

namespace TemplateFiller.ViewModels
{
    public class CreateDocumentViewModel
    {
        [Required(ErrorMessage = "Выберите шаблон !")]
        public int? SelectedTemplateId { get; set; }

        [BindNever] 
        public List<Template> Templates { get; set; }

        [Required(ErrorMessage = "Введите название документа !")]
        [Display(Name = "Название документа")]
        public string DocumentViewModelName { get; set; }

        [Required(ErrorMessage = "Все переменные должны быть заполнены !")]
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
    }

}