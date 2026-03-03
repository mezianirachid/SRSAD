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

namespace SRSAD.ViewModels
{
    public class SecteurController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: Secteur
        public ActionResult Index(string searchNom, string searchCode, bool? actifUniquement)
        {
            var secteurs = db.SecteursSRSAD.Include(s => s.CodesPostauxSecteur).AsQueryable();

            if (!string.IsNullOrEmpty(searchNom))
                secteurs = secteurs.Where(s => s.Nom.Contains(searchNom));

            if (!string.IsNullOrEmpty(searchCode))
                secteurs = secteurs.Where(s => s.Code.Contains(searchCode));

            if (actifUniquement ?? true)
                secteurs = secteurs.Where(s => s.EstActif == true);

            var model = secteurs.OrderBy(s => s.Nom).ToList();
            return View(model);
        }

        // GET: Secteur/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            SecteursSRSAD secteur = db.SecteursSRSAD
                .Include(s => s.CodesPostauxSecteur)
                .FirstOrDefault(s => s.SecteurID == id);

            if (secteur == null)
                return HttpNotFound();

            return View(secteur);
        }

        // GET: Secteur/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Secteur/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Code,Nom,EstActif")] SecteursSRSAD secteur)
        {
            if (ModelState.IsValid)
            {
                db.SecteursSRSAD.Add(secteur);
                db.SaveChanges();

                JournaliserAction("CREATE", "SecteursSRSAD", secteur.SecteurID.ToString(), null,
                    $"Création secteur: {secteur.Code} - {secteur.Nom}");

                TempData["Success"] = "Secteur créé avec succès.";
                return RedirectToAction("Index");
            }

            return View(secteur);
        }

        // GET: Secteur/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            SecteursSRSAD secteur = db.SecteursSRSAD.Find(id);
            if (secteur == null)
                return HttpNotFound();

            return View(secteur);
        }

        // POST: Secteur/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "SecteurID,Code,Nom,EstActif")] SecteursSRSAD secteur)
        {
            if (ModelState.IsValid)
            {
                var original = db.SecteursSRSAD.AsNoTracking().FirstOrDefault(s => s.SecteurID == secteur.SecteurID);

                db.Entry(secteur).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "SecteursSRSAD", secteur.SecteurID.ToString(), original, secteur);

                TempData["Success"] = "Secteur modifié avec succès.";
                return RedirectToAction("Index");
            }
            return View(secteur);
        }

        // POST: Secteur/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            SecteursSRSAD secteur = db.SecteursSRSAD.Find(id);

            bool estUtilise = db.Usagers.Any(u => u.SecteurID == id) ||
                              db.Intervenants.Any(i => i.SecteurID == id) ||
                              db.CodesPostauxSecteur.Any(c => c.SecteurID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Ce secteur ne peut pas être supprimé car il est utilisé.";
                return RedirectToAction("Index");
            }

            db.SecteursSRSAD.Remove(secteur);
            db.SaveChanges();

            JournaliserAction("DELETE", "SecteursSRSAD", id.ToString(), secteur, null);

            TempData["Success"] = "Secteur supprimé avec succès.";
            return RedirectToAction("Index");
        }

        // GET: Secteur/CodesPostaux/5
        public ActionResult CodesPostaux(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var secteur = db.SecteursSRSAD.Find(id);
            if (secteur == null)
                return HttpNotFound();

            var codesPostaux = db.CodesPostauxSecteur
                .Where(c => c.SecteurID == id)
                .OrderBy(c => c.CodePostalPrefix)
                .ToList();

            ViewBag.SecteurNom = secteur.Nom;
            ViewBag.SecteurID = id;

            return View(codesPostaux);
        }

        // POST: Secteur/AjouterCodePostal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjouterCodePostal(int secteurId, string codePostal)
        {
            if (string.IsNullOrEmpty(codePostal))
            {
                TempData["Error"] = "Le code postal est requis.";
                return RedirectToAction("CodesPostaux", new { id = secteurId });
            }

            var existant = db.CodesPostauxSecteur.FirstOrDefault(c => c.CodePostalPrefix == codePostal);
            if (existant != null)
            {
                TempData["Error"] = "Ce code postal est déjà associé à un secteur.";
                return RedirectToAction("CodesPostaux", new { id = secteurId });
            }

            var code = new CodesPostauxSecteur
            {
                CodePostalPrefix = codePostal.ToUpper(),
                SecteurID = secteurId
            };

            db.CodesPostauxSecteur.Add(code);
            db.SaveChanges();

            JournaliserAction("CREATE", "CodesPostauxSecteur", codePostal, null,
                $"Ajout code postal {codePostal} au secteur {secteurId}");

            TempData["Success"] = "Code postal ajouté avec succès.";
            return RedirectToAction("CodesPostaux", new { id = secteurId });
        }

        // POST: Secteur/SupprimerCodePostal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SupprimerCodePostal(string codePostal)
        {
            var code = db.CodesPostauxSecteur.Find(codePostal);
            if (code == null)
                return HttpNotFound();

            db.CodesPostauxSecteur.Remove(code);
            db.SaveChanges();

            JournaliserAction("DELETE", "CodesPostauxSecteur", codePostal, code, null);

            TempData["Success"] = "Code postal supprimé.";
            return RedirectToAction("CodesPostaux", new { id = code.SecteurID });
        }

        private void JournaliserAction(string action, string table, string clePrimaire, object ancien, object nouveau)
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