namespace fbognini.EfCoreLocalization.Dashboard.Handlers.Languages
{
    public class UpdateLanguageCommand
    {
        public string Id { get; set; } = default!;
        public required string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
    }
}
