﻿using System.Threading.Tasks;
using AntiPlagiarism.Api.Models.Parameters;
using AntiPlagiarism.Api.Models.Results;

namespace AntiPlagiarism.Api;

public interface IAntiPlagiarismClient
{
	Task<AddSubmissionResponse> AddSubmissionAsync(AddSubmissionParameters parameters);
	Task<GetSubmissionPlagiarismsResponse> GetSubmissionPlagiarismsAsync(GetSubmissionPlagiarismsParameters parameters);
	Task<GetAuthorPlagiarismsResponse> GetAuthorPlagiarismsAsync(GetAuthorPlagiarismsParameters parameters);
	Task<GetMostSimilarSubmissionsResponse> GetMostSimilarSubmissionsAsync(GetMostSimilarSubmissionsParameters parameters);
	Task<GetSuspicionLevelsResponse> GetSuspicionLevelsAsync(GetSuspicionLevelsParameters parameters);
	Task<GetSuspicionLevelsResponse> SetSuspicionLevelsAsync(SetSuspicionLevelsParameters parameters);
	Task<GetProcessingStatusResponse> GetProcessingStatusAsync(GetProcessingStatusParameters parameters);
}