using System;
using System.Collections.Generic;

namespace Ulearn.Core.Courses
{
	public static class CourseStaticFilesHelper
	{
		public static readonly Dictionary<string, string> AllowedExtensions
			= new(StringComparer.OrdinalIgnoreCase)
			{
				{ ".png", "image/png" },
				{ ".jpg", "image/jpeg" },
				{ ".bmp", "image/bmp" },
				{ ".gif", "image/gif" },
				{ ".zip", "application/x-zip-compressed" },
				{ ".odp", "application/vnd.oasis.opendocument.presentation" },
				{ ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
				{ ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
				{ ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
				{ ".pdf", "application/pdf" },
				{ ".html", "text/html" },
				{ ".mmap", "application/vnd.mindjet.mindmanage" },
				{ ".xmind", "application/vnd.xmind.workbook" },
				{ ".fig", "application/octet-stream" }
			};
	}
}