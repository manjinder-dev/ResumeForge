using ResumeForge.Api.Models;

namespace ResumeForge.Api.Services;

public interface IResumeService
{
    Task<TailoredResumeResult> TailorAsync(
        string resumeText,
        string jobDescription,
        AiProviderOptions aiOptions,
        CancellationToken cancellationToken);
}
