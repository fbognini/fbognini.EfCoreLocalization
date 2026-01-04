namespace fbognini.EfCoreLocalization.Dashboard.Handlers.Texts
{
    public class DeleteTextCommand
    {
        public required string TextId { get; set; }
        public required string ResourceId { get; set; }
    }
}
