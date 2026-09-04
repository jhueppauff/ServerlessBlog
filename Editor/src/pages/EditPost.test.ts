import { describe, expect, it } from 'vitest';
import { renderMarkdown } from './EditPost';

describe('renderMarkdown', () => {
  it('renders markdown as html', () => {
    expect(renderMarkdown('# Title')).toContain('<h1>Title</h1>');
  });

  it('removes dangerous markup', () => {
    expect(renderMarkdown('<img src=x onerror="alert(1)">')).not.toContain('onerror');
  });
});
