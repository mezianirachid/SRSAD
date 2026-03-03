using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SRSAD.ViewModels
{
    public class ImportEClinibaseViewModel
    {
        public string NoDossierIPM { get; set; }
        public string RAMQ { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public DateTime? DateNaissance { get; set; }
        public string Sexe { get; set; }
        public string Telephone { get; set; }

        public List<AdresseImportViewModel> Adresses { get; set; }

        [Required(ErrorMessage = "Le secteur est obligatoire")]
        [Display(Name = "Secteur")]
        public int? SecteurID { get; set; }

        [Required(ErrorMessage = "Le CLSC est obligatoire")]
        [Display(Name = "CLSC")]
        public int? CLSCID { get; set; }

        [Required(ErrorMessage = "Le statut dossier est obligatoire")]
        [Display(Name = "Statut dossier")]
        public int? StatutDossierID { get; set; }

        [Display(Name = "Code Hydro-Québec")]
        public bool CodeHydroQuebec { get; set; }

        [Display(Name = "Agent payeur")]
        public int? AgentPayeurID { get; set; }

        [Display(Name = "Centre de coût")]
        public string CentreDeCout { get; set; }

        [Display(Name = "Bon de commande")]
        public string BonCommande { get; set; }

        [Display(Name = "Statut tabagique")]
        public string StatutTabagique { get; set; }

        public bool EstActif { get; set; }
        public string Pays { get; set; }
        public string Province { get; set; }
        public DateTime? DateDerniereSyncEClinibase { get; set; }
    }
}