using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using SRSAD.Models;
using Microsoft.AspNet.Identity;

namespace SRSAD.Controllers  // Changé de ViewModels à Controllers
{
    public class AgentPayeurController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: AgentPayeur
        public ActionResult Index(string searchCode, string searchNom, string searchAgentPayeurCode, int? typeFinancementID, bool? actifUniquement)
        {
            var agents = db.AgentsPayeurs
                .Include(a => a.TypesFinancementRef)  // Inclure le type de financement
                .AsQueryable();

            // Filtres de recherche
            if (!string.IsNullOrEmpty(searchCode))
                agents = agents.Where(a => a.AgentPayeurCode.Contains(searchCode));

            if (!string.IsNullOrEmpty(searchNom))
                agents = agents.Where(a => a.Nom.Contains(searchNom));

            if (!string.IsNullOrEmpty(searchAgentPayeurCode))
                agents = agents.Where(a => a.AgentPayeurCode.Contains(searchAgentPayeurCode));

            if (typeFinancementID.HasValue)
                agents = agents.Where(a => a.TypeFinancementID == typeFinancementID.Value);

            if (actifUniquement ?? true)
                agents = agents.Where(a => a.EstActif == true);

            var model = agents.OrderBy(a => a.Nom).ToList();

            // Statistiques d'utilisation
            var statsPatients = db.Usagers
                .Where(u => u.AgentPayeurID != null)
                .GroupBy(u => u.AgentPayeurID.Value)
                .ToDictionary(g => g.Key, g => g.Count());
            ViewBag.StatistiquesPatients = statsPatients;

            var statsCommandes = db.CommandesCylindres
                .Where(c => c.AgentPayeurID != null)
                .GroupBy(c => c.AgentPayeurID.Value)
                .ToDictionary(g => g.Key, g => g.Count());
            ViewBag.StatistiquesCommandes = statsCommandes;

            // Liste des types de financement pour le filtre
            ViewBag.TypeFinancementList = new SelectList(
                db.TypesFinancementRef.Where(t => t.EstActif == true).OrderBy(t => t.TypeFinancementCode),
                "TypeFinancementID",
                "Description",
                typeFinancementID
            );

            return View(model);
        }

        // GET: AgentPayeur/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            AgentsPayeurs agent = db.AgentsPayeurs
                .Include(a => a.TypesFinancementRef)
                .FirstOrDefault(a => a.AgentPayeurID == id);

            if (agent == null)
                return HttpNotFound();

            // Statistiques patients
            ViewBag.NombrePatients = db.Usagers.Count(u => u.AgentPayeurID == id);
            ViewBag.PatientsActifs = db.Usagers.Count(u => u.AgentPayeurID == id && u.EstActif);

            // Derniers patients
            ViewBag.DerniersPatients = db.Usagers
                .Where(u => u.AgentPayeurID == id)
                .OrderByDescending(u => u.DateCreation)
                .Take(10)
                .ToList();

            // Statistiques commandes
            var commandes = db.CommandesCylindres
                .Include(c => c.Usagers)
                .Include(c => c.CommandeCylindreLignes)
                .Where(c => c.AgentPayeurID == id);

            ViewBag.NombreCommandes = commandes.Count();
            ViewBag.CommandesEnCours = commandes.Count(c => c.Statut == "EN_COURS");
            ViewBag.MontantTotal = commandes
                .SelectMany(c => c.CommandeCylindreLignes)
                .Sum(l => (decimal?)l.PrixTotal) ?? 0;

            // Dernières commandes
            ViewBag.DernieresCommandes = commandes
                .OrderByDescending(c => c.DateCreation)
                .Take(10)
                .ToList();

            return View(agent);
        }

        // POST: AgentPayeur/Activer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Activer(int id)
        {
            try
            {
                AgentsPayeurs agent = db.AgentsPayeurs.Find(id);
                if (agent == null)
                    return HttpNotFound();

                agent.EstActif = true;
                agent.DateModification = DateTime.Now;
                agent.UtilisateurModification = User.Identity.GetUserName();

                db.Entry(agent).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("ACTIVER", "AgentsPayeurs", id.ToString(), null,
                    $"Activation agent payeur: {agent.AgentPayeurCode} - {agent.Nom}");

                TempData["Success"] = "Agent payeur activé avec succès.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Erreur lors de l'activation : " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: AgentPayeur/Desactiver/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Desactiver(int id)
        {
            try
            {
                AgentsPayeurs agent = db.AgentsPayeurs.Find(id);
                if (agent == null)
                    return HttpNotFound();

                agent.EstActif = false;
                agent.DateModification = DateTime.Now;
                agent.UtilisateurModification = User.Identity.GetUserName();

                db.Entry(agent).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("DESACTIVER", "AgentsPayeurs", id.ToString(), null,
                    $"Désactivation agent payeur: {agent.AgentPayeurCode} - {agent.Nom}");

                TempData["Success"] = "Agent payeur désactivé avec succès.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Erreur lors de la désactivation : " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: AgentPayeur/GetStats/5
        public JsonResult GetStats(int id)
        {
            var stats = new
            {
                nombrePatients = db.Usagers.Count(u => u.AgentPayeurID == id),
                patientsActifs = db.Usagers.Count(u => u.AgentPayeurID == id && u.EstActif),
                nombreCommandes = db.CommandesCylindres.Count(c => c.AgentPayeurID == id),
                commandesEnCours = db.CommandesCylindres.Count(c => c.AgentPayeurID == id && c.Statut == "EN_COURS"),
                montantTotal = db.CommandesCylindres
                    .Where(c => c.AgentPayeurID == id)
                    .SelectMany(c => c.CommandeCylindreLignes)
                    .Sum(l => (decimal?)l.PrixTotal) ?? 0
            };

            return Json(stats, JsonRequestBehavior.AllowGet);
        }

        // GET: AgentPayeur/VerifierUtilisation/5
        public JsonResult VerifierUtilisation(int id)
        {
            bool estUtilise = db.Usagers.Any(u => u.AgentPayeurID == id) ||
                              db.CommandesCylindres.Any(c => c.AgentPayeurID == id);

            return Json(new
            {
                estUtilise,
                nombrePatients = db.Usagers.Count(u => u.AgentPayeurID == id),
                nombreCommandes = db.CommandesCylindres.Count(c => c.AgentPayeurID == id)
            }, JsonRequestBehavior.AllowGet);
        }

        // GET: AgentPayeur/Create
        public ActionResult Create()
        {
            // Initialiser avec des valeurs par défaut
            var agent = new AgentsPayeurs
            {
                EstActif = true,
                DateCreation = DateTime.Now
            };

            // Charger la liste des types de financement
            ViewBag.TypeFinancementList = new SelectList(
                db.TypesFinancementRef.Where(t => t.EstActif == true).OrderBy(t => t.TypeFinancementCode),
                "TypeFinancementID",
                "Description"
            );

            return View(agent);
        }

        // POST: AgentPayeur/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,AgentPayeurCode,Nom,EstActif,Description,Adresse,Telephone,Contact,TypeFinancementID")] AgentsPayeurs agent)
        {
            // Vérifier l'unicité du Code
            if (db.AgentsPayeurs.Any(a => a.AgentPayeurCode == agent.AgentPayeurCode))
                ModelState.AddModelError("Code", "Ce code existe déjà.");

            // Vérifier l'unicité de l'AgentPayeurCode
            if (db.AgentsPayeurs.Any(a => a.AgentPayeurCode == agent.AgentPayeurCode))
                ModelState.AddModelError("AgentPayeurCode", "Ce code agent existe déjà.");

            if (ModelState.IsValid)
            {
                agent.DateCreation = DateTime.Now;
                agent.UtilisateurCreation = User.Identity.GetUserName();

                db.AgentsPayeurs.Add(agent);
                db.SaveChanges();

                JournaliserAction("CREATE", "AgentsPayeurs", agent.AgentPayeurID.ToString(), null,
                    $"Création agent payeur: {agent.AgentPayeurCode} - {agent.Nom}");

                TempData["Success"] = "Agent payeur créé avec succès.";
                return RedirectToAction("Index");
            }

            // Recharger la liste en cas d'erreur
            ViewBag.TypeFinancementList = new SelectList(
                db.TypesFinancementRef.Where(t => t.EstActif == true).OrderBy(t => t.TypeFinancementCode),
                "TypeFinancementID",
                "Description",
                agent.TypeFinancementID 
            );

            return View(agent);
        }

        // GET: AgentPayeur/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            AgentsPayeurs agent = db.AgentsPayeurs.Find(id);
            if (agent == null)
                return HttpNotFound();

            // Charger la liste des types de financement
            ViewBag.TypeFinancementList = new SelectList(
                db.TypesFinancementRef.Where(t => t.EstActif==true).OrderBy(t => t.TypeFinancementCode),
                "TypeFinancementID",
                "Description",
                agent.TypeFinancementID
            );

            return View(agent);
        }

        // POST: AgentPayeur/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "AgentPayeurID,Code,AgentPayeurCode,Nom,EstActif,Description,Adresse,Telephone,Contact,TypeFinancementID")] AgentsPayeurs agent)
        {
            // Vérifier l'unicité du Code (exclure l'enregistrement actuel)
            if (db.AgentsPayeurs.Any(a => a.AgentPayeurCode == agent.AgentPayeurCode && a.AgentPayeurID != agent.AgentPayeurID))
                ModelState.AddModelError("Code", "Ce code existe déjà.");

            // Vérifier l'unicité de l'AgentPayeurCode (exclure l'enregistrement actuel)
            if (db.AgentsPayeurs.Any(a => a.AgentPayeurCode == agent.AgentPayeurCode && a.AgentPayeurID != agent.AgentPayeurID))
                ModelState.AddModelError("AgentPayeurCode", "Ce code agent existe déjà.");

            if (ModelState.IsValid)
            {
                var original = db.AgentsPayeurs.AsNoTracking().FirstOrDefault(a => a.AgentPayeurID == agent.AgentPayeurID);

                agent.DateModification = DateTime.Now;
                agent.UtilisateurModification = User.Identity.GetUserName();
                agent.DateCreation = original?.DateCreation; // Préserver la date de création originale

                db.Entry(agent).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "AgentsPayeurs", agent.AgentPayeurID.ToString(), original, agent);

                TempData["Success"] = "Agent payeur modifié avec succès.";
                return RedirectToAction("Index");
            }

            // Recharger la liste en cas d'erreur
            ViewBag.TypeFinancementList = new SelectList(
                db.TypesFinancementRef.Where(t => t.EstActif==true).OrderBy(t => t.TypeFinancementCode),
                "TypeFinancementID",
                "Description",
                agent.TypeFinancementID
            );

            return View(agent);
        }

        // POST: AgentPayeur/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            AgentsPayeurs agent = db.AgentsPayeurs.Find(id);

            bool estUtilise = db.Usagers.Any(u => u.AgentPayeurID == id) ||
                              db.CommandesCylindres.Any(c => c.AgentPayeurID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Cet agent payeur ne peut pas être supprimé car il est utilisé.";
                return RedirectToAction("Index");
            }

            var original = db.AgentsPayeurs.AsNoTracking().FirstOrDefault(a => a.AgentPayeurID == id);

            db.AgentsPayeurs.Remove(agent);
            db.SaveChanges();

            JournaliserAction("DELETE", "AgentsPayeurs", id.ToString(), original, null);

            TempData["Success"] = "Agent payeur supprimé avec succès.";
            return RedirectToAction("Index");
        }

        private void JournaliserAction(string action, string table, string clePrimaire, object ancien, object nouveau)
        {
            try
            {
                var audit = new JournalAudit
                {
                    DateHeure = DateTime.Now,
                    Action = action,
                    TableConcernee = table,
                    ClePrimaire = clePrimaire,
                    AnciennesValeurs = ancien != null ? Newtonsoft.Json.JsonConvert.SerializeObject(ancien) : null,
                    NouvellesValeurs = nouveau != null ? Newtonsoft.Json.JsonConvert.SerializeObject(nouveau) : null,
                    UtilisateurId = User.Identity.GetUserId()
                };

                db.JournalAudit.Add(audit);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne fait pas échouer l'opération principale
                System.Diagnostics.Debug.WriteLine($"Erreur journalisation: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}