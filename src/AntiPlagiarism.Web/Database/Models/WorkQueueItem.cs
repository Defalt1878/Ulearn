using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AntiPlagiarism.Web.Database.Models
{
	public class WorkQueueItem
	{
		public const string IdColumnName = "Id";
		public const string QueueIdColumnName = "QueueId";
		public const string ItemIdColumnName = "ItemId";
		public const string TakeAfterTimeColumnName = "TakeAfterTime";

		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		public QueueIds QueueId { get; set; } // В одной таблице может лежать несколько очередей

		[Required]
		public string ItemId { get; set; } // Id элемента другой таблицы, которому соответствует запись в очереди

		public DateTime? TakeAfterTime { get; set; } // Устанавливается при взятии элемента из очереди. После этого времени разрешено повторно взять элемент из очереди
	}

	public enum QueueIds
	{
		None,
		NewSubmissionsQueue
	}
}