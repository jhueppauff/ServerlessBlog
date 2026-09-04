import { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Box, Button, Paper, Stack, TextField, Typography } from '@mui/material';
import SaveIcon from '@mui/icons-material/Save';
import SearchIcon from '@mui/icons-material/Search';
import DOMPurify from 'dompurify';
import { marked } from 'marked';
import { createSlug } from '../api/blogClient';
import { useBlogClient } from '../api/BlogClientProvider';
import { emptyPost, type PostMetadata } from '../api/models';
import { ImageSelectDialog } from '../components/ImageSelectDialog';
import { Loading } from '../components/Loading';
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
  const lines = useMemo(() => Math.max(body.split('\n').length, 20), [body]);

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
      <Typography variant="h4" gutterBottom>
        {slug ?? 'New Post'}
      </Typography>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <Paper sx={{ p: 2, flex: 1 }}>
          <Stack spacing={2}>
            <TextField
              label="Title"
              required
              value={post.title ?? ''}
              onChange={(event) => update({ title: event.target.value })}
            />
            <TextField
              label="Content"
              required
              multiline
              minRows={Math.min(lines, 50)}
              value={body}
              onChange={(event) => setBody(event.target.value)}
            />
          </Stack>
        </Paper>
        <Paper sx={{ p: 2, flex: 1 }}>
          <Typography variant="subtitle2">Preview</Typography>
          <Box dangerouslySetInnerHTML={{ __html: preview }} />
        </Paper>
      </Stack>

      <Stack spacing={2} sx={{ mt: 2 }}>
        <Paper sx={{ p: 2 }}>
          <Stack spacing={2}>
            <TextField
              label="Preview Text"
              required
              multiline
              minRows={3}
              value={post.preview ?? ''}
              onChange={(event) => update({ preview: event.target.value })}
            />
            <TextField
              label="Tags"
              required
              value={post.tags ?? ''}
              onChange={(event) => update({ tags: event.target.value })}
            />
          </Stack>
        </Paper>
        <Paper sx={{ p: 2 }}>
          <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
            <TextField
              label="Image Url"
              required
              fullWidth
              value={post.imageUrl ?? ''}
              onChange={(event) => update({ imageUrl: event.target.value })}
            />
            <Button
              id="button-search-image"
              variant="contained"
              color="success"
              aria-label="Search image"
              onClick={() => setImageDialogOpen(true)}
            >
              <SearchIcon />
            </Button>
          </Stack>
        </Paper>
        <Paper sx={{ p: 2 }}>
          <Button
            variant="contained"
            color="primary"
            startIcon={<SaveIcon />}
            onClick={() => void save()}
          >
            Save
          </Button>
        </Paper>
      </Stack>

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
