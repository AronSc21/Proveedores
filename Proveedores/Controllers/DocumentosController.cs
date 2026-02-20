using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Proveedores.Controllers
{
    public class DocumentosController : Controller
    {
        ProveedoresEntities db = new ProveedoresEntities();

        [HttpPost]
        public ActionResult SubirDocumentosMasivo(
    int proveedorId,
     int idTipoTramite,
    IEnumerable<HttpPostedFileBase> archivos,
    IEnumerable<int> tipos)
        {
            try
            {
                var listaArchivos = archivos.ToList();
                var listaTipos = tipos.ToList();

                if (listaArchivos.Count != listaTipos.Count)
                    return Json(new { success = false, message = "Datos inconsistentes" });

                string carpetaProveedor = $"TRIJAEV-{proveedorId}";
                string rutaBase = Server.MapPath("~/DocumentosProveedores/");
                string rutaFinal = Path.Combine(rutaBase, carpetaProveedor);

                if (!Directory.Exists(rutaFinal))
                    Directory.CreateDirectory(rutaFinal);

                for (int i = 0; i < listaArchivos.Count; i++)
                {
                    var archivo = listaArchivos[i];
                    var idTipoDocumento = listaTipos[i]; 

                    if (archivo == null || archivo.ContentLength == 0)
                        continue;

                    if (Path.GetExtension(archivo.FileName).ToLower() != ".pdf")
                        continue;

                    string nombreArchivo = $"{idTipoDocumento}_{DateTime.Now.Ticks}.pdf";
                    string rutaArchivo = Path.Combine(rutaFinal, nombreArchivo);

                    archivo.SaveAs(rutaArchivo);

                    var doc = new Documentos_Proveedor
                    {
                        IdProveedor = proveedorId,
                        IdTipoDocumento = idTipoDocumento, 
                        IdTipoTramite = idTipoTramite,
                        RutaArchivo = $"DocumentosProveedores/{carpetaProveedor}/{nombreArchivo}",
                        FechaCarga = DateTime.Now,
                        Activo = true,
                        Validado = false
                    };

                    db.Documentos_Proveedor.Add(doc);
                }

                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                var error = ex.InnerException != null
                    ? ex.InnerException.InnerException?.Message ?? ex.InnerException.Message
                    : ex.Message;

                return Json(new { success = false, message = error });
            }
        }


        [HttpPost]
        public ActionResult SubirDocumentos(
             int proveedorId,
             int idTipoDocumento,
             int idTipoTramite,
             HttpPostedFileBase archivo)
        {
            try
            {
                if (archivo == null || archivo.ContentLength == 0)
                    return Json(new { success = false, message = "Archivo no válido" });

                
                if (Path.GetExtension(archivo.FileName).ToLower() != ".pdf")
                    return Json(new { success = false, message = "Solo se permiten archivos PDF" });

                
                string carpetaProveedor = $"TRIJAEV-{proveedorId}";
                string rutaBase = Server.MapPath("~/DocumentosProveedores/");
                string rutaFinal = Path.Combine(rutaBase, carpetaProveedor);

                if (!Directory.Exists(rutaFinal))
                    Directory.CreateDirectory(rutaFinal);

                string nombreArchivo = $"{idTipoDocumento}_{DateTime.Now.Ticks}.pdf";
                string rutaArchivo = Path.Combine(rutaFinal, nombreArchivo);

                archivo.SaveAs(rutaArchivo);

                
                var doc = new Documentos_Proveedor
                {
                    IdProveedor = proveedorId,
                    IdTipoDocumento = idTipoDocumento,
                    IdTipoTramite = idTipoTramite,
                    RutaArchivo = $"DocumentosProveedores/{carpetaProveedor}/{nombreArchivo}",
                    FechaCarga = DateTime.Now,
                    Activo = true,
                    Validado = false
                };

                db.Documentos_Proveedor.Add(doc);
                db.SaveChanges();

                return Json(new { success = true, message = "Documento cargado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


    }


}
