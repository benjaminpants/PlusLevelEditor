using PlusLevelFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject
{
    [TestClass]
    public class BoolArrayTest
    {
        [TestMethod]
        public void TestSaveLoad()
        {
            bool[] bools = { true, false, false, true, true, false, false, false, true, true, false, true, true, true, false, false, true, true, true, false, true };
            MemoryStream stream = new MemoryStream(100);
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(bools);
            stream.Seek(0, SeekOrigin.Begin); //reset to beginning of stream so binaryreader can read
            bool[] newBools = new BinaryReader(stream).ReadBoolArray();
            for (int i = 0; i < newBools.Length; i++)
            {
                Console.WriteLine("Comparing: " + i);
                Console.WriteLine(bools[i] + " vs " + newBools[i]);
                Assert.AreEqual(bools[i], newBools[i]);
            }
            Assert.AreEqual(bools.Length, newBools.Length);
        }
    }
}
