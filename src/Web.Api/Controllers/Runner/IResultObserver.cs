using System.Threading.Tasks;
using Database.Models;
using Ulearn.Core.RunCheckerJobApi;

namespace Ulearn.Web.Api.Controllers.Runner;

public interface IResultObserver
{
	Task ProcessResult(UserExerciseSubmission submission, RunningResults result);
}