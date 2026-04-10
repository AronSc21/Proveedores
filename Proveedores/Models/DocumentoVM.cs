using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proveedores.Models
{
    public class DocumentoVM
    {
        public int Id { get; set; }
        public DateTime FechaCarga { get; set; }
        public string NombreDocumento { get; set; }
        public int IdTipoDocumento { get; set; }
    }
}