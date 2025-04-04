using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fenris.Test
{
    internal class Test : ITestInterface
    {
        public Test()
        {
            
        }
        void ITestInterface.TestMethod()
        {
            throw new NotImplementedException();
        }
        public void TestMethod()
        {
            throw new NotImplementedException();
        }

        public void TestMethod2()
        {
            Test test = new Test();
            ITestInterface iface = test;
            iface.TestMethod();
            test.TestMethod();
        }
    }


    
}
