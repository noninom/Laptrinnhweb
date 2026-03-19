using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Laptrinnhweb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BanAnsController : Controller
    {
        // GET: BanAnsController
        public ActionResult Index()
        {
            return View();
        }

        // GET: BanAnsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: BanAnsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: BanAnsController/Create
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

        // GET: BanAnsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: BanAnsController/Edit/5
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

        // GET: BanAnsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: BanAnsController/Delete/5
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
