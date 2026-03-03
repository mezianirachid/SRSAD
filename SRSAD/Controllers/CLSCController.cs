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
    public class CLSCController : Controller
    {
        private EntitiesDbConnection db = new EntitiesDbConnection();

        // GET: CLSC
        public ActionResult Index(string searchNom, string searchCode, bool? actifUniquement)
        {
            var clscs = db.CLSC.AsQueryable();

            if (!string.IsNullOrEmpty(searchNom))
                clscs = clscs.Where(c => c.NomCLSC.Contains(searchNom));

            if (!string.IsNullOrEmpty(searchCode))
                clscs = clscs.Where(c => c.CodeCLSC.Contains(searchCode));

            if (actifUniquement ?? true)
                clscs = clscs.Where(c => c.EstActif == true);

            var model = clscs.OrderBy(c => c.NomCLSC).ToList();
            return View(model);
        }

        // GET: CLSC/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            CLSC clsc = db.CLSC.Find(id);
            if (clsc == null)
                return HttpNotFound();

            return View(clsc);
        }

        // GET: CLSC/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CLSC/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CodeCLSC,NomCLSC,EstActif")] CLSC clsc)
        {
            if (ModelState.IsValid)
            {
                db.CLSC.Add(clsc);
                db.SaveChanges();

                JournaliserAction("CREATE", "CLSC", clsc.CLSCID.ToString(), null,
                    $"Création CLSC: {clsc.CodeCLSC} - {clsc.NomCLSC}");

                TempData["Success"] = "CLSC créé avec succès.";
                return RedirectToAction("Index");
            }

            return View(clsc);
        }

        // GET: CLSC/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            CLSC clsc = db.CLSC.Find(id);
            if (clsc == null)
                return HttpNotFound();

            return View(clsc);
        }

        // POST: CLSC/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CLSCID,CodeCLSC,NomCLSC,EstActif")] CLSC clsc)
        {
            if (ModelState.IsValid)
            {
                var original = db.CLSC.AsNoTracking().FirstOrDefault(c => c.CLSCID == clsc.CLSCID);

                db.Entry(clsc).State = EntityState.Modified;
                db.SaveChanges();

                JournaliserAction("UPDATE", "CLSC", clsc.CLSCID.ToString(), original, clsc);

                TempData["Success"] = "CLSC modifié avec succès.";
                return RedirectToAction("Index");
            }
            return View(clsc);
        }

        // POST: CLSC/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            CLSC clsc = db.CLSC.Find(id);

            // Vérifier si le CLSC est utilisé
            bool estUtilise = db.Intervenants.Any(i => i.CLSCID == id) ||
                              db.Usagers.Any(u => u.CLSCID == id);

            if (estUtilise)
            {
                TempData["Error"] = "Ce CLSC ne peut pas être supprimé car il est utilisé par des intervenants ou des patients.";
                return RedirectToAction("Index");
            }

            db.CLSC.Remove(clsc);
            db.SaveChanges();

            JournaliserAction("DELETE", "CLSC", id.ToString(), clsc, null);

            TempData["Success"] = "CLSC supprimé avec succès.";
            return RedirectToAction("Index");
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
                Description = nouveau != null ? Newtonsoft.Json.JsonConvert.SerializeObject(nouveau) : null,
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