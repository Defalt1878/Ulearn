using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ulearn.Common;

[JsonConverter(typeof(StringEnumConverter), true)]
public enum InterpretNonJsonOutputType : short
{
	[XmlEnum("SE")]
	SandboxError,

	[XmlEnum("WA")]
	WrongAnswer,

	[XmlEnum("CE")]
	CompilationError
}