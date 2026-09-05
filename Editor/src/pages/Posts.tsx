import { useCallback, useEffect, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Box,
  Button,
  Chip,
  IconButton,
  CircularProgress,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Toolbar,
  Tooltip,
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
import { RowActionsMenu } from '../components/RowActionsMenu';
import { hideBelow, truncate } from '../components/tableStyles';
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
        <Toolbar sx={{ flexWrap: 'wrap', gap: 1, py: 1 }}>
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
          >
            Refresh
          </Button>
        </Toolbar>
        <Table size="small" sx={{ minWidth: 320 }}>
          <TableHead>
            <TableRow>
              <TableCell>Public</TableCell>
              <TableCell>Title</TableCell>
              <TableCell sx={hideBelow('md')}>Tags</TableCell>
              <TableCell sx={hideBelow('lg')}>Created on</TableCell>
              <TableCell sx={hideBelow('sm')}>Views</TableCell>
              <TableCell sx={hideBelow('lg')}>Intro</TableCell>
              <TableCell align="right">Actions</TableCell>
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
                  <TableCell sx={{ minWidth: 140 }}>{post.title}</TableCell>
                  <TableCell sx={hideBelow('md')}>
                    {(post.tags ?? '')
                      .split(';')
                      .filter((tag) => tag.length > 0)
                      .map((tag) => (
                        <Chip key={tag} label={tag} size="small" sx={{ mr: 0.5 }} />
                      ))}
                  </TableCell>
                  <TableCell sx={hideBelow('lg')}>{post.published}</TableCell>
                  <TableCell sx={hideBelow('sm')}>{post.views}</TableCell>
                  <TableCell sx={{ ...hideBelow('lg'), maxWidth: 320 }}>
                    <Typography variant="body2" sx={truncate}>
                      {post.preview}
                    </Typography>
                  </TableCell>
                  <TableCell align="right">
                    <Stack
                      direction="row"
                      spacing={0.5}
                      sx={{ justifyContent: 'flex-end', alignItems: 'center' }}
                    >
                      <Tooltip title="Edit post">
                        <IconButton
                          size="small"
                          color="primary"
                          component={RouterLink}
                          to={`/edit/${encodeURIComponent(post.slug)}`}
                          aria-label={`Edit ${post.title}`}
                        >
                          <EditIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <RowActionsMenu
                        title={post.title ?? post.slug}
                        actions={[
                          {
                            label: 'Edit',
                            icon: <EditIcon fontSize="small" />,
                            to: `/edit/${encodeURIComponent(post.slug)}`,
                          },
                          ...(post.isPublic
                            ? []
                            : [
                                {
                                  label: 'Publish',
                                  icon: <PublicIcon fontSize="small" />,
                                  onClick: () => setPostToPublish(post),
                                },
                              ]),
                          {
                            label: 'Delete',
                            icon: <DeleteIcon fontSize="small" />,
                            onClick: () => setPostToDelete(post),
                            destructive: true,
                          },
                        ]}
                      />
                    </Stack>
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
