using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRSAD.ViewModels
{
    public class HistoriqueActionViewModel
    {
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public string Utilisateur { get; set; }
    }
}