using System;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using Component = Ulearn.Core.Model.Edx.EdxComponents.Component;

namespace Ulearn.Core.Model.Edx
{
	[XmlRoot("vertical")]
	public class Vertical : EdxItem
	{
		[XmlIgnore]
		public override string SubfolderName => "vertical";

		[XmlIgnore]
		public Component[] Components;

		private EdxReference[] componentReferences;

		[XmlElement("video", Type = typeof(VideoComponentReference))]
		[XmlElement("html", Type = typeof(HtmlComponentReference))]
		[XmlElement("problem", Type = typeof(ProblemComponentReference))]
		[XmlElement("lti", Type = typeof(LtiComponentReference))]
		public EdxReference[] ComponentReferences
		{
			get { return componentReferences ??= Array.Empty<EdxReference>(); }
			set => componentReferences = value;
		}

		public Vertical()
		{
		}

		[XmlAttribute("format")]
		[DefaultValue(null)]
		public string ScoringGroup;

		[XmlAttribute("graded")]
		[DefaultValue(false)]
		public bool Graded
		{
			get => !string.IsNullOrEmpty(ScoringGroup);
			// ReSharper disable once ValueParameterNotUsed
			set { }
		}

		public bool ShouldSerializeGraded() => !string.IsNullOrEmpty(ScoringGroup);

		[XmlAttribute("weight")]
		[DefaultValue("0.0")]
		public string Weight;

		public Vertical(string urlName, string displayName, Component[] components, string scoringGroup = null, double weight = 0)
		{
			UrlName = urlName;
			DisplayName = displayName;
			Components = components;
			ScoringGroup = scoringGroup;
			Weight = weight.Equals(0.0) ? "0.0" : "1.0";
			ComponentReferences = components.Select(x => x.GetReference()).ToArray();
		}

		public VerticalReference GetReference()
		{
			return new VerticalReference { UrlName = UrlName };
		}

		public override void SaveAdditional(string folderName)
		{
			foreach (var component in Components)
				component.Save(folderName);
		}

		public static Vertical Load(string folderName, string urlName, EdxLoadOptions options)
		{
			return Load<Vertical>(folderName, "vertical", urlName, options, v =>
			{
				v.Components = v.ComponentReferences.Select(x => x.LoadComponent(folderName, x.UrlName, options)).ExceptNulls().ToArray();
				v.ComponentReferences = v.Components.Select(c => c.GetReference()).ToArray();
			});
		}
	}
}