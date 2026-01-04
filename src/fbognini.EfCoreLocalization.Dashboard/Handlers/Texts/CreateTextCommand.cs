namespace fbognini.EfCoreLocalization.Dashboard.Handlers.Texts
{
    public class CreateTextCommand
    {
        public required string TextId { get; set; }
        public required string ResourceId { get; set; }
        public string? Description { get; set; }
    }
}
