import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import type { BlogClient } from '../api/blogClient';
import { BlogClientProvider } from '../api/BlogClientProvider';
import { NotificationProvider } from '../components/NotificationProvider';
import { EditPost } from './EditPost';

const post = {
  partitionKey: 'my-post',
  rowKey: 'my-post',
  slug: 'my-post',
  title: 'My Post',
  published: '2024-05-01',
  tags: 'azure;serverless',
  preview: 'A preview text',
  imageUrl: 'https://blob/image.png',
  pageUrl: '/my-post',
  views: 0,
  isPublic: false,
};

const renderEditor = (client: Partial<BlogClient>, route = '/edit/my-post') =>
  render(
    <MemoryRouter initialEntries={[route]}>
      <BlogClientProvider client={client as BlogClient}>
        <NotificationProvider>
          <Routes>
            <Route path="/add" element={<EditPost />} />
            <Route path="/edit/:slug" element={<EditPost />} />
          </Routes>
        </NotificationProvider>
      </BlogClientProvider>
    </MemoryRouter>,
  );

describe('EditPost page', () => {
  it('loads the metadata and the markdown of an existing post', async () => {
    renderEditor({
      getBlogPost: vi.fn().mockResolvedValue(post),
      getBlogPostMarkdown: vi.fn().mockResolvedValue('# Hello'),
    });

    expect(await screen.findByDisplayValue('My Post')).toBeInTheDocument();
    expect(screen.getByDisplayValue('# Hello')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Post details' }));

    expect(screen.getByDisplayValue('azure;serverless')).toBeInTheDocument();
    expect(screen.getByDisplayValue('A preview text')).toBeInTheDocument();
    expect(screen.getByDisplayValue('/my-post')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Search image' })).toBeInTheDocument();
  });

  it('switches between the editor and the rendered preview', async () => {
    renderEditor({
      getBlogPost: vi.fn().mockResolvedValue(post),
      getBlogPostMarkdown: vi.fn().mockResolvedValue('# Hello'),
    });

    expect(await screen.findByDisplayValue('# Hello')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Preview only' }));

    expect(screen.queryByDisplayValue('# Hello')).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Hello' })).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Editor only' }));

    expect(screen.getByDisplayValue('# Hello')).toBeInTheDocument();
  });

  it('saves the post with the edited markdown', async () => {
    const saveBlogPost = vi.fn().mockResolvedValue(undefined);

    renderEditor({
      getBlogPost: vi.fn().mockResolvedValue(post),
      getBlogPostMarkdown: vi.fn().mockResolvedValue('# Hello'),
      saveBlogPost,
    });

    const content = await screen.findByDisplayValue('# Hello');
    await userEvent.type(content, '!');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => expect(saveBlogPost).toHaveBeenCalled());
    expect(saveBlogPost.mock.calls[0][1]).toBe('# Hello!');
  });

  it('normalizes custom page urls while saving', async () => {
    const saveBlogPost = vi.fn().mockResolvedValue(undefined);

    renderEditor({
      getBlogPost: vi.fn().mockResolvedValue(post),
      getBlogPostMarkdown: vi.fn().mockResolvedValue('# Hello'),
      saveBlogPost,
    });

    await screen.findByDisplayValue('/my-post');
    await userEvent.click(screen.getByRole('button', { name: 'Post details' }));
    const pageUrl = screen.getByLabelText('Page Url');
    await userEvent.clear(pageUrl);
    await userEvent.type(pageUrl, 'custom/path');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => expect(saveBlogPost).toHaveBeenCalled());
    expect(saveBlogPost.mock.calls[0][0].pageUrl).toBe('/custom/path');
  });

  it('reports missing required fields instead of saving', async () => {
    const saveBlogPost = vi.fn();

    renderEditor({ saveBlogPost }, '/add');

    await userEvent.click(await screen.findByRole('button', { name: 'Save' }));

    expect(await screen.findByText('Not all required fields have been filled out.')).toBeInTheDocument();
    expect(saveBlogPost).not.toHaveBeenCalled();
  });
});
