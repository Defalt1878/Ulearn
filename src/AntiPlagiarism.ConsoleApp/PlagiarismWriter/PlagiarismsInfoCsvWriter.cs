using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AntiPlagiarism.ConsoleApp.Models;
using CsvHelper;
using Ulearn.Common.Extensions;

namespace AntiPlagiarism.ConsoleApp.PlagiarismWriter;

public class PlagiarismsInfoCsvWriter
{
	private readonly string path;

	public PlagiarismsInfoCsvWriter(string path)
	{
		this.path = path.PathCombine(Repository.AntiplagiarismDataDirectory);
	}

	public void WritePlagiarism(IEnumerable<PlagiarismInfo> plagiarisms, string fileName = "plagiarisms.csv")
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