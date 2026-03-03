using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRSAD.ViewModels
{
    public class UsagerListViewModel
    {
        public int UsagerID { get; set; }
        public string NoDossierSRSAD { get; set; }
        public string NoDossierIPM { get; set; }
        public string NomComplet { get; set; }
        public DateTime? DateNaissance { get; set; }
        public string RAMQ { get; set; }
        public string Secteur { get; set; }
        public string StatutDossier { get; set; }
        public bool EstActif { get; set; }
        public string AdresseCourante { get; set; }
        public string Telephone { get; set; }
    }
}