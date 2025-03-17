
using System.ComponentModel.DataAnnotations;

namespace TemplateFiller.Models
{
    public class Template
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название шаблона !")]
        [Display(Name = "Название шаблона")]
        public string Name {get; set;}

        [Required(ErrorMessage = "Загрузите файл шаблона !")]
        [Display(Name = "Файл шаблона")]
        public string FilePath {get; set;}

    }
}