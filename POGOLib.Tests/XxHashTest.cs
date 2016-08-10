using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using POGOLib.Util.Encryption;

namespace POGOLib.Tests
{
    [TestClass]
    public class XxHashTest
    {
        [TestMethod]
        public void TestXxHash32()
        {
            const uint expectedLocationHash2 = 1992650851;
            const string locationHex = "4049C0F0E0A84BE4BFC05A5FC7E6B3FFFFF8000000000000";

            var location = StringToByteArray(locationHex);
            var calculatedLocationHash2 = xxHash32.CalculateHash(location, location.Length, 0x1B845238);
            
            Assert.AreEqual(expectedLocationHash2, calculatedLocationHash2);
        }

        [TestMethod]
        public void TestXxHash64()
        {
            const ulong expectedSeed = 14154164285468792240;
            const string serializedTicketHex = "0A0370746312530A4F5447542D31303235393130382D743237717856627A77586566744372586555394242326563545A4D414577673475664A55786879594B35794C6972776755792D73736F2E706F6B656D6F6E2E636F6D103B";

            var serializedTicket = StringToByteArray(serializedTicketHex);
            var calculatedSeed = xxHash64.CalculateHash(serializedTicket, serializedTicket.Length, 0x1B845238);

            Assert.AreEqual(expectedSeed, calculatedSeed);
        }

        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
