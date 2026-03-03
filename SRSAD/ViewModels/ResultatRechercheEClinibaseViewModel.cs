using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRSAD.ViewModels
{
    public class ResultatRechercheEClinibaseViewModel
    {
        public string NoIPM { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public DateTime DateNaissance { get; set; }
        public string RAMQ { get; set; }
        public string Sexe { get; set; }
        public string AdresseComplete { get; set; }
        public string Telephone { get; set; }
        public int Age => DateTime.Now.Year - DateNaissance.Year;
        public string NomComplet => Nom + " " + Prenom;
    }
}