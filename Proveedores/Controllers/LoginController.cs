using ClosedXML.Excel;
using Microsoft.Reporting.WebForms;
using Proveedores.Models;
using System;
using System.Collections.Generic;
using System.IO;
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
            ViewBag.Rol = Session["Rol"];
            ViewBag.DescargarValidacion = TempData["DescargarValidacion"];
            ViewBag.DescargarActualizacion = TempData["DescargarActualizacion"];
            ViewBag.DescargarValidacionRefrendo = TempData["DescargarValidacionRefrendo"];

            var proveedores = db.Proveedores.ToList();

            foreach (var p in proveedores)
            {
                // Último validado (para vigencia)
                var vigente = db.Refrendos
                    .Where(r => r.idProveedor == p.Id && r.fechaValidacion != null)
                    .OrderByDescending(r => r.fechaValidacion)
                    .FirstOrDefault();

                // Pendiente
                var pendiente = db.Refrendos
                    .Where(r => r.idProveedor == p.Id && r.fechaValidacion == null)
                    .FirstOrDefault();

                ViewData["Vigencia_" + p.Id] = vigente?.fechaVigencia;
                ViewData["Pendiente_" + p.Id] = pendiente != null;
            }

            return View(proveedores);
        }
        private string ObtenerNombreDocumento(int id)
        {
            switch (id)
            {
                case 1: return "Acta de Nacimiento";
                case 2: return "Identificación Oficial";
                case 3: return "Constancia de Situación Fiscal actualizada";
                case 4: return "Última declaración de impuestos";
                case 5: return "Último estado de cuenta bancario";
                case 6: return "Acta Constitutiva de la Sociedad inscrita en el Registro Público";
                case 7: return "Poder Notarial del Representante Legal o Apoderado inscrito en el Registro Público";
                case 8: return "Identificación Oficial del Representante Legal";
                case 9: return "Registro Federal de Contribuyentes";
                case 10: return "Expediente Completo";
                case 11: return "Pre-registro Firmado";
                case 12: return "Oficio de Alta Firmado";
                default: return "Documento desconocido";
            }
        }
        
        public ActionResult Datos(int id, string modo = "ver")

        {
            if (Session["UsuarioId"] == null)
                return RedirectToAction("Login");

            var proveedor = db.Proveedores.Find(id);

            if (proveedor == null)
                return HttpNotFound();

            ViewBag.Modo = modo;

            ////
            ///

            var documentos = db.Documentos_Proveedor
    .Where(d => d.IdProveedor == id && d.Activo == true)
    .Select(d => new
    {
        d.Id,
        d.FechaCarga,
        d.IdTipoDocumento
    })
    .ToList();

            var documentosConNombre = documentos
    .Select(d => new DocumentoVM
    {
        Id = d.Id,
        FechaCarga = d.FechaCarga,
        NombreDocumento = ObtenerNombreDocumento(d.IdTipoDocumento),
        IdTipoDocumento = d.IdTipoDocumento
    })
    .ToList();

            ViewBag.Documentos = documentosConNombre;
            // ===============================
            // DOCUMENTOS REQUERIDOS
            // ===============================
            List<int> docsRequeridos = new List<int>();

            if (proveedor.IdTipoPersona == 1)
            {
                docsRequeridos = new List<int> { 1, 2, 3, 4, 5, 10, 11, 12 };
            }
            else if (proveedor.IdTipoPersona == 2)
            {
                docsRequeridos = new List<int> { 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            }

            ViewBag.DocumentosRequeridos = docsRequeridos;

            var nombresDocs = new Dictionary<int, string>()
{
    {1, "Acta de Nacimiento"},
    {2, "Identificación Oficial"},
    {3, "Constancia de Situación Fiscal"},
    {4, "Última Declaración de Impuestos"},
    {5, "Estado de Cuenta Bancario"},
    {6, "Acta Constitutiva"},
    {7, "Poder Notarial"},
    {8, "Identificación Representante Legal"},
    {9, "RFC"},
    {10, "Expediente Completo"},
    {11, "Pre-registro Firmado"},
    {12, "Oficio de Alta Firmado"}
};

            ViewBag.NombresDocumentos = nombresDocs;
            return View(proveedor);
        }

        [HttpPost]
        public ActionResult ValidarProveedor(int id)
        {
            if (Session["UsuarioId"] == null)
                return RedirectToAction("Login");

            var proveedor = db.Proveedores.Find(id);

            if (proveedor == null)
                return HttpNotFound();

            proveedor.Fecha_Aceptacion = DateTime.Now;

            db.SaveChanges();
            // PRIMER REFRENDO
            DateTime hoy = DateTime.Now;

            Refrendos refrendo = new Refrendos()
            {
                idProveedor = proveedor.Id,
                fechaSolicitud = hoy,
                fechaValidacion = hoy,
                fechaVigencia = hoy.AddYears(1)
            };

            db.Refrendos.Add(refrendo);
            db.SaveChanges();
            TempData["DescargarValidacion"] = proveedor.Id;
            return RedirectToAction("Lista");
            //return RedirectToAction("Lista", new { descargarValidacion = proveedor.Id });
        }
        [HttpPost]
        public ActionResult ActualizarProveedor(Proveedores model, IEnumerable<HttpPostedFileBase> archivos, IEnumerable<int> tipos)
        {
            var proveedor = db.Proveedores.Find(model.Id);

            if (proveedor == null)
                return HttpNotFound();

            // ===============================
            // DETECTAR CAMBIOS EN DATOS
            // ===============================
            bool hayCambiosDatos = false;

            if (proveedor.CURP != model.CURP) hayCambiosDatos = true;
            if (proveedor.Estratificacion != model.Estratificacion) hayCambiosDatos = true;
            if (proveedor.Giro_Comercial != model.Giro_Comercial) hayCambiosDatos = true;

            if (proveedor.Calle != model.Calle) hayCambiosDatos = true;
            if (proveedor.Colonia != model.Colonia) hayCambiosDatos = true;
            if (proveedor.No_Exterior != model.No_Exterior) hayCambiosDatos = true;
            if (proveedor.No_Interior != model.No_Interior) hayCambiosDatos = true;
            if (proveedor.Codigo_Postal != model.Codigo_Postal) hayCambiosDatos = true;
            if (proveedor.Localidad != model.Localidad) hayCambiosDatos = true;
            if (proveedor.Municipio != model.Municipio) hayCambiosDatos = true;
            if (proveedor.Estado != model.Estado) hayCambiosDatos = true;

            if (proveedor.Telefono != model.Telefono) hayCambiosDatos = true;
            if (proveedor.Fax != model.Fax) hayCambiosDatos = true;
            if (proveedor.Pagina_Web != model.Pagina_Web) hayCambiosDatos = true;

            if (proveedor.Rep_Nombre != model.Rep_Nombre) hayCambiosDatos = true;
            if (proveedor.Rep_Telefono != model.Rep_Telefono) hayCambiosDatos = true;
            if (proveedor.Rep_Extension != model.Rep_Extension) hayCambiosDatos = true;
            if (proveedor.Rep_Correo_E != model.Rep_Correo_E) hayCambiosDatos = true;
            if (proveedor.Rep_Numero_Acta != model.Rep_Numero_Acta) hayCambiosDatos = true;
            if (proveedor.Rep_Numero_Poder != model.Rep_Numero_Poder) hayCambiosDatos = true;
            if (proveedor.Rep_Notario_Notaria != model.Rep_Notario_Notaria) hayCambiosDatos = true;
            if (proveedor.Rep_Personal_Tramite != model.Rep_Personal_Tramite) hayCambiosDatos = true;

            if (proveedor.Tra_Telefono != model.Tra_Telefono) hayCambiosDatos = true;
            if (proveedor.Tra_Extension != model.Tra_Extension) hayCambiosDatos = true;
            if (proveedor.Tra_Correo_E != model.Tra_Correo_E) hayCambiosDatos = true;

            if (proveedor.Ban_Banco != model.Ban_Banco) hayCambiosDatos = true;
            if (proveedor.Ban_Cuenta != model.Ban_Cuenta) hayCambiosDatos = true;
            if (proveedor.Ban_Sucursal != model.Ban_Sucursal) hayCambiosDatos = true;
            if (proveedor.Ban_Clabe != model.Ban_Clabe) hayCambiosDatos = true;


            // ===============================
            // ACTUALIZAR DATOS
            // ===============================
            proveedor.CURP = model.CURP;
            proveedor.Estratificacion = model.Estratificacion;
            proveedor.Giro_Comercial = model.Giro_Comercial;

            proveedor.Calle = model.Calle;
            proveedor.Colonia = model.Colonia;
            proveedor.No_Exterior = model.No_Exterior;
            proveedor.No_Interior = model.No_Interior;
            proveedor.Codigo_Postal = model.Codigo_Postal;
            proveedor.Localidad = model.Localidad;
            proveedor.Municipio = model.Municipio;
            proveedor.Estado = model.Estado;

            proveedor.Telefono = model.Telefono;
            proveedor.Fax = model.Fax;
            proveedor.Pagina_Web = model.Pagina_Web;

            proveedor.Rep_Nombre = model.Rep_Nombre;
            proveedor.Rep_Telefono = model.Rep_Telefono;
            proveedor.Rep_Extension = model.Rep_Extension;
            proveedor.Rep_Correo_E = model.Rep_Correo_E;
            proveedor.Rep_Numero_Acta = model.Rep_Numero_Acta;
            proveedor.Rep_Numero_Poder = model.Rep_Numero_Poder;
            proveedor.Rep_Notario_Notaria = model.Rep_Notario_Notaria;
            proveedor.Rep_Personal_Tramite = model.Rep_Personal_Tramite;

            proveedor.Tra_Telefono = model.Tra_Telefono;
            proveedor.Tra_Extension = model.Tra_Extension;
            proveedor.Tra_Correo_E = model.Tra_Correo_E;

            proveedor.Ban_Banco = model.Ban_Banco;
            proveedor.Ban_Cuenta = model.Ban_Cuenta;
            proveedor.Ban_Sucursal = model.Ban_Sucursal;
            proveedor.Ban_Clabe = model.Ban_Clabe;

            db.SaveChanges();


            // ===============================
            // DOCUMENTOS
            // ===============================


            bool hayCambiosDocumentos = false;

            if (archivos != null && tipos != null)
            {
                var listaArchivos = archivos.ToList();
                var listaTipos = tipos.ToList();

                hayCambiosDocumentos = listaArchivos.Any(a => a != null && a.ContentLength > 0);

                if (hayCambiosDocumentos)
                {
                    for (int i = 0; i < listaArchivos.Count; i++)
                    {
                        var archivo = listaArchivos[i];
                        var tipo = listaTipos[i];

                        if (archivo == null || archivo.ContentLength == 0)
                            continue;

                        var docsMismoTipo = db.Documentos_Proveedor
                            .Where(d => d.IdProveedor == model.Id
                                     && d.Activo
                                     && d.IdTipoDocumento == tipo)
                            .ToList();

                        foreach (var d in docsMismoTipo)
                        {
                            d.Activo = false;
                        }
                    }

                    string carpetaProveedor = $"TRIJAEV-{model.Id}";
                    string rutaBase = Server.MapPath("~/DocumentosProveedores/");
                    string rutaFinal = Path.Combine(rutaBase, carpetaProveedor);

                    if (!Directory.Exists(rutaFinal))
                        Directory.CreateDirectory(rutaFinal);

                    for (int i = 0; i < listaArchivos.Count; i++)
                    {
                        var archivo = listaArchivos[i];
                        var tipo = listaTipos[i];

                        if (archivo == null || archivo.ContentLength == 0)
                            continue;

                        if (Path.GetExtension(archivo.FileName).ToLower() != ".pdf")
                            continue;

                        string nombreArchivo = $"{tipo}_{DateTime.Now.Ticks}.pdf";
                        string rutaArchivo = Path.Combine(rutaFinal, nombreArchivo);

                        archivo.SaveAs(rutaArchivo);

                        var nuevoDoc = new Documentos_Proveedor
                        {
                            IdProveedor = model.Id,
                            IdTipoDocumento = tipo,
                            IdTipoTramite = 2,
                            RutaArchivo = $"DocumentosProveedores/{carpetaProveedor}/{nombreArchivo}",
                            FechaCarga = DateTime.Now,
                            Activo = true,
                            Validado = false
                        };

                        db.Documentos_Proveedor.Add(nuevoDoc);
                    }

                    db.SaveChanges();
                }
            }


            // ===============================
            // DECIDIR SI GENERAR REPORTE
            // ===============================
            if (hayCambiosDatos)
            {
                TempData["DescargarActualizacion"] = proveedor.Id;
            }

            return RedirectToAction("Lista");
        }

        




[HttpGet]
    public ActionResult ExportarExcelProveedores(string filtro)
    {
            var hoy = DateTime.Now.Date;

            var proveedores = db.Proveedores
                .Select(p => new
                {
                    p,
                    ultimoRefrendo = db.Refrendos
                        .Where(r => r.idProveedor == p.Id)
                        .OrderByDescending(r => r.fechaVigencia)
                        .FirstOrDefault()
                })
                .ToList();

            switch (filtro)
            {
                case "vigentes":
                    proveedores = proveedores.Where(x =>
                        x.ultimoRefrendo != null &&
                        x.ultimoRefrendo.fechaVigencia != null &&
                        x.ultimoRefrendo.fechaVigencia.Value.Date >= hoy &&
                        x.ultimoRefrendo.fechaValidacion != null   // !pendiente
                    ).ToList();
                    break;

                case "vencidos":
                    proveedores = proveedores.Where(x =>
                        x.ultimoRefrendo != null &&
                        x.ultimoRefrendo.fechaVigencia != null &&
                        x.ultimoRefrendo.fechaVigencia.Value.Date < hoy
                    ).ToList();
                    break;

                case "pendientes":
                    proveedores = proveedores.Where(x =>
                        x.ultimoRefrendo != null &&
                        x.ultimoRefrendo.fechaValidacion == null
                    ).ToList();
                    break;

                case "todos":
                default:
                    break;
            }

            using (var workbook = new XLWorkbook())
        {
            var ws = workbook.Worksheets.Add("Proveedores");

            int fila = 1;

            // =========================
            // ENCABEZADO
            // =========================
            string titulo = "LISTADO DEL PADRÓN DE PROVEEDORES";

            if (filtro == "vigentes") titulo += " - VIGENTES";
            else if (filtro == "vencidos") titulo += " - VENCIDOS";
            else if (filtro == "pendientes") titulo += " - PENDIENTES";

            ws.Cell(fila, 1).Value = titulo;
            ws.Range(fila, 1, fila, 40).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            fila++;

            // =========================
            // ENCABEZADOS
            // =========================
            string[] headers = new string[]
            {
            "No. Proveedor","Razón Social","RFC","CURP","Personalidad jurídica",
            "Calle","Colonia","No Exterior","No Interior","C.P.","Localidad","Municipio","Estado",
            "Teléfono","Fax","Página Web",
            "Estratificación","Giro Comercial",
            "Nombre del representante legal","Teléfono del representante legal","Extensión del representante legal","Correo Electrónico del representante legal",
            "Número de acta","Número de poder","Notaría","Personal designado para trámites ante la institución",
            "Teléfono del personal designado para trámites ante la institución","Extensión del personal designado para trámites ante la institución","Correo electrónico del personal designado para trámites ante la institución",
            "Banco","Cuenta","CLABE",
            "Fecha Solicitud","Fecha Aceptación","Fecha Vigencia",
            "Vigente","Validado"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(fila, i + 1).Value = headers[i];
            }

            ws.Range(fila, 1, fila, headers.Length).Style.Font.Bold = true;

            fila++;

            // =========================
            // DATOS
            // =========================
            foreach (var item in proveedores)
            {
                var p = item.p;
                var r = item.ultimoRefrendo;

                DateTime? vigencia = r?.fechaVigencia;
                bool vigente = vigencia != null && vigencia > hoy;
                bool validado = r?.fechaValidacion != null;

                int col = 1;

                ws.Cell(fila, col++).Value = $"TRIJAEV-{p.Id}";
                ws.Cell(fila, col++).Value = p.Razon_Social;
                ws.Cell(fila, col++).Value = p.RFC;
                ws.Cell(fila, col++).Value = p.CURP;
                ws.Cell(fila, col++).Value = p.IdTipoPersona == 1 ? "Física" : "Moral";

                ws.Cell(fila, col++).Value = p.Calle;
                ws.Cell(fila, col++).Value = p.Colonia;
                ws.Cell(fila, col++).Value = p.No_Exterior;
                ws.Cell(fila, col++).Value = p.No_Interior;
                ws.Cell(fila, col++).Value = p.Codigo_Postal;
                ws.Cell(fila, col++).Value = p.Localidad;
                ws.Cell(fila, col++).Value = p.Municipio;
                ws.Cell(fila, col++).Value = p.Estado;

                ws.Cell(fila, col++).Value = p.Telefono;
                ws.Cell(fila, col++).Value = p.Fax;
                ws.Cell(fila, col++).Value = p.Pagina_Web;

                ws.Cell(fila, col++).Value = p.Estratificacion;
                ws.Cell(fila, col++).Value = p.Giro_Comercial;

                ws.Cell(fila, col++).Value = p.Rep_Nombre;
                ws.Cell(fila, col++).Value = p.Rep_Telefono;
                ws.Cell(fila, col++).Value = p.Rep_Extension;
                ws.Cell(fila, col++).Value = p.Rep_Correo_E;

                ws.Cell(fila, col++).Value = p.Rep_Numero_Acta;
                ws.Cell(fila, col++).Value = p.Rep_Numero_Poder;
                ws.Cell(fila, col++).Value = p.Rep_Notario_Notaria;
                ws.Cell(fila, col++).Value = p.Rep_Personal_Tramite;

                ws.Cell(fila, col++).Value = p.Tra_Telefono;
                ws.Cell(fila, col++).Value = p.Tra_Extension;
                ws.Cell(fila, col++).Value = p.Tra_Correo_E;

                ws.Cell(fila, col++).Value = p.Ban_Banco;
                ws.Cell(fila, col++).Value = p.Ban_Cuenta;
                ws.Cell(fila, col++).Value = p.Ban_Clabe;

                ws.Cell(fila, col++).Value = p.Fecha_Solicitud?.Date;
                ws.Cell(fila, col++).Value = p.Fecha_Aceptacion?.Date;
                ws.Cell(fila, col++).Value = vigencia?.Date;

                ws.Cell(fila, col++).Value = vigente ? "Sí" : "No";
                ws.Cell(fila, col++).Value = validado ? "Sí" : "No";

                fila++;
            }

            // FORMATO FECHAS
            ws.Column(33).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Column(34).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Column(35).Style.DateFormat.Format = "dd/MM/yyyy";

            ws.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                stream.Position = 0;

                return File(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Proveedores_{filtro}.xlsx"
                );
            }
        }
    }
    [HttpGet]
        public ActionResult ValidarRefrendo(int id)
        {
            if (Session["UsuarioId"] == null)
                return RedirectToAction("Login");

            var proveedor = db.Proveedores.Find(id);

            if (proveedor == null)
                return HttpNotFound();

            var solicitudPendiente = db.Refrendos
                .Where(r => r.idProveedor == id && r.fechaValidacion == null)
                .OrderByDescending(r => r.fechaSolicitud)
                .FirstOrDefault();

            if (solicitudPendiente == null)
                return RedirectToAction("Lista");

            ViewBag.Proveedor = proveedor;
            ViewBag.Refrendo = solicitudPendiente;

            return View();
        }

        [HttpPost]
        public ActionResult ConfirmarValidacionRefrendo(int idRefrendo)
        {
            if (Session["UsuarioId"] == null)
                return RedirectToAction("Login");

            var refrendo = db.Refrendos.Find(idRefrendo);

            if (refrendo == null)
                return RedirectToAction("Lista");

            DateTime hoy = DateTime.Now;

            refrendo.fechaValidacion = hoy;
            refrendo.fechaVigencia = hoy.AddYears(1);
            db.SaveChanges();

            TempData["DescargarValidacionRefrendo"] = refrendo.idProveedor;

            return RedirectToAction("Lista");
        }

        [HttpGet]
        public ActionResult GenerarReporteValidacion(int idProveedor)
        {
            var proveedor = db.Proveedores
                .Where(p => p.Id == idProveedor)
                .Select(p => new
                {
                    p.Id,
                    p.RFC,
                    p.Razon_Social,
                    p.Fecha_Aceptacion
                })
                .ToList();

            LocalReport reporte = new LocalReport();

            string rutaReporte = Server.MapPath("~/Reportes/validacion_alta.rdlc");

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

            return File(bytes, "application/pdf", "ValidacionProveedor.pdf");
        }
        [HttpGet]
        public ActionResult GenerarReporteActualizacion(int idProveedor)
        {
            var proveedor = db.Proveedores
                .Where(p => p.Id == idProveedor)
                .ToList();

            LocalReport reporte = new LocalReport();

            string rutaReporte = Server.MapPath("~/Reportes/modificacion.rdlc");

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

            return File(bytes, "application/pdf", "ActualizacionProveedor.pdf");
        }

        [HttpGet]
        public ActionResult GenerarReporteValidacionRefrendo(int idProveedor)
        {
            var proveedor = db.Proveedores
                .Where(p => p.Id == idProveedor)
                .ToList();

            LocalReport reporte = new LocalReport();

            string rutaReporte = Server.MapPath("~/Reportes/validacion_refrendo.rdlc");

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

            return File(bytes, "application/pdf", "ValidacionRefrendo.pdf");
        }

    }
}