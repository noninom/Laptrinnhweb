using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Laptrinnhweb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MonAnsController : Controller
    {
        // GET: MonAnsController
        public ActionResult Index()
        {
            return View();
        }

        // GET: MonAnsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: MonAnsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: MonAnsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: MonAnsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: MonAnsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: MonAnsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: MonAnsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
