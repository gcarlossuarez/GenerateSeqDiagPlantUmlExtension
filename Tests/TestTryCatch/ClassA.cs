using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestTryCatch
{
    public class ClassA
    {
        public void CallATest1()
        {
            try
            {
                ClassB classB = new ClassB();
                classB.CallBTest1();
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("FileNotFoundException manejada en A");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception manejada en A");
            }
        }


        public void CallATest3()
        {
            try
            {
                ClassB classB = new ClassB();
                classB.CallBTest3();
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("FileNotFoundException manejada en A");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception manejada en A");
            }
        }

        public void CallATest4()
        {
            try
            {
                ClassB classB = new ClassB();
                classB.CallBTest4();
            }
            catch (ArrayTypeMismatchException ex)
            {
                Console.WriteLine("ArrayTypeMismatchException manejada en A");
            }
        }

        public void CallATest5()
        {
            try
            {
                ClassB classB = new ClassB();
                classB.CallBTest5();
            }
            catch
            {
                Console.WriteLine("Exception manejada en A");
            }
        }

        public void CallATest6()
        {
            ClassB classB = new ClassB();
            classB.CallBTest6();
        }

        public void CallATest7(int i)
        {
            try
            {
                ClassB classB = new ClassB();
                classB.CallBTest7();
            }
            catch (MyException e)
            {
                Console.WriteLine("MyException:" + e.Message);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void CallATest8()
        {
            try
            {
                ClassB classB = new ClassB();
                classB.CallBTest8();
            }
            catch (MyException e)
            {
                Console.WriteLine("MyException:" + e.Message);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            Console.WriteLine("Inside ClassA.CallTest8. Outside of try-catch");
        }
    }
}
