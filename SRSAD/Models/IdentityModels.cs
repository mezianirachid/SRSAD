using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.ComponentModel.DataAnnotations;
using System;

namespace SRSAD.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        //custom fields
        [Required(ErrorMessage = "Le champ {0} est obligatoire")]
        [StringLength(100)]
        public string Nom { get; set; }

        [Required(ErrorMessage = "Le champ {0} est obligatoire")]
        [StringLength(100)]
        public string Prenom { get; set; }

        [Required(ErrorMessage = "Le champ {0} est obligatoire")]
        [StringLength(100)]
        public override string Email { get; set; }

        [StringLength(50)]
        [Display(Name = "Téléphone")]
        //[Required(AllowEmptyStrings = false, ErrorMessage = "Le champ {0} est obligatoire")]
        [RegularExpression(@"^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$", ErrorMessage = "Veuillez entrer un numéro de téléphone valide.")]
        public override string PhoneNumber { get; set; }
       
        [Required(ErrorMessage = "Le champ {0} est obligatoire")]
        [StringLength(100)]      
        public override string UserName { get; set; }

        //[StringLength(20)]
        //public string NoEmploye { get; set; }

        //public int InstallationId { get; set; }
        //[ForeignKey("InstallationId")]      

        // ajout pour affichage nom et prénom de l'usager au lieu du nom d'usager connecté Houda 2018-12-06
        public string FullName => $"{Nom} {Prenom}";

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext() : base("IdentityConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }       
    }
    /// <summary>
    /// Interface pour les entités auditées (CreatedOn / ModifiedOn)
    /// </summary>
    public interface IAuditableEntity
    {
        /// <summary>
        /// Date de création
        /// </summary>
        DateTime CreatedOn { get; set; }

        /// <summary>
        /// Date de dernière modification
        /// </summary>
        DateTime ModifiedOn { get; set; }
    }
}