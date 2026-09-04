import { useEffect, useMemo, useState } from 'react';
import { Link as RouterLink, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  IconButton,
  InputAdornment,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import SaveIcon from '@mui/icons-material/Save';
import SearchIcon from '@mui/icons-material/Search';
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

export const EditPost = () => {
  const { slug } = useParams<{ slug: string }>();
  const client = useBlogClient();
  const notify = useNotification();
  const [post, setPost] = useState<PostMetadata>();
  const [body, setBody] = useState('');
  const [imageDialogOpen, setImageDialogOpen] = useState(false);

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
    const postToSave: PostMetadata = {
      ...post,
      slug: slugToSave,
      partitionKey: slugToSave,
      rowKey: slugToSave,
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
      <Paper
        sx={{
          p: 2,
          mb: 2,
          position: 'sticky',
          top: (theme) => theme.spacing(8),
          zIndex: 2,
        }}
      >
        <Stack
          direction="row"
          spacing={2}
          sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}
        >
          <IconButton component={RouterLink} to="/" aria-label="Back to posts">
            <ArrowBackIcon />
          </IconButton>
          <Typography variant="h6" noWrap sx={{ flexGrow: 1, minWidth: 0 }}>
            {slug ?? 'New Post'}
          </Typography>
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

      <Stack direction={{ xs: 'column', lg: 'row' }} spacing={2} sx={{ alignItems: 'stretch' }}>
        <Paper
          sx={{
            p: 2,
            flex: 1,
            minWidth: 0,
            display: 'flex',
            flexDirection: 'column',
            gap: 2,
          }}
        >
          <TextField
            label="Title"
            required
            fullWidth
            value={post.title ?? ''}
            onChange={(event) => update({ title: event.target.value })}
          />
          <TextField
            label="Content"
            required
            fullWidth
            multiline
            minRows={12}
            value={body}
            onChange={(event) => setBody(event.target.value)}
            sx={{
              flex: 1,
              '& .MuiInputBase-root': { height: '100%', alignItems: 'flex-start' },
              '& textarea': { height: '100% !important', overflow: 'auto !important' },
            }}
          />
        </Paper>

        <Paper sx={{ p: 2, flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
          <Typography variant="subtitle2" gutterBottom>
            Preview
          </Typography>
          <Box sx={{ flex: 1, overflow: 'auto', maxHeight: { lg: '70vh' } }}>
            <MarkdownPreview html={preview} />
          </Box>
        </Paper>
      </Stack>

      <Paper sx={{ p: 2, mt: 2 }}>
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
      </Paper>

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
