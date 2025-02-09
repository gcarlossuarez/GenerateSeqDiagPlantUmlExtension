using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Traductor;

namespace ConsoleAppProbarTraductor
{
    class Program
    {
        static void Main(string[] args)
        {

            string cadenaEjemplo = @"Formato AAAAMMDD .
COD_ERROR=75
R_AFIP_E=?
Debe estar comprendida dentro de la fecha desde y 
fecha hasta de vigencia del CAEA.
COD_ERROR=76
R_AFIP_E=702

Debe ser mayor o igual a la fecha del último
comprobante informado para el mismo tipo de 
comprobante y punto de venta. 
COD_ERROR=76
R_AFIP_E=704
";
            /*cadenaEjemplo = @"Formato AAAAMMDD .


R_AFIP_E=?
Debe estar comprendida dentro de la fecha desde y 
fecha hasta de vigencia del CAEA.
COD_ERROR=76
R_AFIP_E=702


R_AFIP_NE=704
";
            cadenaEjemplo = @"R_AFIP_E=?Debe estar comprendida dentro de la fecha desde y fecha hasta de vigencia del CAEA.COD_ERROR=76R_AFIP_E=702


R_AFIP_I   =   704
";*/
            cadenaEjemplo = @"Formato AAAAMMDD";
            Traductor.Traductor traductor = new Traductor.Traductor();
            traductor.Traducir(cadenaEjemplo, Traductor.Traductor.EParteAnalizada.Cabecera);

            cadenaEjemplo = @"Código de Producto/Servicio. Deberán corresponder a la estructura provista por la ASOCIACION ARGENTINA DE CODIFICACION DE PRODUCTOS COMERCIALES —CODIGO—, códigos GTIN 13, GTIN 12 y GTIN 8, correspondientes a la unidad de consumo minorista o presentación al consumidor final. 
COD_ERROR= 0Q
R_AFIP_NE=1104
";
            cadenaEjemplo = @"
Si tipo de comprobante es 2 o 3, Cantidad – No debe informarse
COD_ERROR=3.
R_AFIP_E=?
";
            // NOTA.- Lo que hay después de "COD_ERROR=3"; o sea "." (COD_ERROR=3.R_AFIP_E=?), sirve de contención, pra el caso especial de número de un
            // dígito sin letras al lado y, en este caso, no afecta; ya que se considera como descripción de "R_AFIP_E"; que, en este caso, se ignora, y
            // es lo que nos conviene. 
            // Lo mismo pasa con "COD_ERROR=3.aaaR_AFIP_E=?".
            cadenaEjemplo = @"
Si tipo de comprobante es 2 o 3, Cantidad – No debe informarse
COD_ERROR=3.aaa
R_AFIP_E=?
";
            traductor.Traducir(cadenaEjemplo, Traductor.Traductor.EParteAnalizada.Detalle, true);
            Console.WriteLine("Pulse una tecla, para continuar...");
            Console.ReadKey();
        }

    }
}
