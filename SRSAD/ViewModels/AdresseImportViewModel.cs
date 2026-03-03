using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRSAD.ViewModels
{
    public class AdresseImportViewModel
    {
        public string TypeAdresse { get; set; }
        public string SourceAdresse { get; set; }
        public string Adresse1 { get; set; }
        public string Adresse2 { get; set; }
        public string Ville { get; set; }
        public string Province { get; set; }
        public string CodePostal { get; set; }
        public string Pays { get; set; }
        public string Telephone { get; set; }
        public DateTime? DateDebutValidite { get; set; }
        public DateTime? DateFinValidite { get; set; }
        public string EClinibaseAdresseId { get; set; }
        public bool EstCourante { get; set; }
        public string AdresseComplete => $"{Adresse1} {Adresse2}, {Ville}, {Province} {CodePostal}".Trim();
    }
}