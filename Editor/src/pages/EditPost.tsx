import { useEffect, useMemo, useState } from 'react';
import { Link as RouterLink, useParams } from 'react-router-dom';
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Button,
  IconButton,
  InputAdornment,
  Paper,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import EditNoteIcon from '@mui/icons-material/EditNote';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import SaveIcon from '@mui/icons-material/Save';
import SearchIcon from '@mui/icons-material/Search';
import VerticalSplitIcon from '@mui/icons-material/VerticalSplit';
import VisibilityIcon from '@mui/icons-material/Visibility';
import DOMPurify from 'dompurify';
import { marked } from 'marked';
import { createSlug } from '../api/blogClient';
import { useBlogClient } from '../api/BlogClientProvider';
import { emptyPost, type PostMetadata } from '../api/models';
import { ImageSelectDialog } from '../components/ImageSelectDialog';
import { Loading } from '../components/Loading';
import { MarkdownPreview } from '../components/MarkdownPreview';
import { useNotification } from '../components/NotificationProvider';

export const renderMarkdown = (markdown: string): string =>
  DOMPurify.sanitize(marked.parse(markdown, { async: false }));

type ViewMode = 'write' | 'split' | 'preview';

/** Height of the writing area: the viewport minus the app bar, toolbar and page padding. */
const workspaceHeight = 'calc(100vh - 240px)';

export const EditPost = () => {
  const { slug } = useParams<{ slug: string }>();
  const client = useBlogClient();
  const notify = useNotification();
  const [post, setPost] = useState<PostMetadata>();
  const [body, setBody] = useState('');
  const [imageDialogOpen, setImageDialogOpen] = useState(false);
  const theme = useTheme();
  const canSplit = useMediaQuery(theme.breakpoints.up('md'));
  const [viewMode, setViewMode] = useState<ViewMode>('split');

  // A side by side view does not fit on small screens, fall back to the editor.
  const effectiveMode: ViewMode = !canSplit && viewMode === 'split' ? 'write' : viewMode;
  const showEditor = effectiveMode !== 'preview';
  const showPreview = effectiveMode !== 'write';

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      if (!slug) {
        setPost(emptyPost());
        setBody('');
        return;
      }

      try {
        const [markdown, metadata] = await Promise.all([
          client.getBlogPostMarkdown(slug),
          client.getBlogPost(slug),
        ]);

        if (!cancelled) {
          setBody(markdown);
          setPost(metadata);
        }
      } catch (error) {
        notify(`An error occured: ${(error as Error).message}`, 'error');
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [client, notify, slug]);

  const preview = useMemo(() => renderMarkdown(body), [body]);

  if (!post) {
    return <Loading />;
  }

  const update = (changes: Partial<PostMetadata>) => setPost({ ...post, ...changes });

  const save = async () => {
    if (!post.title || !post.preview || !post.imageUrl || !body) {
      notify('Not all required fields have been filled out.', 'error');
      return;
    }

    const slugToSave = slug ?? createSlug(post.title);
    const pageUrl = post.pageUrl?.trim();
    const postToSave: PostMetadata = {
      ...post,
      slug: slugToSave,
      partitionKey: slugToSave,
      rowKey: slugToSave,
      pageUrl: pageUrl ? (pageUrl.startsWith('/') ? pageUrl : `/${pageUrl}`) : '',
    };

    try {
      await client.saveBlogPost(postToSave, body);
      setPost(postToSave);
      notify('Saved Blog Post sucessfully', 'success');
    } catch (error) {
      notify(`An error occured: ${(error as Error).message}`, 'error');
    }
  };

  return (
    <>
      <Paper sx={{ p: 1.5, mb: 2, position: 'sticky', top: 0, zIndex: 2 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
          <IconButton component={RouterLink} to="/" aria-label="Back to posts">
            <ArrowBackIcon />
          </IconButton>
          <Typography variant="h6" noWrap sx={{ flexGrow: 1, minWidth: 0 }}>
            {slug ?? 'New Post'}
          </Typography>
          <ToggleButtonGroup
            size="small"
            exclusive
            value={effectiveMode}
            onChange={(_event, value: ViewMode | null) => value && setViewMode(value)}
            aria-label="View mode"
          >
            <ToggleButton value="write" aria-label="Editor only">
              <EditNoteIcon fontSize="small" sx={{ mr: 0.5 }} /> Write
            </ToggleButton>
            {canSplit && (
              <ToggleButton value="split" aria-label="Editor and preview">
                <VerticalSplitIcon fontSize="small" sx={{ mr: 0.5 }} /> Split
              </ToggleButton>
            )}
            <ToggleButton value="preview" aria-label="Preview only">
              <VisibilityIcon fontSize="small" sx={{ mr: 0.5 }} /> Preview
            </ToggleButton>
          </ToggleButtonGroup>
          <Button
            variant="contained"
            color="primary"
            startIcon={<SaveIcon />}
            onClick={() => void save()}
          >
            Save
          </Button>
        </Stack>
      </Paper>

      <TextField
        label="Title"
        required
        fullWidth
        value={post.title ?? ''}
        onChange={(event) => update({ title: event.target.value })}
        sx={{ mb: 2 }}
      />

      <Stack
        direction="row"
        spacing={2}
        sx={{ alignItems: 'stretch', height: workspaceHeight, minHeight: 360 }}
      >
        {showEditor && (
          <TextField
            label="Content"
            required
            multiline
            value={body}
            onChange={(event) => setBody(event.target.value)}
            sx={{
              flex: 1,
              minWidth: 0,
              '& .MuiInputBase-root': {
                height: '100%',
                alignItems: 'flex-start',
                overflow: 'hidden',
              },
              '& textarea': {
                height: '100% !important',
                overflow: 'auto !important',
                fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Consolas, monospace',
                fontSize: 14,
                lineHeight: 1.6,
              },
            }}
          />
        )}

        {showPreview && (
          <Paper
            variant="outlined"
            sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}
          >
            <Typography variant="overline" sx={{ px: 2, pt: 1, color: 'text.secondary' }}>
              Preview
            </Typography>
            <Box sx={{ flex: 1, overflow: 'auto', px: 2, pb: 2 }}>
              <MarkdownPreview html={preview} />
            </Box>
          </Paper>
        )}
      </Stack>

      <Accordion defaultExpanded={false} sx={{ mt: 2 }}>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography>Post details</Typography>
        </AccordionSummary>
        <AccordionDetails>
          <Stack spacing={2}>
            <TextField
              label="Preview Text"
              required
              fullWidth
              multiline
              minRows={3}
              value={post.preview ?? ''}
              onChange={(event) => update({ preview: event.target.value })}
            />
            <TextField
              label="Tags"
              required
              fullWidth
              helperText="Separate multiple tags with a semicolon"
              value={post.tags ?? ''}
              onChange={(event) => update({ tags: event.target.value })}
            />
            <TextField
              label="Page Url"
              fullWidth
              helperText="Optional. Set a custom page route (for example /about or / for the main page)."
              value={post.pageUrl ?? ''}
              onChange={(event) => update({ pageUrl: event.target.value })}
            />
            <TextField
              label="Image Url"
              required
              fullWidth
              value={post.imageUrl ?? ''}
              onChange={(event) => update({ imageUrl: event.target.value })}
              slotProps={{
                input: {
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton
                        id="button-search-image"
                        aria-label="Search image"
                        edge="end"
                        onClick={() => setImageDialogOpen(true)}
                      >
                        <SearchIcon />
                      </IconButton>
                    </InputAdornment>
                  ),
                },
              }}
            />
          </Stack>
        </AccordionDetails>
      </Accordion>

      <ImageSelectDialog
        open={imageDialogOpen}
        onClose={() => setImageDialogOpen(false)}
        onSelect={(url) => {
          update({ imageUrl: url });
          setImageDialogOpen(false);
        }}
      />
    </>
  );
};
