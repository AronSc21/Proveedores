    app.controller('proveedoresController', function ($scope, $http) {

        /* 1. MODELO DEL PROVEEDOR */

        $scope.proveedor = {

            IdTipoPersona: null, 

            // Datos generales
            Razon_Social: '',
            RFC: '',
            CURP: '',
            Giro_Comercial: '',
            Fecha_Solicitud: '',

            // Domicilio
            Calle: '',
            Colonia: '',
            No_Exterior: '',
            No_Interior: '',
            Codigo_Postal: '',
            Localidad: '',
            Municipio: '',
            Estado: '',

            // Contacto
            Telefono: '',
            Fax: '',
            Pagina_Web: '',

            // Representante
            Rep_Nombre: '',
            Rep_Telefono: '',
            Rep_Extension: '',
            Rep_Correo_E: '',
            Rep_Numero_Acta: '',
            Rep_Numero_Poder: '',
            Rep_Notario_Notaria: '',
            Rep_Personal_Tramite: '',

            // Bancarios
            Ban_Banco: '',
            Ban_Cuenta: '',
            Ban_Sucursal: '',
            Ban_Clabe: ''
        };


        /* 2. SELECCIÓN DE TIPO DE PERSONA */

        $scope.seleccionarPersona = function (tipo) {
            if (tipo === 'F') {
                $scope.proveedor.IdTipoPersona = 1;
            }
            if (tipo === 'M') {
                $scope.proveedor.IdTipoPersona = 2;
            }
        };


        /* 3. DOCUMENTOS */

        $scope.documentos = [];

        $scope.agregarDocumento = function (archivo, idTipoDocumento) {

            if (!archivo) return;

            
            if (archivo.type !== 'application/pdf') {
                alert('Solo se permiten archivos PDF');
                return;
            }

            $scope.documentos.push({
                archivo: archivo,
                idTipoDocumento: idTipoDocumento
            });

            
            $scope.$applyAsync();
        };



        /* 4. GUARDAR TODO */

        $scope.guardarTodo = function () {

            if (!$scope.proveedor.IdTipoPersona) {
                alert('Debe seleccionar si es Persona Física o Moral');
                return;
            }

            
            $http.post('/Proveedores/GuardarProveedor', $scope.proveedor)
                .then(function (response) {

                    if (!response.data.success) {
                        alert(response.data.message);
                        throw 'Error proveedor';
                    }

                    var proveedorId = response.data.proveedorId;

                    
                    if ($scope.documentos.length === 0) {
                        alert('Debe adjuntar al menos un documento');
                        throw 'Sin documentos';
                    }

                    
                    var formData = new FormData();
                    formData.append('proveedorId', proveedorId);
                    formData.append('idTipoTramite', 1);

                    angular.forEach($scope.documentos, function (doc, index) {
                        formData.append('archivos', doc.archivo);
                        formData.append('tipos', doc.idTipoDocumento);
                    });

                    
                    return $http.post('/Documentos/SubirDocumentosMasivo', formData, {
                        transformRequest: angular.identity,
                        headers: { 'Content-Type': undefined }
                    });
                })
                .then(function (response) {
                    if (response.data.success) {
                        alert('Proveedor y documentos guardados correctamente');
                        $scope.limpiarFormulario();
                    } else {
                        alert(response.data.message);
                    }
                })
                .catch(function (err) {
                    console.error(err);
                    alert('Error al guardar');
                });
        };



        /* 5. LIMPIAR FORMULARIO*/

        $scope.limpiarFormulario = function () {

            $scope.proveedor = {
                IdTipoPersona: null
            };

            $scope.documentos = [];
        };

    });
