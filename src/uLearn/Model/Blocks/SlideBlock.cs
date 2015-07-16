﻿using System.Collections.Generic;
using System.Collections.Immutable;
using uLearn.Model.EdxComponents;

namespace uLearn.Model.Blocks
{
	public abstract class SlideBlock
	{
		public virtual void Validate()
		{
		}

		public virtual IEnumerable<SlideBlock> BuildUp(BuildUpContext context, IImmutableSet<string> filesInProgress)
		{
			yield return this;
		}

		public abstract IEnumerable<Component> ToEdxComponent(string folderName, string courseId, string displayName, Slide slide, int componentIndex);
	}
}