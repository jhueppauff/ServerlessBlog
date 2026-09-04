import { useCallback, useEffect, useRef, useState } from 'react';
import {
  Box,
  Button,
  CircularProgress,
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
  Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import PreviewIcon from '@mui/icons-material/Preview';
import { useBlogClient } from '../api/BlogClientProvider';
import type { Blob } from '../api/models';
import { useNotification } from '../components/NotificationProvider';
import { breakAnywhere, hideBelow } from '../components/tableStyles';

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
        <Toolbar sx={{ flexWrap: 'wrap', gap: 1, py: 1 }}>
          <Typography variant="h6">Images</Typography>
          <Box sx={{ flexGrow: 1 }} />
          <Button
            variant="contained"
            color="primary"
            startIcon={<AddIcon />}
            onClick={() => setUploadOpen(true)}
          >
            Upload new image
          </Button>
        </Toolbar>
        <Table size="small" sx={{ minWidth: 320 }}>
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell sx={hideBelow('md')}>Url</TableCell>
              <TableCell>Action</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading && (
              <TableRow>
                <TableCell colSpan={3} align="center">
                  <CircularProgress />
                </TableCell>
              </TableRow>
            )}
            {!loading &&
              blobs.map((blob) => (
                <TableRow key={blob.name} hover>
                  <TableCell sx={{ ...breakAnywhere, minWidth: 120 }}>{blob.name}</TableCell>
                  <TableCell sx={{ ...hideBelow('md'), ...breakAnywhere, maxWidth: 420 }}>
                    {blob.url}
                  </TableCell>
                  <TableCell>
                    <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
                      <Button
                        size="small"
                        variant="contained"
                        color="info"
                        startIcon={<PreviewIcon />}
                        onClick={() => setPreviewUrl(blob.url)}
                      >
                        Preview
                      </Button>
                      <Button
                        size="small"
                        variant="contained"
                        color="error"
                        startIcon={<DeleteIcon />}
                        onClick={() => void deleteBlob(blob)}
                      >
                        Delete
                      </Button>
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
