using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Proveedores.Controllers
{
    public class ProveedoresController : Controller
    {
        private ProveedoresEntities db = new ProveedoresEntities();
        
        [HttpGet]
        public ActionResult Registro()
        {
            return View();
        }
        [HttpPost]
        public ActionResult GuardarProveedor(Proveedores model)
        {
            try
            {
                if (model.IdTipoPersona != 1 && model.IdTipoPersona != 2)
                {
                    return Json(new { success = false, message = "Tipo de persona no definido" });
                }
                model.Fecha_Alta = DateTime.Now;
                db.Proveedores.Add(model);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Proveedor guardado correctamente",
                    proveedorId = model.Id 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }




    }
}