import { Injectable } from '@angular/core';
import {
  Document,
  HeadingLevel,
  Packer,
  Paragraph,
  TextRun
} from 'docx';
import { TailoredResult } from '../models/tailored-result';

@Injectable({ providedIn: 'root' })
export class DownloadService {
  downloadText(result: TailoredResult): void {
    const content = [
      'PROFESSIONAL SUMMARY',
      result.summary,
      '',
      'REWRITTEN BULLET POINTS',
      ...result.bulletPoints.map((point) => `• ${point}`),
      '',
      'COVER LETTER',
      result.coverLetter
    ].join('\n');

    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
    this.downloadBlob(blob, 'ResumeForge-Tailored-Result.txt');
  }

  async downloadDocx(result: TailoredResult): Promise<void> {
    const document = new Document({
      sections: [
        {
          properties: {},
          children: [
            new Paragraph({
              text: 'ResumeForge Tailored Result',
              heading: HeadingLevel.TITLE
            }),
            new Paragraph({
              text: 'Professional Summary',
              heading: HeadingLevel.HEADING_1
            }),
            new Paragraph({ text: result.summary }),
            new Paragraph({
              text: 'Rewritten Bullet Points',
              heading: HeadingLevel.HEADING_1
            }),
            ...result.bulletPoints.map(
              (point) =>
                new Paragraph({
                  children: [new TextRun(point)],
                  bullet: { level: 0 }
                })
            ),
            new Paragraph({
              text: 'Cover Letter',
              heading: HeadingLevel.HEADING_1
            }),
            ...result.coverLetter
              .split(/\r?\n/)
              .filter((line) => line.trim().length > 0)
              .map((line) => new Paragraph({ text: line.trim() }))
          ]
        }
      ]
    });

    const blob = await Packer.toBlob(document);
    this.downloadBlob(blob, 'ResumeForge-Tailored-Result.docx');
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  }
}
