using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace TestInvocation1
{
    class Program
    {
        static void Main(string[] args)
        {
            string xmlStr = @"<Comprobantes>
    <Comprobante>
        <CuitEmisor>1111</CuitEmisor>
    </Comprobante>
</Comprobantes>";
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlStr);
            Process(xmlDocument);
            ProcessMyClass();
            Console.WriteLine("Pulse una tecla, para continuar...");
            Console.ReadKey();
        }
        public static void Process(XmlDocument xmlDocument)
        {
			var z1 = xmlDocument.GetElementsByTagName("Comprobantes")?.Item(0);
			
			var y = xmlDocument?.GetElementsByTagName("Comprobantes")?.Item(0);
            var z2 = xmlDocument?.GetElementsByTagName("Comprobantes").Item(0);
			var aa = xmlDocument.GetElementsByTagName("Comprobantes");
            var x1 = xmlDocument.GetElementsByTagName("Comprobantes").Item(0).GetType();
            var x2 = xmlDocument.GetElementsByTagName("Comprobantes").Item(0);
            var bb = xmlDocument.GetElementsByTagName("Comprobantes");

            //Console.WriteLine($"x1:{x1}, x2:{x2.InnerText}, y:{y.InnerText}, z1:{z1.InnerText}, z2:{z2.InnerText}");
        }
		
		public static void ProcessMyClass()
		{
			var X = new MyClass();

			X.A().B()?.C(3)?.D()?.E("Hello")?.H();
			
            X.A().B().C().D().E().H();

            X.A().B().C()?.D().E()?.H();

            X.A().B().C(3).D().E().H();
			
            X.A().B()?.C(3)?.D().E("Hello")?.H();

            X.A().B()?.C(200)?.D().E().H();

			if (X != null)
			{
				X.F()?.G();
			}
			X.A();
			
			if (X != null)
			{
				X.F()?.G();
			}
			X.A();
			
			AA();
		}
		
		public static string AA()
		{
			return "Called AA";
		}
		
		class MyClass
        {
            public MyClass A() => this;
            public MyClass B() => this;
            public MyClass C() => this;
			public MyClass C(int valueInt)
			{
				if(valueInt > 19)
				{
					valueInt++;
                    Console.WriteLine($"valueInt:{valueInt}");
				}
				return new MyClass();
			}
            public MyClass D() => this;
            public MyClass E() => this;

            public MyClass E(string str)
            {
				Console.WriteLine($"i:{i}, str:{str}");
				
                for (int i = 0; i < 10; ++i)
                {
                    int j = 1;
                    while (j <= 3)
                    {
                        Console.WriteLine($"i:{i}, str:{str}");
                        ++j;
                    }
                }
                
                //return this;
                return new MyClass();
            }
            public MyClass F() => this;
            public MyClass G() => this;
            public MyClass H() => this;
        }
    }
}
