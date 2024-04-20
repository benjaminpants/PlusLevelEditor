using PlusLevelFormat;

namespace TestProject
{
    [TestClass]
    public class NybbleReadWriteTest
    {
        [TestMethod]
        public void TestNybbleLoading()
        {
            Nybble[] nybbles = new Nybble[]
            {
                new Nybble(0),
                new Nybble(1),
                new Nybble(2),
                new Nybble(3),
                new Nybble(4),
                new Nybble(5),
                new Nybble(6),
                new Nybble(7),
                new Nybble(8),
                new Nybble(9),
                new Nybble(10),
                new Nybble(15),
                new Nybble(15),
                new Nybble(15),
                new Nybble(15),
                new Nybble(1),
                new Nybble(3),
                new Nybble(6),
                new Nybble(8),
                new Nybble(7),
                new Nybble(7),
            };
            MemoryStream stream = new MemoryStream(100);
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(nybbles);
            stream.Seek(0, SeekOrigin.Begin); //reset to beginning of stream so binaryreader can read
            Nybble[] newNybs = new BinaryReader(stream).ReadNybbles();
            for (int i = 0; i < nybbles.Length; i++)
            {
                Console.WriteLine("Comparing: " + i);
                Console.WriteLine(nybbles[i] + " vs " + newNybs[i]);
                Assert.AreEqual(nybbles[i], newNybs[i]);
            }
        }
    }
}