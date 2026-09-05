import type { Blob, PageMetric, PostMetadata } from './models';

export type TokenProvider = () => Promise<string | undefined>;

/**
 * Azure Functions serializes responses with camelCase while the tables may
 * return PascalCase properties, so all keys are lowered before mapping.
 */
const camelCase = (value: unknown): unknown => {
  if (Array.isArray(value)) {
    return value.map(camelCase);
  }

  if (value !== null && typeof value === 'object') {
    return Object.fromEntries(
      Object.entries(value as Record<string, unknown>).map(([key, entry]) => [
        key.charAt(0).toLowerCase() + key.slice(1),
        camelCase(entry),
      ]),
    );
  }

  return value;
};

export const createSlug = (title: string): string => encodeURIComponent(title.replace(/ /g, '-'));

export class BlogClient {
  private readonly baseUrl: string;
  private readonly getToken: TokenProvider;

  constructor(baseUrl: string, getToken: TokenProvider) {
    this.baseUrl = baseUrl;
    this.getToken = getToken;
  }

  private async request(path: string, init: RequestInit = {}): Promise<Response> {
    const token = await this.getToken();
    const headers = new Headers(init.headers);

    if (token) {
      headers.set('Authorization', 'Bearer ' + token);
    }

    const response = await fetch(`${this.baseUrl}${path}`, { ...init, headers });

    if (!response.ok) {
      throw new Error(`Request to ${path} failed with status ${response.status}`);
    }

    return response;
  }

  private async getJson<T>(path: string): Promise<T> {
    const response = await this.request(path);
    return camelCase(await response.json()) as T;
  }

  getBlogPosts(): Promise<PostMetadata[]> {
    return this.getJson<PostMetadata[]>('/api/post');
  }

  getBlogPost(slug: string): Promise<PostMetadata> {
    return this.getJson<PostMetadata>(`/api/post/${encodeURIComponent(slug)}`);
  }

  async getBlogPostMarkdown(slug: string): Promise<string> {
    const response = await this.request(`/api/post/${encodeURIComponent(slug)}/markdown`);
    return response.text();
  }

  getPageView(slug: string): Promise<PageMetric> {
    return this.getJson<PageMetric>(`/api/metric/${encodeURIComponent(slug)}`);
  }

  getPageViewHistory(slug: string): Promise<PageMetric[]> {
    return this.getJson<PageMetric[]>(`/api/metric/${encodeURIComponent(slug)}/history`);
  }

  getPageViews(): Promise<PageMetric[]> {
    return this.getJson<PageMetric[]>('/api/metric');
  }

  async saveBlogPost(post: PostMetadata, markdown: string): Promise<void> {
    await Promise.all([
      this.request('/api/post/', {
        method: 'POST',
        body: JSON.stringify(post),
      }),
      this.request(`/api/post/${encodeURIComponent(post.slug)}`, {
        method: 'PUT',
        body: markdown,
      }),
    ]);
  }

  async deleteBlogPost(post: PostMetadata): Promise<void> {
    await this.request(`/api/post/${encodeURIComponent(post.slug)}`, { method: 'DELETE' });
  }

  getBlobs(): Promise<Blob[]> {
    return this.getJson<Blob[]>('/api/image');
  }

  async deleteBlob(blob: Blob): Promise<void> {
    await this.request(`/api/image/${encodeURIComponent(blob.name)}`, { method: 'DELETE' });
  }

  async uploadFile(file: File, extension: string): Promise<string> {
    const content = new FormData();
    content.append('files', file, file.name);

    const response = await this.request(`/api/image/upload/${encodeURIComponent(extension)}`, {
      method: 'PUT',
      body: content,
    });

    return (await response.text()).replace(/^"|"$/g, '');
  }

  async publishPost(slug: string, publishDate: Date): Promise<void> {
    await this.request('/api/publish', {
      method: 'POST',
      body: JSON.stringify({ slug, publishDate: publishDate.toISOString() }),
    });
  }
}
