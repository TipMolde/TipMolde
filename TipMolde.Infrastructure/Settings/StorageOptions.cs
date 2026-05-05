namespace TipMolde.Infrastructure.Settings
{
    public sealed class StorageOptions
    {
        public const string SectionName = "Storage";
        public string FichasRootPath { get; set; } = "Storage/Fichas";
        public string UploadsRootPath { get; set; } = "Storage/Uploads";
    }
}
