using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PersistentCollection;

namespace Tests
{
    [TestClass]
    public class ExtensionsTests
    {
        [TestMethod]
        public void BitCount()
        {
            Assert.AreEqual(50u.BitCount(), 3);
            Assert.AreEqual(40u.BitCount(), 2);
            Assert.AreEqual(89999771u.BitCount(), 15);
            Assert.AreEqual(8481548u.BitCount(), 9);
            Assert.AreEqual(891981u.BitCount(), 11);
            Assert.AreEqual(84u.BitCount(), 3);
            Assert.AreEqual(0u.BitCount(), 0);
        }
    }
}
