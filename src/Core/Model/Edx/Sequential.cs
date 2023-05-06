using System;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace Ulearn.Core.Model.Edx
{
	[XmlRoot("sequential")]
	public class Sequential : EdxItem
	{
		[XmlIgnore]
		public override string SubfolderName => "sequential";

		private VerticalReference[] verticalReferences;

		[XmlElement("vertical")]
		public VerticalReference[] VerticalReferences
		{
			get { return verticalReferences ??= Array.Empty<VerticalReference>(); }
			set => verticalReferences = value;
		}

		[XmlAttribute("visible_to_staff_only")]
		[DefaultValue(false)]
		public bool VisibleToStaffOnly { get; set; }

		[XmlIgnore]
		public Vertical[] Verticals;

		public Sequential()
		{
		}

		public Sequential(string urlName, string displayName, Vertical[] verticals)
		{
			UrlName = urlName;
			DisplayName = displayName;
			Verticals = verticals;
			VerticalReferences = verticals.Select(x => x.GetReference()).ToArray();
		}

		public SequentialReference GetReference()
		{
			return new SequentialReference { UrlName = UrlName };
		}

		public override void SaveAdditional(string folderName)
		{
			foreach (var vertical in Verticals)
				vertical.Save(folderName);
		}

		public static Sequential Load(string folderName, string urlName, EdxLoadOptions options)
		{
			return Load<Sequential>(folderName, "sequential", urlName, options, seq =>
			{
				seq.Verticals = seq.VerticalReferences.Select(x => Vertical.Load(folderName, x.UrlName, options)).ExceptNulls().ToArray();
				seq.VerticalReferences = seq.Verticals.Select(v => v.GetReference()).ToArray();
			});
		}
	}
}