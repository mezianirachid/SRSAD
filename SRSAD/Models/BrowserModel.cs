using System.Collections.Generic;

namespace SRSAD.Models
{
    public class BrowserModel
    {
        public int ID { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Telephone { get; set; }
        public string Courriel { get; set; }
        public string CodePostal { get; set; }

        public BrowserModel() { }
        public BrowserModel(int id, string nom, string prenom, string telephone, string courriel, string codePostal)
        {
            ID = id;
            Nom = nom;
            Prenom = prenom;
            Telephone = telephone;
            Courriel = courriel;
            CodePostal = codePostal;
        }
    }

}