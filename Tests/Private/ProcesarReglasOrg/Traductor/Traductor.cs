using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Traductor
{
    public class Traductor
    {
        public const string COD_ERROR = AnalizadorSintáctico.COD_ERROR;
        public const string ERROR_ORG_EXCLUYENTE = AnalizadorSintáctico.ERROR_ORG_EXCLUYENTE;
        public const string ERROR_ORG_NO_EXCLUYENTE = AnalizadorSintáctico.ERROR_ORG_NO_EXCLUYENTE;
        public const string ERROR_ORG_INDEFINIDO = AnalizadorSintáctico.ERROR_ORG_INDEFINIDO;
        public const string VALOR_ERROR_INDEFINIDO = AnalizadorSintáctico.VALOR_ERROR_INDEFINIDO;

        public enum EParteAnalizada
        {
            Nada = 0,
            Cabecera = 1,
            Detalle = 2
        }

        public List<Bloque> Traducir(string cadenaParaTraducir, EParteAnalizada eParteAnalizada, bool mostrarPorPantallaLasSentenciasGeneradas = false)
        {
            List<Bloque> listBloque = new List<Bloque>();
            AnalizadorSintáctico analizadorSintáctico = new AnalizadorSintáctico();

            string source = cadenaParaTraducir;
            Sentencia sentencia = GetSentencia(eParteAnalizada, analizadorSintáctico, source);
            while (sentencia != null)
            {
                if (mostrarPorPantallaLasSentenciasGeneradas)
                {
                    Console.WriteLine($"Descripción Bloque:{sentencia.DescripciónBloque}");
                    Console.WriteLine($"Error Natura:{sentencia.ErrorNatura}");
                    Console.WriteLine($"Tipo Error ORG:{sentencia.TipoErroOrg}");
                    Console.WriteLine($"Error ORG:{sentencia.ErrorOrg}");
                    Console.WriteLine();
                }

                Bloque bloque = new Bloque(sentencia);
                listBloque.Add(bloque);

                if ((sentencia.PosFinSentencia + 1) < source.Length)
                {
                    source = source.Substring(sentencia.PosFinSentencia + 1).Trim();
                    sentencia = GetSentencia(eParteAnalizada, analizadorSintáctico, source);
                }
                else
                {
                    sentencia = null;
                }
            }

            return listBloque;
        }

        private static Sentencia GetSentencia(EParteAnalizada eParteAnalizada, AnalizadorSintáctico analizadorSintáctico, string source)
        {
            return (eParteAnalizada == EParteAnalizada.Cabecera) ? analizadorSintáctico.GetSentenciaCabecera(source) :
                            (eParteAnalizada == EParteAnalizada.Detalle) ? analizadorSintáctico.GetSentenciaDetalle(source) 
                            : throw new Exception("Parte del comprobante a analizar, no definida");
        }
    }
}
