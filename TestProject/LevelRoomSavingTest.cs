using PlusLevelFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject
{
    [TestClass]
    public class LevelRoomSavingTest
    {
        [TestMethod]
        public void TestLevelTiles()
        {
            Random rng = new Random();
            Level level = new Level(25, 25);
            level.tiles[0, 1].walls = new Nybble(15);
            level.tiles[0, 2].walls = new Nybble(15);
            level.tiles[0, 0].roomId = 1;
            level.tiles[0, 1].roomId = 1;
            level.tiles[0, 2].roomId = 1;
            for (int x = 0; x < level.width; x++)
            {
                for (int y = 0; y < level.width; y++)
                {
                    level.tiles[x, y].walls = new Nybble(rng.Next(0,16));
                    level.entitySafeTiles[x, y] = rng.Next(0, 2) == 0;
                    level.eventSafeTiles[x, y] = rng.Next(0, 2) == 0;
                }
            }
            level.rooms.Add(new RoomProperties("hall"));
            MemoryStream stream = new MemoryStream(3000);
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(level);
            stream.Seek(0, SeekOrigin.Begin); //reset to beginning of stream so binaryreader can read
            Level newLvl = new BinaryReader(stream).ReadLevel();
            for(int x = 0; x < level.width; x++)
            {
                for (int y = 0; y < level.width; y++)
                {
                    Assert.AreEqual(level.tiles[x, y].walls, newLvl.tiles[x, y].walls);
                    Assert.AreEqual(level.entitySafeTiles[x, y], newLvl.entitySafeTiles[x, y]);
                    Assert.AreEqual(level.eventSafeTiles[x, y], newLvl.eventSafeTiles[x, y]);
                }
            }
        }
    }
}
