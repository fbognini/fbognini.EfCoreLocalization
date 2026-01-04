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

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
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
            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}