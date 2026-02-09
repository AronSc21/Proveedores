using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Proveedores.Controllers
{
    public class ProveedoresController : Controller
    {
        // GET: Proveedores
        [HttpGet]
        public ActionResult Registro()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Registro(Proveedores model)
        {
            try
            {
                using (var db = new ProveedoresEntities())
                {
                    model.Fecha_Solicitud = DateTime.Now;
                    model.Alta = true;
                    model.Modificacion = false;
                    model.Renovacion = false;
                    model.Fecha_Aceptacion = null;

                    db.Proveedores.Add(model);
                    db.SaveChanges();
                }

                return Json(new { ok = true, mensaje = "Solicitud registrada correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = ex.Message });
            }
        }
    }
}