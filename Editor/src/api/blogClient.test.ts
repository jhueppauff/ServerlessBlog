import { beforeEach, describe, expect, it, vi } from 'vitest';
import { BlogClient, createSlug } from './blogClient';

const response = (body: unknown, ok = true) =>
  new Response(typeof body === 'string' ? body : JSON.stringify(body), {
    status: ok ? 200 : 500,
  });

describe('BlogClient', () => {
  const fetchMock = vi.fn();

  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
  });

  const client = new BlogClient('https://api.test', async () => 'token');

  it('sends the access token as bearer token', async () => {
    fetchMock.mockResolvedValue(response([]));

    await client.getBlogPosts();

    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe('https://api.test/api/post');
    expect((init.headers as Headers).get('Authorization')).toBe('Bearer ' + 'token');
  });

  it('maps PascalCase responses to camelCase models', async () => {
    fetchMock.mockResolvedValue(
      response([{ PartitionKey: 'my-post', Title: 'My Post', IsPublic: true }]),
    );

    const posts = await client.getBlogPosts();

    expect(posts[0].partitionKey).toBe('my-post');
    expect(posts[0].title).toBe('My Post');
    expect(posts[0].isPublic).toBe(true);
  });

  it('returns markdown as plain text', async () => {
    fetchMock.mockResolvedValue(response('# Hello'));

    await expect(client.getBlogPostMarkdown('my post')).resolves.toBe('# Hello');
    expect(fetchMock.mock.calls[0][0]).toBe('https://api.test/api/post/my%20post/markdown');
  });

  it('saves metadata and markdown', async () => {
    fetchMock.mockResolvedValue(response(''));

    await client.saveBlogPost(
      { partitionKey: 'slug', rowKey: 'slug', slug: 'slug' } as never,
      '# Body',
    );

    const calls = fetchMock.mock.calls.map(([url, init]) => [url, init.method, init.body]);
    expect(calls).toEqual(
      expect.arrayContaining([
        ['https://api.test/api/post/', 'POST', JSON.stringify({ partitionKey: 'slug', rowKey: 'slug', slug: 'slug' })],
        ['https://api.test/api/post/slug', 'PUT', '# Body'],
      ]),
    );
  });

  it('uploads a file and strips the quotes from the returned url', async () => {
    fetchMock.mockResolvedValue(response('"https://blob/image.png"'));

    const url = await client.uploadFile(new File(['x'], 'image.png'), 'png');

    expect(url).toBe('https://blob/image.png');
    expect(fetchMock.mock.calls[0][0]).toBe('https://api.test/api/image/upload/png');
    expect(fetchMock.mock.calls[0][1].method).toBe('PUT');
    expect((fetchMock.mock.calls[0][1].body as FormData).get('files')).toBeInstanceOf(File);
  });

  it('publishes a post', async () => {
    fetchMock.mockResolvedValue(response(''));

    await client.publishPost('slug', new Date('2024-05-01T00:00:00.000Z'));

    expect(fetchMock.mock.calls[0][1].body).toBe(
      JSON.stringify({ slug: 'slug', publishDate: '2024-05-01T00:00:00.000Z' }),
    );
  });

  it('throws on failed requests', async () => {
    fetchMock.mockResolvedValue(response('', false));

    await expect(client.getBlogPosts()).rejects.toThrow('failed with status 500');
  });
});

describe('createSlug', () => {
  it('replaces spaces and encodes the title', () => {
    expect(createSlug('Hello World & Co')).toBe('Hello-World-%26-Co');
  });
});
