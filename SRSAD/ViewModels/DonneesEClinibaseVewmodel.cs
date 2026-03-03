using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRSAD.ViewModels
{
    public class DonneesEClinibaseVewmodel
    {
        public string NoIPM { get; set; }
        public string RAMQ { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public DateTime DateNaissance { get; set; }
        public string Sexe { get; set; }
        public string Telephone { get; set; }
        public List<AdresseEClinibaseViewModel> Adresses { get; set; }
    }
}