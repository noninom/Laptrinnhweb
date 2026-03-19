using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Laptrinnhweb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DatBansController : Controller
    {
        // GET: DatBansController
        public ActionResult Index()
        {
            return View();
        }

        // GET: DatBansController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: DatBansController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DatBansController/Create
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

        // GET: DatBansController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: DatBansController/Edit/5
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

        // GET: DatBansController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: DatBansController/Delete/5
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
