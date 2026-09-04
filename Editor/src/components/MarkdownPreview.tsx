import { Box } from '@mui/material';

/**
 * Styles for markdown that is rendered into the preview pane. Keeps wide
 * content (images, code blocks, tables) inside the pane instead of stretching
 * the page.
 */
export const MarkdownPreview = ({ html }: { html: string }) => (
  <Box
    dangerouslySetInnerHTML={{ __html: html }}
    sx={{
      overflowWrap: 'anywhere',
      '& img': { maxWidth: '100%', height: 'auto' },
      '& pre': { overflowX: 'auto', p: 1, borderRadius: 1, bgcolor: 'action.hover' },
      '& code': { overflowWrap: 'anywhere' },
      '& table': { display: 'block', overflowX: 'auto', borderCollapse: 'collapse' },
      '& blockquote': {
        m: 0,
        pl: 2,
        borderLeft: 3,
        borderColor: 'divider',
        color: 'text.secondary',
      },
      '& > :first-of-type': { mt: 0 },
    }}
  />
);
