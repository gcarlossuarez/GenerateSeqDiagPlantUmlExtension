using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TestTryCatch
{
    internal class ClassB
    {

        public void CallBTest1()
        {
            ClassC classC = new ClassC();
            classC.CallCTest1(true);
        }

        public void CallBTest3()
        {
            try
            {
                ClassC classC = new ClassC();
                classC.CallCTest3(true);
            }
            catch (IndexOutOfRangeException e)
            {
                Console.WriteLine(e);
            }
        }

        public void CallBTest4()
        {
            try
            {
                ClassC classC = new ClassC();
                classC.CallCTest4(true);
            }
            catch (IndexOutOfRangeException e)
            {
                Console.WriteLine(e);
            }
        }

        public void CallBTest5()
        {
            try
            {
                ClassC classC = new ClassC();
                classC.CallCTest5(true);
            }
            catch (IndexOutOfRangeException e)
            {
                Console.WriteLine(e);
            }
        }

        public void CallBTest6()
        {
            try
            {
                ClassC classC = new ClassC();
                classC.CallCTest6(true);
            }
            catch (IndexOutOfRangeException e)
            {
                Console.WriteLine(e);
            }
        }

        public void CallBTest7()
        {
            try
            {
                ClassC classC = new ClassC();
                classC.CallCTest7(true);
            }
            catch (IndexOutOfRangeException e)
            {
                Console.WriteLine(e);
            }
        }

        public void CallBTest8()
        {
            try
            {
                ClassC classC = new ClassC();
                classC.CallCTest8(true);
            }
            catch (IndexOutOfRangeException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine("Ending ClassB.CallBTest8");
            }
        }

        public void CallBTest9()
        {
            try
            {
                ClassC classC = new ClassC();
                classC.CallCTest9(true);
            }
            catch (IndexOutOfRangeException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine("Ending ClassB.CallBTest8");
            }
        }

        public void CallBTest10()
        {
            try
            {
                ClassC classC = new ClassC();
                classC.CallCTest10(true);
            }
            catch (IndexOutOfRangeException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine("Ending ClassB.CallBTest8");
            }
        }
    }
}
