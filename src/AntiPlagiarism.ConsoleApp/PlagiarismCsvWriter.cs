﻿using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AntiPlagiarism.ConsoleApp.Models;
using CsvHelper;
using Ulearn.Common.Extensions;

namespace AntiPlagiarism.ConsoleApp
{
	public class PlagiarismCsvWriter
	{
		private readonly string path;
		
		public PlagiarismCsvWriter(string path)
		{
			this.path = path;
		}

		public void WritePlagiarism(List<PlagiarismInfo> plagiarisms, string fileName="plagiarisms.csv")
		{
			var file = path.PathCombine(fileName);
			try
			{
				using var stream = new StreamWriter(file);
				using var csv = new CsvWriter(stream, CultureInfo.InvariantCulture);
				csv.WriteRecords(plagiarisms);

				ConsoleWorker.WriteLine($"Информация о плагиате записана в файл {file}");
			}
			catch (IOException e)
			{
				ConsoleWorker.WriteLine("Не удалось записать результат");
				ConsoleWorker.WriteLine($"Если у вас открыт файл {file} - закройте его и попробуйте ещё раз");
				ConsoleWorker.WriteError(e, false);
			}
		}
	}
}