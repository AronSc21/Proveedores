app.controller('proveedoresController', function ($scope, $http) {

    // =========================================
    // CONTROL DE BLOQUEO DE BOTÓN
    // =========================================
    $scope.guardando = false;

    // =========================================
    // 1. MODELO DEL PROVEEDOR
    // =========================================
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

    // =========================================
    // 2. DOCUMENTOS
    // =========================================
    $scope.agregarDocumento = function (archivo, idTipoDocumento) {

        if (!archivo) return;

        if (archivo.type !== 'application/pdf') {
            Swal.fire({
                icon: 'warning',
                title: 'Incompatibilidad de archivos',
                text: 'Solo se permiten archivos PDF',
                confirmButtonText: 'OK',
                confirmButtonColor: '#28a745'
            });
            return;
        }

        var existe = $scope.documentos.some(d => d.idTipoDocumento === idTipoDocumento);

        if (existe) {
            return;
        }

        $scope.documentos.push({
            archivo: archivo,
            idTipoDocumento: idTipoDocumento
        });

        $scope.$applyAsync();
    };

    // =========================================
    // 3. GUARDAR TODO
    // =========================================
    $scope.guardarTodo = function () {

        if ($scope.guardando) return;

        // ===============================
        // VALIDACIÓN SOCIOS (MORAL)
        // ===============================
        if ($scope.proveedor.IdTipoPersona == 2) {
            if (!$scope.proveedor.Fax || $scope.proveedor.Fax.trim() === "") {
                Swal.fire({
                    icon: 'warning',
                    title: 'Campo obligatorio',
                    text: 'Debe capturar el nombre de socios y/o accionistas para persona moral',
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#28a745'
                });
                return;
            }
        }

        // ===============================
        // VALIDACIÓN CURP (FÍSICA)
        // ===============================
        if ($scope.proveedor.IdTipoPersona == 1) {
            if (!$scope.proveedor.CURP || $scope.proveedor.CURP.trim() === "") {
                Swal.fire({
                    icon: 'warning',
                    title: 'Campo obligatorio',
                    text: 'La CURP es obligatoria para Persona Física',
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#28a745'
                });
                return;
            }
        }

        // ===============================
        // VALIDACIÓN FORMULARIO
        // ===============================
        if ($scope.formRegistro.$invalid) {
            Swal.fire({
                icon: 'warning',
                title: 'Campos faltantes',
                text: 'Completa todos los campos obligatorios correctamente',
                confirmButtonText: 'OK',
                confirmButtonColor: '#28a745'
            });
            return;
        }

        // ===============================
        // BLOQUEAR BOTÓN
        // ===============================
        $scope.guardando = true;

        // ===============================
        // GUARDAR PROVEEDOR
        // ===============================
        $http.post('/Proveedores/GuardarProveedor', $scope.proveedor)

            .then(function (response) {

                if (!response.data.success) {
                    $scope.guardando = false;
                    return Promise.reject(response.data.message);
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

                if (!response.data.success) {
                    return Promise.reject(response.data.message);
                }

                Swal.fire({
                    icon: 'success',
                    title: 'Registro exitoso',
                    text: 'La información y documentos fueron guardados con éxito',
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#28a745'
                });

                window.open('/Proveedores/GenerarReporteProveedor?idProveedor=' + $scope.proveedorIdGenerado, '_blank');

                $scope.limpiarFormulario();
                $scope.guardando = false;
            })

            .catch(function (error) {

                console.error(error);

                $scope.guardando = false;

                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: error || 'Ocurrió un error al guardar. Intenta nuevamente.',
                    confirmButtonColor: '#dc3545'
                });
            });
    };

    // =========================================
    // 4. LIMPIAR FORMULARIO
    // =========================================
    $scope.limpiarFormulario = function () {

        $scope.proveedor = {
            IdTipoPersona: null
        };

        $scope.documentos = [];

        if ($scope.formRegistro) {
            $scope.formRegistro.$setPristine();
            $scope.formRegistro.$setUntouched();
        }

        // LIMPIAR INPUT FILE 
        setTimeout(function () {
            var inputs = document.querySelectorAll('.input-documento');
            inputs.forEach(function (input) {
                input.value = null;
            });
        }, 0);
    };

    // =========================================
    // 5. VALIDACIONES DE DOCUMENTOS
    // =========================================
    $scope.documentosRequeridos = function () {

        if ($scope.proveedor.IdTipoPersona == 1)
            return [1, 2, 3, 4, 5];

        if ($scope.proveedor.IdTipoPersona == 2)
            return [6, 7, 8, 9, 4, 5];

        return [];
    };

    $scope.formularioValido = function () {

        if (!$scope.proveedor.IdTipoPersona) return false;

        var requeridos = $scope.documentosRequeridos();
        var tiposSubidos = $scope.documentos.map(d => d.idTipoDocumento);

        return requeridos.every(r => tiposSubidos.includes(r));
    };

});
