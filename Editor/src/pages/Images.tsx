import { useCallback, useEffect, useRef, useState } from 'react';
import {
  Box,
  Button,
  CircularProgress,
  IconButton,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Link,
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
import ContentCopyIcon from '@mui/icons-material/ContentCopy';
import OpenInNewIcon from '@mui/icons-material/OpenInNew';
import PreviewIcon from '@mui/icons-material/Preview';
import { useBlogClient } from '../api/BlogClientProvider';
import type { Blob } from '../api/models';
import { useNotification } from '../components/NotificationProvider';
import { RowActionsMenu } from '../components/RowActionsMenu';
import {
  breakAnywhere,
  compactButton,
  compactButtonLabel,
  dataTable,
  hideBelow,
} from '../components/tableStyles';

const defaultStatus = 'Drop a file here to upload it, or click to choose a file';
const maxFileSize = 200 * 1024 * 1024;

export const Images = () => {
  const client = useBlogClient();
  const notify = useNotification();
  const [blobs, setBlobs] = useState<Blob[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploadOpen, setUploadOpen] = useState(false);
  const [previewUrl, setPreviewUrl] = useState<string>();
  const [status, setStatus] = useState(defaultStatus);
  const [link, setLink] = useState('');
  const fileInput = useRef<HTMLInputElement>(null);

  const refresh = useCallback(async () => {
    setLoading(true);

    try {
      setBlobs(await client.getBlobs());
    } catch (error) {
      notify(`An error occured: ${(error as Error).message}`, 'error');
    } finally {
      setLoading(false);
    }
  }, [client, notify]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const upload = async () => {
    const file = fileInput.current?.files?.[0];

    if (!file) {
      return;
    }

    if (file.size > maxFileSize) {
      setStatus(`That's too big. Max size: ${maxFileSize} bytes.`);
      return;
    }

    try {
      const extension = file.name.split('.').pop() ?? '';
      setLink(await client.uploadFile(file, extension));
      setStatus('Uploaded');
      await refresh();
    } catch (error) {
      setStatus(`Error: ${(error as Error).message}`);
    }
  };

  const copyUrl = async (blob: Blob) => {
    try {
      await navigator.clipboard.writeText(blob.url);
      notify('Url copied to the clipboard', 'success');
    } catch {
      notify('The url could not be copied to the clipboard', 'error');
    }
  };

  const deleteBlob = async (blob: Blob) => {
    if (!window.confirm(`Are you sure you want to delete the post '${blob.name}'?`)) {
      return;
    }

    try {
      await client.deleteBlob(blob);
      await refresh();
    } catch (error) {
      notify(`An error occured: ${(error as Error).message}`, 'error');
    }
  };

  return (
    <>
      <TableContainer component={Paper}>
        <Toolbar sx={{ gap: 1, py: 1.5 }}>
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="h6" noWrap>
              Images
            </Typography>
            <Typography variant="body2" color="text.secondary" noWrap>
              {loading ? 'Loading…' : `${blobs.length} image${blobs.length === 1 ? '' : 's'}`}
            </Typography>
          </Box>
          <Box sx={{ flexGrow: 1 }} />
          <Tooltip title="Upload new image">
            <Button
              variant="contained"
              color="primary"
              aria-label="Upload new image"
              startIcon={<AddIcon />}
              onClick={() => setUploadOpen(true)}
              sx={compactButton}
            >
              <Box component="span" sx={compactButtonLabel}>
                Upload
              </Box>
            </Button>
          </Tooltip>
        </Toolbar>
        <Table size="small" sx={{ minWidth: 320, ...dataTable }}>
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell sx={hideBelow('md')}>Url</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading && (
              <TableRow>
                <TableCell colSpan={3} align="center" sx={{ py: 6 }}>
                  <CircularProgress />
                </TableCell>
              </TableRow>
            )}
            {!loading && blobs.length === 0 && (
              <TableRow>
                <TableCell colSpan={3} align="center" sx={{ py: 6 }}>
                  <Typography color="text.secondary">
                    No images yet. Upload one to use it in a post.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {!loading &&
              blobs.map((blob) => (
                <TableRow key={blob.name} hover>
                  <TableCell sx={{ ...breakAnywhere, minWidth: 160 }}>
                    <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
                      <Box
                        component="img"
                        src={blob.url}
                        alt=""
                        loading="lazy"
                        sx={{
                          width: 40,
                          height: 40,
                          objectFit: 'cover',
                          borderRadius: 1,
                          flexShrink: 0,
                          bgcolor: 'action.hover',
                        }}
                      />
                      <Typography variant="body2" sx={breakAnywhere}>
                        {blob.name}
                      </Typography>
                    </Stack>
                  </TableCell>
                  <TableCell sx={{ ...hideBelow('md'), maxWidth: 420 }}>
                    <Link
                      href={blob.url}
                      target="_blank"
                      rel="noopener noreferrer"
                      variant="body2"
                      sx={breakAnywhere}
                    >
                      {blob.url}
                    </Link>
                  </TableCell>
                  <TableCell align="right">
                    <Stack
                      direction="row"
                      spacing={0.5}
                      sx={{ justifyContent: 'flex-end', alignItems: 'center' }}
                    >
                      <Tooltip title="Preview image">
                        <IconButton
                          size="small"
                          color="info"
                          aria-label={`Preview ${blob.name}`}
                          onClick={() => setPreviewUrl(blob.url)}
                        >
                          <PreviewIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <RowActionsMenu
                        title={blob.name}
                        actions={[
                          {
                            label: 'Preview',
                            icon: <PreviewIcon fontSize="small" />,
                            onClick: () => setPreviewUrl(blob.url),
                          },
                          {
                            label: 'Copy url',
                            icon: <ContentCopyIcon fontSize="small" />,
                            onClick: () => void copyUrl(blob),
                          },
                          {
                            label: 'Open in new tab',
                            icon: <OpenInNewIcon fontSize="small" />,
                            onClick: () => window.open(blob.url, '_blank', 'noopener,noreferrer'),
                          },
                          {
                            label: 'Delete',
                            icon: <DeleteIcon fontSize="small" />,
                            onClick: () => void deleteBlob(blob),
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

      <Dialog open={uploadOpen} onClose={() => setUploadOpen(false)} fullWidth>
        <DialogTitle>Upload an image</DialogTitle>
        <DialogContent>
          <input type="file" ref={fileInput} aria-label="File to upload" />
          <Typography sx={{ mt: 1 }}>{status}</Typography>
          {link && (
            <Link href={link} target="_blank" rel="noopener noreferrer" sx={breakAnywhere}>
              {link}
            </Link>
          )}
        </DialogContent>
        <DialogActions>
          <Button variant="contained" color="success" onClick={() => void upload()}>
            Upload
          </Button>
          <Button variant="contained" color="error" onClick={() => setUploadOpen(false)}>
            Close
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={previewUrl !== undefined} onClose={() => setPreviewUrl(undefined)} fullWidth>
        <DialogTitle>Preview</DialogTitle>
        <DialogContent>
          <Box component="img" src={previewUrl} alt="Preview" sx={{ width: '100%' }} />
        </DialogContent>
        <DialogActions>
          <Button variant="contained" color="info" onClick={() => setPreviewUrl(undefined)}>
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};
