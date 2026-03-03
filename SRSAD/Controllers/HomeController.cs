using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using SRSAD.Models;

namespace GestionLocaux.Controllers
{
    public class HomeController : Controller
    {
        private const string NotAuthenticatedMessage = "Utilisateur non authentifié.";
        private const string UserNotFoundMessage = "Utilisateur introuvable.";

        [HttpPost]
        public JsonResult KeepSessionAlive()
        {
            return Json(new { Data = "Beat Generated" });
        }

        public ActionResult Grid()
        {
            ViewBag.six = "6";
            ViewBag.height = "150px";
            ViewBag.width = "250px";
            return View();
        }

        public ActionResult FetchColors()
        {
            return View();
        }

        public ActionResult Support()
        {
            return View();
        }

        public ActionResult Home()
        {
            return View();
        }

        public ActionResult Index()
        {
            return View();
        }

        
    }
}