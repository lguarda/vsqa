using System;

namespace TestHarnessMod.Core
{
    public class TestFailedException : Exception
    {
        public TestFailedException(string message) : base(message) { }
    }
}
