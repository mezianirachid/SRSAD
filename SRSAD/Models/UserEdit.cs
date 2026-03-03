using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace SRSAD.Models
{
    public class UserEdit
    {
        public string UserId { get; set; }

        [Display(Name = "Nom utilisateur")]
        public string Username { get; set; }

        [Required]
        [Display(Name = "Nom")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Prénom")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Courriel")]
        public string Email { get; set; }

        [Display(Name = "Téléphone")]
        public string Telephone { get; set; }

        public string Role { get; set; }

        [Required]
        [Display(Name = "Rôle utilisateur")]
        public string UserRoles { get; set; }

        public bool EmailConfirmed { get; set; }

        public bool LockoutEnabled { get; set; }


    }
}