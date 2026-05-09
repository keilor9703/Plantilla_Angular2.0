import { Pipe, PipeTransform } from '@angular/core';

const ALLOWED_HOSTS = new Set([
  'www.youtube.com',
  'youtube.com',
  'youtu.be',
  'www.youtube-nocookie.com',
  'youtube-nocookie.com'
]);

@Pipe({ name: 'safeUrl', standalone: true })
export class SafeUrlPipe implements PipeTransform {
  transform(url: string | null | undefined): string {
    const raw = (url ?? '').trim();
    if (!raw) return '';

    try {
      const parsed = new URL(raw, window.location.origin);
      const isHttp = parsed.protocol === 'https:' || parsed.protocol === 'http:';
      const isAllowedHost = ALLOWED_HOSTS.has(parsed.hostname.toLowerCase());
      return isHttp && isAllowedHost ? parsed.toString() : '';
    } catch {
      return '';
    }
  }
}
