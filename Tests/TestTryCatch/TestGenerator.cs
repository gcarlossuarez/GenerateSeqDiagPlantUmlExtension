using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestTryCatch;

namespace Test
{
    public class TestGenerator
    {
        public void InitTest2()
        {
            Console.WriteLine("Initializing...");
            ClassA classA = new ClassA();
            classA.CallATest1();
        }

        public void InitTest3()
        {
            Console.WriteLine("Initializing...");
            ClassA classA = new ClassA();
            classA.CallATest3();
        }

        public void InitTest4()
        {
            Console.WriteLine("Initializing...");
            ClassA classA = new ClassA();
            classA.CallATest4();
        }

        public void InitTest5()
        {
            Console.WriteLine("Initializing...");
            ClassA classA = new ClassA();
            classA.CallATest5();
        }

        public void InitTest6()
        {
            Console.WriteLine("Initializing...");
            ClassA classA = new ClassA();
            classA.CallATest6();
        }

        public void InitTest7()
        {
            try
            {
                Console.WriteLine("Initializing...");
                ClassA classA = new ClassA();
                classA.CallATest7(10);
            }
            catch (MyException e)
            {
                Console.WriteLine("MyException:" + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void InitTest8()
        {
            try
            {
                Console.WriteLine("Initializing...");
                ClassA classA = new ClassA();
                classA.CallATest8();
            }
            catch (MyException e)
            {
                Console.WriteLine("MyException:" + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine("Ending TestGenerator.InitTest8");
            }
        }
    }
}

