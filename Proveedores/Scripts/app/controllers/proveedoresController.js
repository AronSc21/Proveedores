
app.controller('proveedoresController', function ($scope, $http) {

    /* 1. MODELO DEL PROVEEDOR */

    $scope.proveedor = {

        IdTipoPersona: null,

        // Datos generales
        Razon_Social: '',
        RFC: '',
        CURP: '',
        Giro_Comercial: '',
        Estratificacion: '',
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
        Tra_Telefono: '',
        Tra_Extension: '',
        Tra_Correo_E: '',

        // Bancarios
        Ban_Banco: '',
        Ban_Cuenta: '',
        Ban_Sucursal: '',
        Ban_Clabe: ''
    };

    $scope.documentos = [];
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

    $scope.agregarDocumento = function (archivo, idTipoDocumento) {

        if (!archivo) return;

        if (archivo.type !== 'application/pdf') {
            alert('Solo se permiten archivos PDF');
            return;
        }

        var existe = $scope.documentos.some(d => d.idTipoDocumento === idTipoDocumento);

        if (existe) {
            alert('Este documento ya fue cargado');
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

        // VALIDAR FORMULARIO
        if ($scope.formRegistro.$invalid) {
            alert('Completa todos los campos obligatorios correctamente');
            return;
        }

        

        // GUARDAR PROVEEDOR
        $http.post('/Proveedores/GuardarProveedor', $scope.proveedor)
            .then(function (response) {

                if (!response.data.success) {
                    alert(response.data.message);
                    throw 'Error proveedor';
                }

                var proveedorId = response.data.proveedorId;
                $scope.proveedorIdGenerado = proveedorId;

                var formData = new FormData();
                formData.append('proveedorId', proveedorId);
                formData.append('idTipoTramite', 1);

                angular.forEach($scope.documentos, function (doc) {
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

                    window.open('/Proveedores/GenerarReporteProveedor?idProveedor=' + $scope.proveedorIdGenerado, '_blank');

                    $scope.limpiarFormulario();

                } else {
                    alert(response.data.message);
                }

            });

    };


    /* 5. LIMPIAR FORMULARIO */

    $scope.limpiarFormulario = function () {

        $scope.proveedor = {
            IdTipoPersona: null
        };

        $scope.documentos = [];
    };

    $scope.documentosRequeridos = function () {

        if ($scope.proveedor.IdTipoPersona == 1) {
            return [1, 2, 3, 4, 5];
        }

        if ($scope.proveedor.IdTipoPersona == 2) {
            return [6, 7, 8, 9, 4, 5];
        }

        return [];
    };

    $scope.validarDocumentos = function () {

        var requeridos = $scope.documentosRequeridos();

        var cargados = $scope.documentos.map(d => d.idTipoDocumento);

        return requeridos.every(r => cargados.includes(r));
    };


    $scope.formularioValido = function () {

        // VALIDAR CAMPOS
        //for (var prop in $scope.proveedor) {

        //    if ($scope.proveedor[prop] === null || $scope.proveedor[prop] === '') {
        //        return false;
        //    }
        //}

        // VALIDAR DOCUMENTOS SEGÚN TIPO
        if ($scope.proveedor.IdTipoPersona == 1) {
            // Persona Física necesita 1-5
            var requeridos = [1, 2, 3, 4, 5];
        } else if ($scope.proveedor.IdTipoPersona == 2) {
            // Persona Moral necesita 4,5,6,7,8,9
            var requeridos = [4, 5, 6, 7, 8, 9];
        } else {
            return false;
        }

        var tiposSubidos = $scope.documentos.map(d => d.idTipoDocumento);

        for (var i = 0; i < requeridos.length; i++) {
            if (!tiposSubidos.includes(requeridos[i])) {
                return false;
            }
        }

        return true;
    };
});
