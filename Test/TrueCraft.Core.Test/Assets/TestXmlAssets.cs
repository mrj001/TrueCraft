using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using NUnit.Framework;
using TrueCraft.Core;

namespace TrueCraft.Core.Test.Assets
{
    public class TestXmlAssets
    {
        public TestXmlAssets()
        {
        }

        [Test]
        public void TestXsdCompiles()
        {
            XmlDocument doc = new XmlDocument();

            Assembly thatAssembly = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == "TrueCraft.Core").First();

            using (Stream xsd = thatAssembly.GetManifestResourceStream("TrueCraft.Core.Assets.TrueCraft.xsd")!)
                doc.Schemas.Add(XmlSchema.Read(xsd, null)!);

            doc.Schemas.Compile();
        }

        [Test]
        public void TestTrueCraftXmlValid()
        {
            XmlDocument doc = new XmlDocument();

            Assembly thatAssembly = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == "TrueCraft.Core").First();
            using (Stream xsd = thatAssembly.GetManifestResourceStream("TrueCraft.Core.Assets.TrueCraft.xsd")!)
                doc.Schemas.Add(XmlSchema.Read(xsd, null)!);

            using (Stream sz = thatAssembly.GetManifestResourceStream("TrueCraft.Core.Assets.TrueCraft.xml.gz")!)
            using (Stream s = new GZipStream(sz, CompressionMode.Decompress))
            using (XmlReader xmlr = XmlReader.Create(s))
            {
                doc.Load(xmlr);
                doc.Validate(null);
            }
        }
    }
}

