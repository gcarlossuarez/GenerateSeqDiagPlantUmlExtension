using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using ConsoleAppTestAfip.MTXCA;

namespace ConsoleAppTestAfip
{
    class Program
    {
        static void Main(string[] args)
        {
            string salir = String.Empty;

            do
            {
                //EjecutarProcesoConsultarComprobante();
                EjecutarProcesoConsultarCbte(3);
                Console.Write("Desea salir(S)?:");
                salir = Console.ReadLine();
            } while (salir?.Trim().ToUpper() != "S");

            Console.Write("Pulse una tecla, para continuar...");
            Console.ReadKey();
        }

		private static void EjecutarProcesoConsultarCbte(int x, string a)
		{
			Console.WriteLine(x + a);
		}
		
		private static void EjecutarProcesoConsultarCbte(out int x, string a)
		{
			x = 8;
			Console.WriteLine(x + a);
		}
		
		private static void EjecutarProcesoConsultarCbte(ref int x, string a)
		{
			++x;
			Console.WriteLine(x + a);
		}

        private static void EjecutarProcesoConsultarCbte(int xx)
        {
            long ultimoComp = 0;

            //ServicePointManager.SecurityProtocol = (SecurityProtocolType)768; // TLS 1.1
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            //System.Net.SecurityProtocolType.Tls11;
            //System.Net.SecurityProtocolType.Tls;
            //System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;

            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(Utiles.Config.GetValueInConfigFileDinamically("FullPathXmlRequest"));

                AuthRequestType authRequest = new AuthRequestType()
                {
                    cuitRepresentada = long.Parse(xmlDocument.GetElementsByTagName("Cuit")?[0].InnerText),
                    sign = xmlDocument.GetElementsByTagName("Sign")?[0].InnerText,
                    token = xmlDocument.GetElementsByTagName("Token")?[0].InnerText,
                };
                ConsultaUltimoComprobanteAutorizadoRequestType consultaUltimoComprobanteAutorizadoRequestType =
                    new ConsultaUltimoComprobanteAutorizadoRequestType()
                    {
                        codigoTipoComprobante = short.Parse(xmlDocument.GetElementsByTagName("Cbte_tipo")?[0].InnerText),
                        numeroPuntoVenta = int.Parse(xmlDocument.GetElementsByTagName("Punto_vta")?[0].InnerText),
                    };

                CodigoDescripcionType[] errores;

                string endpointName = Utiles.Config.GetValueInConfigFileDinamically("EndpointConfigurationName");
                MTXCAServicePortTypeClient mtxcaServicePortTypeClient = new MTXCAServicePortTypeClient(endpointName);
                mtxcaServicePortTypeClient.Open();

                ultimoComp = mtxcaServicePortTypeClient.consultarUltimoComprobanteAutorizado(authRequest,
                    consultaUltimoComprobanteAutorizadoRequestType,
                    out ConsoleAppTestAfip.MTXCA.CodigoDescripcionType[] arrayErrores,
                    out ConsoleAppTestAfip.MTXCA.CodigoDescripcionType evento);

                Console.WriteLine(ultimoComp);
				if(ultimoComp < 0){
					EjecutarProcesoConsultarCbte();
				}
				EjecutarProcesoConsultarCbte(1, "uno"); EjecutarProcesoConsultarCbte(1, "uno");
				EjecutarProcesoConsultarCbte(1, "uno");
				int f = 9;
				string s = "bbb";
				EjecutarProcesoConsultarCbte(out int gg, s);
				int g;
				EjecutarProcesoConsultarCbte(out   g, s);
				EjecutarProcesoConsultarCbte(ref f, s);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

    }
}
