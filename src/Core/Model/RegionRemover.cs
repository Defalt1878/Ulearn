﻿using System.Collections.Generic;
using System.Linq;
using uLearn.Courses.Slides.Blocks;
using uLearn.CSharp;
using Ulearn.Common.Extensions;

namespace uLearn.Model
{
	public interface IRegionRemover
	{
		string Remove(string code, IEnumerable<Label> labels, out IEnumerable<Label> notRemoved);
		string RemoveSolution(string code, Label label, out int index);
		string Prepare(string code);
	}

	public class RegionRemover : IRegionRemover
	{
		private readonly List<IRegionRemover> regionRemovers = new List<IRegionRemover>();
		private readonly string pragma;

		public RegionRemover(string langId)
		{
			if (langId == "cs")
			{
				regionRemovers.Add(new CsMembersRemover());
				pragma = CsMembersRemover.Pragma;
			}
			regionRemovers.Add(new CommonRegionRemover());
			if (pragma == null)
				pragma = "";
		}

		public string Remove(string code, IEnumerable<Label> labels, out IEnumerable<Label> notRemoved)
		{
			foreach (var regionRemover in regionRemovers)
			{
				code = regionRemover.Remove(code, labels, out notRemoved);
				labels = notRemoved;
			}
			notRemoved = labels.ToList();
			return code.FixExtraEolns();
		}

		public string RemoveSolution(string code, Label label, out int index)
		{
			foreach (var regionRemover in regionRemovers)
			{
				code = regionRemover.RemoveSolution(code, label, out index);
				if (index < 0)
					continue;
				code = code.Insert(index, pragma);
				index += pragma.Length;
				return code;
			}
			index = -1;
			return code;
		}

		public string Prepare(string code)
		{
			return regionRemovers.Aggregate(code, (current, regionRemover) => regionRemover.Prepare(current));
		}
	}
}