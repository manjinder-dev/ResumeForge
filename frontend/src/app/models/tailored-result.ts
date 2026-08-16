export interface ResumeContact {
  email: string;
  phone: string;
  location: string;
  linkedIn: string;
  portfolio: string;
}

export interface ResumeExperience {
  jobTitle: string;
  company: string;
  location: string;
  startDate: string;
  endDate: string;
  bulletPoints: string[];
}

export interface ResumeProject {
  name: string;
  description: string;
  technologies: string[];
}

export interface ResumeEducation {
  degree: string;
  institution: string;
  location: string;
  graduationDate: string;
  details: string[];
}

export interface ProfessionalResume {
  fullName: string;
  headline: string;
  contact: ResumeContact;
  summary: string;
  skills: string[];
  experience: ResumeExperience[];
  projects: ResumeProject[];
  education: ResumeEducation[];
  certifications: string[];
}

export interface CoverLetterDocument {
  recipient: string;
  companyName: string;
  jobTitle: string;
  content: string;
}

export interface TailoredResult {
  summary: string;
  bulletPoints: string[];
  coverLetter: string;
  resume: ProfessionalResume;
  coverLetterDocument: CoverLetterDocument;
}

export interface ApiErrorResponse {
  code: string;
  message: string;
}
