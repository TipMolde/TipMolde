namespace TipMolde.Infrastructure.Settings
{
    public sealed class TemplateOptions
    {
        public const string SectionName = "Templates";

        public string RootPath { get; set; } = "Templates";
        public string FichaFLT { get; set; } = "FLT.xlsx";
        public string FichaFRE { get; set; } = "FRE.xlsx";
        public string FichaFRM { get; set; } = "FRM.xlsx";
        public string FichaFRA { get; set; } = "FRA.xlsx";
        public string FichaFOP { get; set; } = "FOP.xlsx";
        public string FolhaFLT { get; set; } = "FLT - TM.04.05";
        public string FolhaFRE { get; set; } = "FRE - TM.08.05";
        public string FolhaFRM { get; set; } = "FRM - TM.09.05";
        public string FolhaFRA { get; set; } = "FRA - TM.010.05";
        public string FolhaFOP { get; set; } = "FOP - TM.07.05";
    }
}
