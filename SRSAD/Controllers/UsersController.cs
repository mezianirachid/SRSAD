using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using SRSAD.Models;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Threading.Tasks;
namespace SRSAD.ViewModels
{
    [Authorize]
    public class UsersController : Controller
    {
        private ApplicationDbContext context = new ApplicationDbContext();
        // GET: Users
        public Boolean isAdminUser()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = User.Identity;
                ApplicationDbContext context = new ApplicationDbContext();
                var UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
                var s = UserManager.GetRoles(user.GetUserId());
                if (s[0].ToString() == "Admin")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        public ActionResult Index()
        {
            var userRoles = new List<RolesViewModel>();
            var context = new ApplicationDbContext();
            var userStore = new UserStore<ApplicationUser>(context);
            var userManager = new UserManager<ApplicationUser>(userStore);

            //Get all the usernames
            foreach (var user in userStore.Users)
            {
                var r = new RolesViewModel
                {
                    UserName = user.UserName,
                    Nom = user.Nom,
                    Prenom = user.Prenom,
                    Courriel = user.Email
                };
                userRoles.Add(r);
            }
            return View(userRoles);
        }


        private void LoadUsersAndRoles()
        {
            var users = new List<string>();
            var roles = new List<string>();

            using (var context = new ApplicationDbContext())
            {
                var roleStore = new RoleStore<IdentityRole>(context);
                var roleManager = new RoleManager<IdentityRole>(roleStore);

                var userStore = new UserStore<ApplicationUser>(context);
                var userManager = new UserManager<ApplicationUser>(userStore);

                users = userManager.Users.Select(x => x.UserName).ToList();
                roles = roleManager.Roles.Select(x => x.Name).ToList();
            }

            ViewBag.Users = new SelectList(users);


            ViewBag.roles = new SelectList(roles);

        }

        public ActionResult EditUser(string id)
        {
            ApplicationUser user = new ApplicationUser();
            var UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
            user = UserManager.FindById(id);

            /*********************************/
            string userNameConnected = System.Web.HttpContext.Current.User.Identity.Name;
            if (System.Web.HttpContext.Current.User.IsInRole("Admin"))
                ViewBag.roles = new SelectList(context.Roles.OrderByDescending(x => x.Name).Where(u => !u.Name.Contains("Admin")).ToList(), "Name", "Name");
            else
                if (System.Web.HttpContext.Current.User.IsInRole("Super utilisateur"))
                ViewBag.roles = new SelectList(context.Roles.OrderByDescending(x => x.Name).Where(u => !u.Name.Contains("Admin")).ToList(), "Name", "Name");
            else
                ViewBag.roles = new SelectList(context.Roles.Where(u => !u.Name.Contains("Admin") && !u.Name.Contains("Super utilisateur")).ToList(), "Name", "Name");

            /*********************************/



            //ViewBag.roles = new SelectList(context.Roles.Where(u => !u.Name.ToUpper().Contains("ADMIN")).ToList(), "Name", "Name", user.Roles.FirstOrDefault().RoleId);


            return View(new UserEdit()
            {
                UserId = user.Id,
                Username = user.UserName,
                FirstName = user.Nom,
                LastName = user.Prenom,
                Email = user.Email,
                Telephone = user.PhoneNumber,
                UserRoles = UserManager.GetRoles(user.Id).FirstOrDefault()
            });

        }


        [Authorize(Roles = "Admin, Super utilisateur")]
        [HttpPost]
        public async Task<ActionResult> EditUser(UserEdit model)
        {
            ViewBag.roles = new SelectList(context.Roles.Where(u => !u.Name.ToUpper().Contains("ADMIN")).Distinct().ToList(), "Id", "Name");
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var store = new UserStore<ApplicationUser>(new ApplicationDbContext());
            var Gestionnaire = new UserManager<ApplicationUser>(store);
            var currentUser = Gestionnaire.FindById(model.UserId);
            currentUser.Nom = model.FirstName;
            currentUser.Prenom = model.LastName;
            currentUser.PhoneNumber = model.Telephone;
            currentUser.Email = model.Email;
            IList<string> oldRoleNames = Gestionnaire.GetRoles(model.UserId);
            string oldRoleNameUser = oldRoleNames.FirstOrDefault();
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var newRoleNameUser = roleManager.FindByName(model.UserRoles);
            //string currentRole = currentUser.Roles.FirstOrDefault().RoleId;
            string userId = model.UserId;
            Gestionnaire.RemoveFromRole(userId, oldRoleNameUser);
            Gestionnaire.AddToRole(userId, newRoleNameUser.Name);
            await Gestionnaire.UpdateAsync(currentUser);
            if (currentUser == null)
                throw new Exception("Utilisateur non trouvé!");

            var ctx = store.Context;
            ctx.SaveChanges();

            return RedirectToAction("UsersWithRoles");
        }

        public ActionResult UsersWithRoles()
        {
            var usersWithRoles = (from user in context.Users
                                  select new
                                  {
                                      UserId = user.Id,
                                      Username = user.UserName,
                                      FirstName = user.Prenom,
                                      LastName = user.Nom,
                                      Telephone = user.PhoneNumber,
                                      Email = user.Email,
                                      LockoutEnabled = user.LockoutEnabled,
                                      RoleNames = (from userRole in user.Roles
                                                   join role in context.Roles on userRole.RoleId
                                                   equals role.Id
                                                   select role.Name).ToList()
                                  }).ToList().Select(p => new UserEdit()
                                  {
                                      UserId = p.UserId,
                                      Username = p.Username,
                                      LastName = p.LastName,
                                      FirstName = p.FirstName,
                                      Telephone = p.Telephone,
                                      Email = p.Email,
                                      LockoutEnabled = p.LockoutEnabled,
                                      Role = string.Join(",", p.RoleNames)
                                  });

            return View(usersWithRoles);
        }

        public ActionResult LockUser(string id)
        {
            SRSAD.Models.ApplicationDbContext dbRole = new SRSAD.Models.ApplicationDbContext();
            //Save date

            var user = dbRole.Users.Find(id);
            user.LockoutEnabled = false;         
            dbRole.SaveChanges();
            return RedirectToAction("UsersWithRoles");
        }

        public ActionResult UnlockUser(string id)
        {
            SRSAD.Models.ApplicationDbContext dbRole = new SRSAD.Models.ApplicationDbContext();
            var user = dbRole.Users.Find(id);
            user.LockoutEnabled = true;           
            dbRole.SaveChanges();
            return RedirectToAction("UsersWithRoles");
        }

    }
}