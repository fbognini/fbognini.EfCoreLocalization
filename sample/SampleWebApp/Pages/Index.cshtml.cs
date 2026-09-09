using fbognini.EfCoreLocalization.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata;
using System.ComponentModel.DataAnnotations;

namespace SampleWebApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ILocalizationRepository _localizationRepository;

        public IndexModel(ILogger<IndexModel> logger, ILocalizationRepository localizationRepository)
        {
            _logger = logger;
            _localizationRepository = localizationRepository;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress()]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }


        public async Task OnGet()
        {
        }


        public async Task<IActionResult> OnPostAsync( CancellationToken cancellationToken = default)
        {
            return Page();
        }
    }
}
