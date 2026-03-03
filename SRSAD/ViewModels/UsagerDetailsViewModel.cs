using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRSAD.ViewModels
{
    public class UsagerDetailsViewModel
    {
        public int UsagerID { get; set; }
        public string NoDossierSRSAD { get; set; }
        public string NoDossierIPM { get; set; }
        public string RAMQ { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string NomComplet => Nom + " " + Prenom;
        public DateTime? DateNaissance { get; set; }
        public string Sexe { get; set; }
        public string Telephone { get; set; }

        public List<UsagerAdresseViewModel> Adresses { get; set; }

        public string Secteur { get; set; }
        public string CLSC { get; set; }
        public string StatutDossier { get; set; }
        public bool CodeHydroQuebec { get; set; }
        public string AgentPayeur { get; set; }
        public string CentreDeCout { get; set; }
        public string BonCommande { get; set; }
        public string StatutTabagique { get; set; }
        public bool EstActif { get; set; }

        public DateTime DateCreation { get; set; }
        public DateTime? DateDerniereSyncEClinibase { get; set; }
    }
}