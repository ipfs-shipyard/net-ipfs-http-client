using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Ipfs.Http
{
    /// <summary>
    ///   Asserting an <see cref="Exception"/>.
    /// </summary>
    public static class ExceptionAssert
    {
        public static T Throws<T>(Action action, string? expectedMessage = null) where T : Exception
        {
            try
            {
                action();
            }
            catch (AggregateException e)
            {
                var match = e.InnerExceptions.OfType<T>().FirstOrDefault();
                if (match is not null)
                {
                    if (expectedMessage is not null)
                        Assert.AreEqual(expectedMessage, match.Message, "Wrong exception message.");
                    return match;
                }

                throw;
            }
            catch (T e)
            {
                if (expectedMessage is not null)
                    Assert.AreEqual(expectedMessage, e.Message);
                return e;
            }
            Assert.Fail("Exception of type {0} should be thrown.", typeof(T));

            //  The compiler doesn't know that Assert.Fail will always throw an exception
            throw new Exception();
        }

        public static Exception Throws(Action action, string? expectedMessage = null)
        {
            return Throws<Exception>(action, expectedMessage);
        }
    }
}
