﻿using System;
using NUnit.Framework;
using Ulearn.Core.Configuration;
using Ulearn.Core.GoogleSheet;

namespace Ulearn.Core.Tests
{
	[TestFixture]
	public class GoogleSheetTests
	{
		private const string spreadsheetId = "1dlMITgyLknv-cRd0SleTTGetiNsSfdXAprjKGHU-FNI";
		private readonly string credentialsJson = ApplicationConfiguration.Read<UlearnConfiguration>().GoogleAccessCredentials;

		[Test, Explicit]
		public void FillingTest()
		{
			var client = new GoogleApiClient(credentialsJson);
			var sheet = new GoogleSheet.GoogleSheetModel( 0);
			var date = DateTime.UtcNow;
			sheet.AddCell(0, date);
			sheet.AddCell(0, 1);
			sheet.GoToNewLine();
			sheet.AddCell(1, "2");
			sheet.AddCell(1,  date);
			client.FillSpreadSheet(spreadsheetId, sheet);
		}
	}
}