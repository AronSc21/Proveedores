app.controller('proveedoresController', function ($scope, $http) {

    $scope.proveedor = {
        Razon_Social: '',
        RFC: '',
        CURP: '',
        Giro_Comercial: '',
        Persona_Fisica: false,
        Persona_Moral: false,
        Calle: '',
        Colonia: '',
        No_Exterior: '',
        No_Interior: '',
        Codigo_Postal: '',
        Localidad: '',
        Municipio: '',
        Estado: '',
        Telefono: '',
        Fax: '',
        Pagina_Web: '',
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
        Ban_Banco: '',
        Ban_Cuenta: '',
        Ban_Sucursal: '',
        Ban_Clabe: ''
    };

    $scope.guardar = function () {

        $http.post('/Proveedores/Registro', $scope.proveedor)
            .then(function (response) {

                if (response.data.ok) {
                    alert(response.data.mensaje);
                    $scope.proveedor = {}; // limpiar formulario
                } else {
                    alert(response.data.mensaje);
                }

            }, function () {
                alert('Error al enviar la solicitud');
            });
    };
});


