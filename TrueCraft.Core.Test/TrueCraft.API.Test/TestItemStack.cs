using System;
using System.IO;
using System.Xml;
using NUnit.Framework;


namespace TrueCraft.API.Test
{
    [TestFixture]
    public class TestItemStack
    {
        // Test with default metadata
        [TestCase(17, 1, 0, 
            @"
          <c>
            <id> 17 </id>
            <count> 1 </count>
          </c>")]
        // Test with explicit metadata
        [TestCase(351, 1, 4, 
            @"<c>  
            <id>351</id>
            <count>1</count>
            <metadata>4</metadata>
          </c>")]
        public void Test_Ctor_xml(short expectedID, sbyte expectedCount, short expectedMetadata, string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            ItemStack actual = new ItemStack(doc.FirstChild);

            Assert.AreEqual(expectedID, actual.ID);
            Assert.AreEqual(expectedCount, actual.Count);
            Assert.AreEqual(expectedMetadata, actual.Metadata);
        }

        [TestCase(5)]
        public void Test_Ctor_id(short id)
        {
            ItemStack actual = new ItemStack(id);

            Assert.AreEqual(id, actual.ID);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(0, actual.Metadata);
        }

        [TestCase(3, 5)]
        public void Test_Ctor_id_count(short id, sbyte count)
        {
            ItemStack actual = new ItemStack(id, count);

            Assert.AreEqual(id, actual.ID);
            Assert.AreEqual(count, actual.Count);
            Assert.AreEqual(0, actual.Metadata);
        }

        [TestCase(3, 5, 7)]
        public void Test_Ctor_id_count_metadata(short id, sbyte count, short metadata)
        {
            ItemStack actual = new ItemStack(id, count, metadata);

            Assert.AreEqual(id, actual.ID);
            Assert.AreEqual(count, actual.Count);
            Assert.AreEqual(metadata, actual.Metadata);
        }
    }
}