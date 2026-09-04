import { MemoryRouter } from 'react-router-dom';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import type { BlogClient } from '../api/blogClient';
import { BlogClientProvider } from '../api/BlogClientProvider';
import { NotificationProvider } from '../components/NotificationProvider';
import { Posts } from './Posts';

const post = {
  partitionKey: 'my-post',
  rowKey: 'my-post',
  slug: 'my-post',
  title: 'My Post',
  published: '2024-05-01',
  tags: 'azure;serverless',
  preview: 'A preview text',
  imageUrl: 'https://blob/image.png',
  views: 0,
  isPublic: false,
};

const renderPosts = (client: Partial<BlogClient>) =>
  render(
    <MemoryRouter>
      <BlogClientProvider client={client as BlogClient}>
        <NotificationProvider>
          <Posts />
        </NotificationProvider>
      </BlogClientProvider>
    </MemoryRouter>,
  );

describe('Posts page', () => {
  it('shows the posts including the page views', async () => {
    renderPosts({
      getBlogPosts: vi.fn().mockResolvedValue([post]),
      getPageViews: vi
        .fn()
        .mockResolvedValue([{ slug: 'my-post', views: 7, timestamp: '2024-05-01' }]),
    });

    expect(await screen.findByText('My Post')).toBeInTheDocument();
    expect(screen.getByText('7')).toBeInTheDocument();
    expect(screen.getByText('azure')).toBeInTheDocument();
    expect(screen.getByText('serverless')).toBeInTheDocument();
  });

  it('deletes a post after the confirmation dialog is accepted', async () => {
    const deleteBlogPost = vi.fn().mockResolvedValue(undefined);

    renderPosts({
      getBlogPosts: vi.fn().mockResolvedValue([post]),
      getPageViews: vi.fn().mockResolvedValue([]),
      deleteBlogPost,
    });

    await screen.findByText('My Post');
    await userEvent.click(screen.getByRole('button', { name: 'Delete' }));
    await userEvent.click(screen.getByRole('button', { name: 'Delete Post' }));

    await waitFor(() => expect(deleteBlogPost).toHaveBeenCalledWith(post));
  });

  it('publishes an unpublished post with the selected date', async () => {
    const publishPost = vi.fn().mockResolvedValue(undefined);

    renderPosts({
      getBlogPosts: vi.fn().mockResolvedValue([post]),
      getPageViews: vi.fn().mockResolvedValue([]),
      publishPost,
    });

    await screen.findByText('My Post');
    await userEvent.click(screen.getByRole('button', { name: 'Publish' }));
    await userEvent.click(screen.getByRole('button', { name: 'Publish' }));

    await waitFor(() => expect(publishPost).toHaveBeenCalled());
    expect(publishPost.mock.calls[0][0]).toBe('my-post');
  });
});
