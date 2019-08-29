using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    public static class TestHelper
    {
        /// <summary>値がtrueであることを保証します</summary>
        /// <param name="value"></param>
        public static void Assert(bool value)
        {
            if(!value) { throw new Exception(); }
        }

        /// <summary>例外を投げることを保証します</summary>
        /// <param name="action"></param>
        public static void AssertException(Action action)
        {
            try {
                action();
                throw new TestException();
            }
            catch(TestException) { throw new Exception(); }
            catch(Exception) { }
        }

        /// <summary>例外を投げることを保証します</summary>
        /// <param name="action"></param>
        public static void AssertException<T>(Action action) where T : Exception
        {
            try {
                action();
                throw new TestException();
            }
            catch(TestException) { throw new Exception(); }
            catch(T) { }
        }

        public static TResult AssertException<T, TResult>(Func<TResult> func) where T : Exception
        {
            var result = default(TResult);
            try {
                result = func();
                throw new TestException();
            }
            catch(TestException) { throw new Exception(); }
            catch(T) {
                return result;
            }
        }

        private class TestException : Exception
        {

        }
    }
}
