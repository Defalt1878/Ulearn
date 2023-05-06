using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Ulearn.Core.Courses.Slides.Blocks;

[XmlType("selfCheckups", Namespace = "https://ulearn.me/schema/v2")]
public class SelfCheckupsBlock : SlideBlock
{
	[XmlElement("checkup")]
	public List<string> Checkups { get; set; }

	public SelfCheckupsBlock()
	{
	}

	public SelfCheckupsBlock(IEnumerable<string> checkups)
	{
		Checkups = checkups.ToList();
	}
}