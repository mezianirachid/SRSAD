using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SRSAD.ViewModels
{
    public class RechercheEClinibaseViewModel
    {
        [Display(Name = "Nom")]
        public string Nom { get; set; }

        [Display(Name = "Prénom")]
        public string Prenom { get; set; }

        [Display(Name = "Date de naissance")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DateNaissance { get; set; }

        [Display(Name = "RAMQ")]
        public string RAMQ { get; set; }

        [Display(Name = "N° IPM")]
        public string NoIPM { get; set; }
    }
}