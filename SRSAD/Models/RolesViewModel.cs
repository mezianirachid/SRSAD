using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRSAD.Models
{
    public class RolesViewModel
    {
        public IEnumerable<string> RoleNames { get; set; }
        public string UserName { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Courriel { get; set; }

    }
}