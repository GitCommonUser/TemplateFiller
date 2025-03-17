using System.ComponentModel.DataAnnotations;

namespace TemplateFiller.ViewModels
{
    public class CreateTemplateViewModel
    {
        [Required(ErrorMessage = "Введите название шаблона !")]
        [Display(Name = "Название шаблона")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Загрузите файл шаблона !")]
        [Display(Name = "Файл шаблона")]
        public IFormFile File { get; set; }
    }
}