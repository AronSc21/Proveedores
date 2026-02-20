using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Proveedores.Controllers
{
    public class LoginController : Controller
    {
        private ProveedoresEntities db = new ProveedoresEntities();

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(Usuarios_Proveedores model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuario = db.Usuarios_Proveedores
                .FirstOrDefault(u =>
                    u.usuario == model.usuario &&
                    u.contrasena == model.contrasena);

            if (usuario == null)
            {
                ViewBag.Error = "Usuario o contraseña incorrectos";
                return View(model);
            }

            // Guardar sesión
            Session["UsuarioId"] = usuario.id;
            Session["Usuario"] = usuario.usuario;
            Session["Rol"] = usuario.idRol;

            return RedirectToAction("Lista");
        }

      
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        public ActionResult Lista()
        {
            if (Session["UsuarioId"] == null)
                return RedirectToAction("Login");

            var proveedores = db.Proveedores

                .ToList();

            return View(proveedores);
        }

        public ActionResult Datos(int id)
        {
            if (Session["UsuarioId"] == null)
                return RedirectToAction("Login");

            var proveedor = db.Proveedores
                .FirstOrDefault(p => p.Id == id);

            if (proveedor == null)
                return HttpNotFound();

            return View(proveedor);
        }




    }
}