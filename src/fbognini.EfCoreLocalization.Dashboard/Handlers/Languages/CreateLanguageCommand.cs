namespace fbognini.EfCoreLocalization.Dashboard.Handlers.Languages
{
    public class CreateLanguageCommand
    {
        public required string Id { get; set; }
        public required string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
    }
}
