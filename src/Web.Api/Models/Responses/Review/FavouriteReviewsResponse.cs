using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Database.Models;
using Ulearn.Web.Api.Utils;

namespace Ulearn.Web.Api.Models.Responses.Review
{
	[DataContract]
	public class FavouriteReviewsResponse
	{
		[DataMember]
		public List<FavouriteReviewResponse> FavouriteReviews { get; set; }

		[DataMember]
		public List<FavouriteReviewResponse> UserFavouriteReviews { get; set; }

		[DataMember]
		public List<string> LastUsedReviews { get; set; }

		public static FavouriteReviewsResponse Build(List<FavouriteReview> favouriteReviews, List<FavouriteReview> userFavouriteReviews, List<string> lastUsedReviews)
		{
			return new FavouriteReviewsResponse
			{
				FavouriteReviews = favouriteReviews.Select(FavouriteReviewResponse.Build).ToList(),
				UserFavouriteReviews = userFavouriteReviews.Select(FavouriteReviewResponse.Build).ToList(),
				LastUsedReviews = lastUsedReviews
			};
		}
	}

	[DataContract]
	public class FavouriteReviewResponse
	{
		[DataMember]
		public int Id { get; set; }

		[DataMember]
		public string Text { get; set; }

		[DataMember]
		public string RenderedText { get; set; }

		public static FavouriteReviewResponse Build(FavouriteReview fr)
		{
			return new FavouriteReviewResponse { Id = fr.Id, Text = fr.Text, RenderedText = CommentTextHelper.RenderCommentTextToHtml(fr.Text) };
		}

		public static FavouriteReviewResponse Build(FavouriteReviewByUser fr)
		{
			return Build(fr.FavouriteReview);
		}
	}
}