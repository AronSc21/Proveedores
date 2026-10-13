using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        public ActionResult Refrendo()
        {
            ViewBag.DescargarRenovacion = TempData["DescargarRenovacion"];

            ViewBag.Error = TempData["Error"];

            ViewBag.IdProveedor = TempData["IdProveedor"];
            ViewBag.RazonSocial = TempData["RazonSocial"];
            ViewBag.Vigencia = TempData["Vigencia"];
            ViewBag.DiasRestantes = TempData["DiasRestantes"];
            ViewBag.PuedeRefrendar = TempData["PuedeRefrendar"];
            ViewBag.SolicitudPendiente = TempData["SolicitudPendiente"];

            return View();
        }
        //public ActionResult Refrendo()
        //{
        //    ViewBag.DescargarRenovacion = TempData["DescargarRenovacion"];
        //    return View();
        //}

        [HttpPost]
        public ActionResult GuardarProveedor(Proveedores model)
        //public async Task<ActionResult> GuardarProveedor(Proveedores model, string recaptchaToken)
        {
            try
            {
                //// VALIDAR RECAPTCHA
                //if (!await ValidarRecaptcha(recaptchaToken))
                //{
                //    return Json(new { success = false, message = "Error de seguridad (reCAPTCHA)" });
                //}

                // VALIDACIÓN NORMAL
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


        //private async Task<bool> ValidarRecaptcha(string token)
        //{
        //    var secretKey = "6LeZrLUsAAAAAIZMJPmh8yGXn3CeVOalPVK0JB5l";

        //    using (var client = new System.Net.Http.HttpClient())
        //    {
        //        var response = await client.PostAsync(
        //            $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
        //            null);

        //        var json = await response.Content.ReadAsStringAsync();

        //        dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

        //        return data.success == true && data.score >= 0.5;
        //    }
        //}

        [HttpGet]
        public ActionResult GenerarReporteProveedor(int idProveedor)
        {
            var proveedor = db.Proveedores
                .Where(p => p.Id == idProveedor)
                .ToList();

            LocalReport reporte = new LocalReport();

            string rutaReporte = Server.MapPath("~/Reportes/pre-registro.rdlc");

            reporte.ReportPath = rutaReporte;

            ReportDataSource rds = new ReportDataSource(
                "ProveedoresDS",
                proveedor
            );

            reporte.DataSources.Clear();
            reporte.DataSources.Add(rds);

            string mimeType;
            string encoding;
            string fileNameExtension;
            string[] streams;
            Warning[] warnings;

            byte[] bytes = reporte.Render(
                "PDF",
                null,
                out mimeType,
                out encoding,
                out fileNameExtension,
                out streams,
                out warnings
            );

            return File(bytes, "application/pdf", "PreRegistroProveedor.pdf");
        }

        [HttpGet]
        public ActionResult GenerarReporteRenovacion(int idProveedor)
        {
            var proveedor = db.Proveedores
                .Where(p => p.Id == idProveedor)
                .ToList();

            LocalReport reporte = new LocalReport();

            string rutaReporte = Server.MapPath("~/Reportes/renovacion.rdlc");

            reporte.ReportPath = rutaReporte;

            ReportDataSource rds = new ReportDataSource(
                "ProveedoresDS",
                proveedor
            );

            reporte.DataSources.Clear();
            reporte.DataSources.Add(rds);

            string mimeType;
            string encoding;
            string fileNameExtension;
            string[] streams;
            Warning[] warnings;

            byte[] bytes = reporte.Render(
                "PDF",
                null,
                out mimeType,
                out encoding,
                out fileNameExtension,
                out streams,
                out warnings
            );

            return File(bytes, "application/pdf", "RenovacionProveedor.pdf");
        }
        [HttpPost]
        public ActionResult BuscarRefrendo(string IdProveedor, string RFC)
        {
            string idLimpio = IdProveedor.Replace("TRIJAEV-", "");

            int id;

            if (!int.TryParse(idLimpio, out id))
            {
                TempData["Error"] = "Número de proveedor inválido";
                return RedirectToAction("Refrendo");
            }

            var proveedor = db.Proveedores
                .FirstOrDefault(p =>
                    p.Id == id &&
                    p.RFC == RFC &&
                    p.Fecha_Aceptacion != null);

            if (proveedor == null)
            {
                TempData["Error"] = "Proveedor no encontrado";
                return RedirectToAction("Refrendo");
            }

            // Verificar solicitud pendiente
            var solicitudPendiente = db.Refrendos
                .FirstOrDefault(r =>
                    r.idProveedor == proveedor.Id &&
                    r.fechaValidacion == null);

            var refrendo = db.Refrendos
                .Where(r => r.idProveedor == proveedor.Id)
                .OrderByDescending(r => r.fechaVigencia)
                .FirstOrDefault();

            bool puedeRefrendar = false;
            int? diasRestantes = null;

            if (refrendo != null && refrendo.fechaVigencia != null)
            {
                diasRestantes =
                    (refrendo.fechaVigencia.Value - DateTime.Now).Days;

                if (diasRestantes <= 30 && solicitudPendiente == null)
                {
                    puedeRefrendar = true;
                }
            }

            // GUARDAR EN TEMPDATA
            TempData["IdProveedor"] = proveedor.Id;
            TempData["RazonSocial"] = proveedor.Razon_Social;
            TempData["Vigencia"] = refrendo?.fechaVigencia;
            TempData["DiasRestantes"] = diasRestantes;
            TempData["PuedeRefrendar"] = puedeRefrendar;
            TempData["SolicitudPendiente"] = solicitudPendiente != null;

            return RedirectToAction("Refrendo");
        }
        //[HttpPost]
        //public ActionResult BuscarRefrendo(string IdProveedor, string RFC)
        //{
        //    string idLimpio = IdProveedor.Replace("TRIJAEV-", "");

        //    int id;
        //    if (!int.TryParse(idLimpio, out id))
        //    {
        //        ViewBag.Error = "Número de proveedor inválido";
        //        return View("Refrendo");
        //    }

        //    var proveedor = db.Proveedores
        //        .FirstOrDefault(p =>
        //            p.Id == id &&
        //            p.RFC == RFC &&
        //            p.Fecha_Aceptacion != null);

        //    if (proveedor == null)
        //    {
        //        ViewBag.Error = "Proveedor no encontrado";
        //        return View("Refrendo");
        //    }

        //    // Verificar solicitud pendiente
        //    var solicitudPendiente = db.Refrendos
        //        .FirstOrDefault(r =>
        //            r.idProveedor == proveedor.Id &&
        //            r.fechaValidacion == null);

        //    ViewBag.SolicitudPendiente =
        //        solicitudPendiente != null;

        //    var refrendo = db.Refrendos
        //        .Where(r => r.idProveedor == proveedor.Id)
        //        .OrderByDescending(r => r.fechaVigencia)
        //        .FirstOrDefault();

        //    ViewBag.IdProveedor = proveedor.Id;
        //    ViewBag.RazonSocial = proveedor.Razon_Social;
        //    ViewBag.Vigencia = refrendo?.fechaVigencia;

        //    bool puedeRefrendar = false;

        //    if (refrendo != null && refrendo.fechaVigencia != null)
        //    {
        //        int diasRestantes =
        //            (refrendo.fechaVigencia.Value - DateTime.Now).Days;

        //        ViewBag.DiasRestantes = diasRestantes;

        //        if (diasRestantes <= 30 && solicitudPendiente == null)
        //        {
        //            puedeRefrendar = true;
        //        }
        //    }

        //    ViewBag.PuedeRefrendar = puedeRefrendar;

        //    return View("Refrendo");
        //}

        [HttpPost]
        public ActionResult GenerarRefrendo(int idProveedor)
        //public async Task<ActionResult> GenerarRefrendo(int idProveedor, string recaptchaToken)
        {
            //// 🔐 VALIDAR RECAPTCHA
            //if (!await ValidarRecaptcha(recaptchaToken))
            //{
            //    TempData["Error"] = "Error de seguridad (reCAPTCHA)";
            //    return RedirectToAction("Refrendo");
            //}

            var pendiente = db.Refrendos
                .FirstOrDefault(r =>
                    r.idProveedor == idProveedor &&
                    r.fechaValidacion == null);

            if (pendiente != null)
            {
                TempData["Error"] = "Ya existe una solicitud pendiente.";
                return RedirectToAction("Refrendo");
            }

            var ultimoRefrendo = db.Refrendos
                .Where(r => r.idProveedor == idProveedor)
                .OrderByDescending(r => r.fechaVigencia)
                .FirstOrDefault();

            if (ultimoRefrendo != null && ultimoRefrendo.fechaVigencia != null)
            {
                int diasRestantes =
                    (ultimoRefrendo.fechaVigencia.Value - DateTime.Now).Days;

                if (diasRestantes > 30)
                {
                    TempData["Error"] = "Aún no puede solicitar refrendo.";
                    return RedirectToAction("Refrendo");
                }
            }

            Refrendos nuevo = new Refrendos()
            {
                idProveedor = idProveedor,
                fechaSolicitud = DateTime.Now
            };

            db.Refrendos.Add(nuevo);
            db.SaveChanges();

            TempData["Exito"] = "Solicitud generada correctamente.";
            TempData["DescargarRenovacion"] = idProveedor;

            return RedirectToAction("Refrendo");
        }

    }
}