using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRSAD.ViewModels
{
    public class UsagerAdresseViewModel
    {
        public int UsagerAdresseID { get; set; }
        public string TypeAdresse { get; set; }
        public string SourceAdresse { get; set; }
        public bool EstCourante { get; set; }
        public string AdresseComplete { get; set; }
        public string Telephone { get; set; }
        public DateTime? DateDebutValidite { get; set; }
        public DateTime? DateFinValidite { get; set; }
        public string EClinibaseAdresseId { get; set; }
        public bool EstActive => !DateFinValidite.HasValue || DateFinValidite >= DateTime.Today;
    }
}