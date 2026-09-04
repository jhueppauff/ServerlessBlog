import { useCallback, useEffect, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Toolbar,
  Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import PublicIcon from '@mui/icons-material/Public';
import PublicOffIcon from '@mui/icons-material/PublicOff';
import RefreshIcon from '@mui/icons-material/Refresh';
import { useBlogClient } from '../api/BlogClientProvider';
import type { PostMetadata } from '../api/models';
import { DeletePostDialog } from '../components/DeletePostDialog';
import { PublishDialog } from '../components/PublishDialog';
import { useNotification } from '../components/NotificationProvider';

const toDateInputValue = (date: Date): string => date.toISOString().slice(0, 10);

export const Posts = () => {
  const client = useBlogClient();
  const notify = useNotification();
  const [posts, setPosts] = useState<PostMetadata[]>([]);
  const [loading, setLoading] = useState(true);
  const [postToDelete, setPostToDelete] = useState<PostMetadata>();
  const [postToPublish, setPostToPublish] = useState<PostMetadata>();
  const [publishDate, setPublishDate] = useState(toDateInputValue(new Date()));

  const refresh = useCallback(async () => {
    setLoading(true);

    try {
      const loadedPosts = await client.getBlogPosts();
      const pageViews = await client.getPageViews();

      setPosts(
        loadedPosts.map((post) => ({
          ...post,
          views: pageViews.find((view) => view.slug === post.partitionKey)?.views ?? 0,
        })),
      );
    } catch (error) {
      notify(`An error occured: ${(error as Error).message}`, 'error');
    } finally {
      setLoading(false);
    }
  }, [client, notify]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const deletePost = async (post: PostMetadata) => {
    setPostToDelete(undefined);

    try {
      await client.deleteBlogPost(post);
      notify('Post Deleted', 'success');
      await refresh();
    } catch (error) {
      notify(`An error occured: ${(error as Error).message}`, 'error');
    }
  };

  const publish = async () => {
    if (!postToPublish) {
      return;
    }

    try {
      await client.publishPost(postToPublish.slug, new Date(publishDate));
      notify('Post scheduled for publishing', 'success');
    } catch (error) {
      notify(`An error occured: ${(error as Error).message}`, 'error');
    } finally {
      setPostToPublish(undefined);
    }
  };

  return (
    <>
      <TableContainer component={Paper}>
        <Toolbar>
          <Typography variant="h6">Blog Posts</Typography>
          <Box sx={{ flexGrow: 1 }} />
          <Button
            component={RouterLink}
            to="/add"
            variant="contained"
            color="primary"
            startIcon={<AddIcon />}
          >
            Add
          </Button>
          <Button
            variant="contained"
            color="info"
            startIcon={<RefreshIcon />}
            onClick={() => void refresh()}
            sx={{ ml: 1 }}
          >
            Refresh
          </Button>
        </Toolbar>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Public</TableCell>
              <TableCell>Title</TableCell>
              <TableCell>Tags</TableCell>
              <TableCell>Created on</TableCell>
              <TableCell>Views</TableCell>
              <TableCell>Intro</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading && (
              <TableRow>
                <TableCell colSpan={7} align="center">
                  <CircularProgress />
                </TableCell>
              </TableRow>
            )}
            {!loading &&
              posts.map((post) => (
                <TableRow key={post.partitionKey} hover>
                  <TableCell>
                    {post.isPublic ? (
                      <PublicIcon color="success" titleAccess="Public" />
                    ) : (
                      <PublicOffIcon color="error" titleAccess="Not public" />
                    )}
                  </TableCell>
                  <TableCell>{post.title}</TableCell>
                  <TableCell>
                    {(post.tags ?? '')
                      .split(';')
                      .filter((tag) => tag.length > 0)
                      .map((tag) => (
                        <Chip key={tag} label={tag} size="small" sx={{ mr: 0.5 }} />
                      ))}
                  </TableCell>
                  <TableCell>{post.published}</TableCell>
                  <TableCell>{post.views}</TableCell>
                  <TableCell>{post.preview}</TableCell>
                  <TableCell>
                    <Button
                      variant="contained"
                      color="error"
                      startIcon={<DeleteIcon />}
                      onClick={() => setPostToDelete(post)}
                    >
                      Delete
                    </Button>
                    {!post.isPublic && (
                      <Button
                        variant="contained"
                        color="success"
                        startIcon={<PublicIcon />}
                        sx={{ ml: 1 }}
                        onClick={() => setPostToPublish(post)}
                      >
                        Publish
                      </Button>
                    )}
                    <Button
                      component={RouterLink}
                      to={`/edit/${encodeURIComponent(post.slug)}`}
                      variant="contained"
                      color="success"
                      startIcon={<EditIcon />}
                      sx={{ ml: 1 }}
                    >
                      Edit
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
          </TableBody>
        </Table>
      </TableContainer>

      <DeletePostDialog
        post={postToDelete}
        onCancel={() => setPostToDelete(undefined)}
        onConfirm={(post) => void deletePost(post)}
      />

      <PublishDialog
        open={postToPublish !== undefined}
        publishDate={publishDate}
        onDateChange={setPublishDate}
        onCancel={() => setPostToPublish(undefined)}
        onPublish={() => void publish()}
      />
    </>
  );
};
