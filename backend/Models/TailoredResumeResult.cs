using System.Text.Json.Serialization;

namespace ResumeForge.Api.Models;

public sealed record TailoredResumeResult(
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("bulletPoints")] IReadOnlyList<string> BulletPoints,
    [property: JsonPropertyName("coverLetter")] string CoverLetter,
    [property: JsonPropertyName("resume")] ProfessionalResume Resume,
    [property: JsonPropertyName("coverLetterDocument")] CoverLetterDocument CoverLetterDocument);

public sealed record ProfessionalResume(
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("headline")] string Headline,
    [property: JsonPropertyName("contact")] ResumeContact Contact,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("skills")] IReadOnlyList<string> Skills,
    [property: JsonPropertyName("experience")] IReadOnlyList<ResumeExperience> Experience,
    [property: JsonPropertyName("projects")] IReadOnlyList<ResumeProject> Projects,
    [property: JsonPropertyName("education")] IReadOnlyList<ResumeEducation> Education,
    [property: JsonPropertyName("certifications")] IReadOnlyList<string> Certifications);

public sealed record ResumeContact(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("phone")] string Phone,
    [property: JsonPropertyName("location")] string Location,
    [property: JsonPropertyName("linkedIn")] string LinkedIn,
    [property: JsonPropertyName("portfolio")] string Portfolio);

public sealed record ResumeExperience(
    [property: JsonPropertyName("jobTitle")] string JobTitle,
    [property: JsonPropertyName("company")] string Company,
    [property: JsonPropertyName("location")] string Location,
    [property: JsonPropertyName("startDate")] string StartDate,
    [property: JsonPropertyName("endDate")] string EndDate,
    [property: JsonPropertyName("bulletPoints")] IReadOnlyList<string> BulletPoints);

public sealed record ResumeProject(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("technologies")] IReadOnlyList<string> Technologies);

public sealed record ResumeEducation(
    [property: JsonPropertyName("degree")] string Degree,
    [property: JsonPropertyName("institution")] string Institution,
    [property: JsonPropertyName("location")] string Location,
    [property: JsonPropertyName("graduationDate")] string GraduationDate,
    [property: JsonPropertyName("details")] IReadOnlyList<string> Details);

public sealed record CoverLetterDocument(
    [property: JsonPropertyName("recipient")] string Recipient,
    [property: JsonPropertyName("companyName")] string CompanyName,
    [property: JsonPropertyName("jobTitle")] string JobTitle,
    [property: JsonPropertyName("content")] string Content);
